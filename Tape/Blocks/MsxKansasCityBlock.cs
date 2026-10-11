using System;
using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x4B — Kansas City Standard (KCS) Block — MSX / TSX specific.
/// Encodes data using the Kansas City Standard audio modulation:
///   '1' bit = 2 cycles at 2400 Hz
///   '0' bit = 1 cycle at 1200 Hz
/// This is the native cassette protocol used by MSX BIOS (BLOAD/BSAVE/CLOAD/CSAVE).
/// Includes a leader tone, start bit, 8 data bits, and 2 stop bits per byte.
/// </summary>
public class MsxKansasCityBlock : TapeBlock
{
    public override byte BlockId => 0x4B;
    public override string Description =>
        $"MSX Kansas City Block: {Data?.Length ?? 0} bytes, {BaudRate} baud";

    public uint BlockLen        { get; private set; }
    public ushort PauseAfterMs  { get; private set; }
    public ushort PilotLen      { get; private set; } // duration of pilot pulse in T-states
    public ushort PilotCount    { get; private set; } // number of pilot pulses
    public ushort Bit0Len       { get; private set; } // duration of zero pulse in T-states
    public ushort Bit1Len       { get; private set; } // duration of one pulse in T-states
    public byte BitCfg          { get; private set; }
    public byte ByteCfg         { get; private set; }
    public byte[] Data          { get; private set; } = Array.Empty<byte>();

    // Computed properties for compatibility and details
    public ushort BaudRate => Bit1Len < 500 ? (ushort)2400 : (ushort)1200;
    public byte DataBits => 8;
    public byte NumZeroPulses => (byte)(((BitCfg & 0xF0) >> 4) is int n && n == 0 ? 16 : n);
    public byte NumOnePulses  => (byte)((BitCfg & 0x0F) is int n && n == 0 ? 16 : n);
    public byte StartBits     => (byte)((ByteCfg & 0xC0) >> 6);
    public byte StartValue    => (byte)((ByteCfg & 0x20) >> 5);
    public byte StopBits      => (byte)((ByteCfg & 0x18) >> 3);
    public byte StopValue     => (byte)((ByteCfg & 0x04) >> 2);
    public bool MsbFirst      => (ByteCfg & 0x01) != 0;

    public override void Parse(BinaryReader reader)
    {
        BlockLen     = reader.ReadUInt32();
        PauseAfterMs = reader.ReadUInt16();
        PilotLen     = reader.ReadUInt16();
        PilotCount   = reader.ReadUInt16();
        Bit0Len      = reader.ReadUInt16();
        Bit1Len      = reader.ReadUInt16();
        BitCfg       = reader.ReadByte();
        ByteCfg      = reader.ReadByte();

        int dataLength = (int)BlockLen - 12;
        if (dataLength < 0) dataLength = 0;
        Data = reader.ReadBytes(dataLength);
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        // TSX specification defines timings in standard Z80 T-states (3,579,545 Hz for MSX).
        // Scale to emulator CPU frequency if different.
        double scale = cpuClockHz / 3579545.0;
        long pulsePilot = Math.Max(1, (long)Math.Round(PilotLen * scale));
        long pulseOne   = Math.Max(1, (long)Math.Round(Bit1Len * scale));
        long pulseZero  = Math.Max(1, (long)Math.Round(Bit0Len * scale));

        // 1. Leader tone: PilotCount pulses of duration pulsePilot
        for (int i = 0; i < PilotCount; i++)
        {
            yield return pulsePilot;
        }

        // Helper: emit pulses for a bit (0 or 1)
        IEnumerable<long> EmitBit(bool bit)
        {
            if (bit)
            {
                // '1' = NumOnePulses pulses of pulseOne duration
                for (int i = 0; i < NumOnePulses; i++) yield return pulseOne;
            }
            else
            {
                // '0' = NumZeroPulses pulses of pulseZero duration
                for (int i = 0; i < NumZeroPulses; i++) yield return pulseZero;
            }
        }

        // 2. Encode each byte with start bit(s), data bits, stop bit(s)
        foreach (byte b in Data)
        {
            // Start bits
            for (int t = 0; t < StartBits; t++)
            {
                foreach (var p in EmitBit(StartValue == 1)) yield return p;
            }

            // Data bits (8 bits, LSB first by default)
            for (int bit = 0; bit < 8; bit++)
            {
                bool bitValue = MsbFirst ? (((b >> (7 - bit)) & 1) == 1) : (((b >> bit) & 1) == 1);
                foreach (var p in EmitBit(bitValue)) yield return p;
            }

            // Stop bits
            for (int t = 0; t < StopBits; t++)
            {
                foreach (var p in EmitBit(StopValue == 1)) yield return p;
            }
        }

        // 3. Pause after block (capped to 1 second/1000 ms to avoid timeouts)
        if (PauseAfterMs > 0)
        {
            int pause = PauseAfterMs;
            if (pause > 1000)
                pause = 1000;
            yield return (long)pause * cpuClockHz / 1000;
        }
    }
}
