using System;
using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x11 — Turbo Speed Data Block.
/// Like <see cref="StandardDataBlock"/> but with fully custom timing parameters.
/// Used by commercial games with faster custom loaders (e.g. Speedlock, Alkatraz).
/// </summary>
public class TurboDataBlock : TapeBlock
{
    public override byte BlockId => 0x11;
    public override string Description =>
        $"Turbo Data: {DataLength} bytes, pause {PauseAfterMs} ms";

    public ushort PilotPulseTStates  { get; private set; }
    public ushort Sync1TStates       { get; private set; }
    public ushort Sync2TStates       { get; private set; }
    public ushort Bit0TStates        { get; private set; }
    public ushort Bit1TStates        { get; private set; }
    public ushort PilotPulseCount    { get; private set; }
    public byte   LastByteBitCount   { get; private set; }
    public ushort PauseAfterMs       { get; private set; }
    public int    DataLength         { get; private set; }
    public byte[] Data               { get; private set; } = Array.Empty<byte>();

    public override void Parse(BinaryReader reader)
    {
        PilotPulseTStates = reader.ReadUInt16();
        Sync1TStates      = reader.ReadUInt16();
        Sync2TStates      = reader.ReadUInt16();
        Bit0TStates       = reader.ReadUInt16();
        Bit1TStates       = reader.ReadUInt16();
        PilotPulseCount   = reader.ReadUInt16();
        LastByteBitCount  = reader.ReadByte();
        PauseAfterMs      = reader.ReadUInt16();
        // DataLength is 3 bytes (little-endian)
        int lo = reader.ReadByte();
        int mi = reader.ReadByte();
        int hi = reader.ReadByte();
        DataLength = lo | (mi << 8) | (hi << 16);
        Data = reader.ReadBytes(DataLength);
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        double scale = cpuClockHz / 3_500_000.0;
        long Scale(ushort tstates) => Math.Max(1, (long)Math.Round(tstates * scale));

        // 1. Pilot tone
        for (int i = 0; i < PilotPulseCount; i++)
            yield return Scale(PilotPulseTStates);

        // 2. Sync pulses
        yield return Scale(Sync1TStates);
        yield return Scale(Sync2TStates);

        // 3. Data bits
        for (int byteIdx = 0; byteIdx < Data.Length; byteIdx++)
        {
            int bitsInThisByte = (byteIdx == Data.Length - 1) ? LastByteBitCount : 8;
            byte b = Data[byteIdx];
            for (int bit = 7; bit >= (8 - bitsInThisByte); bit--)
            {
                long pulseDuration = ((b >> bit) & 1) == 1 ? Scale(Bit1TStates) : Scale(Bit0TStates);
                yield return pulseDuration;
                yield return pulseDuration;
            }
        }

        // 4. Pause
        if (PauseAfterMs > 0)
            yield return (long)PauseAfterMs * cpuClockHz / 1000;
    }
}
