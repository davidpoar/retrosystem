using System;
using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x10 — Standard Speed Data Block.
/// The standard ZX Spectrum tape block containing a leader tone, two sync pulses,
/// the data bits and a trailing pause. This is the format used by the stock ROM
/// loading routine (LD-BYTES at ROM address 0x0556).
/// </summary>
public class StandardDataBlock : TapeBlock
{
    public override byte BlockId => 0x10;
    public override string Description =>
        $"Standard Data: {Data?.Length ?? 0} bytes, pause {PauseAfterMs} ms";

    // ---- Standard Spectrum tape timing constants (in T-states) ----
    // Reference: ZX Spectrum ROM LD-BYTES routine, ~3.5 MHz clock
    private const int PILOT_PULSE  = 2168;  // Duration of each pilot tone pulse
    private const int SYNC1_PULSE  = 667;   // First sync pulse
    private const int SYNC2_PULSE  = 735;   // Second sync pulse
    private const int BIT0_PULSE   = 855;   // Pulse width for a '0' bit (x2)
    private const int BIT1_PULSE   = 1710;  // Pulse width for a '1' bit (x2)
    private const int HEADER_PILOT = 8063;  // Number of pilot pulses for a header block
    private const int DATA_PILOT   = 3223;  // Number of pilot pulses for a data block

    /// <summary>Pause after this block in milliseconds.</summary>
    public ushort PauseAfterMs { get; private set; }

    /// <summary>Raw tape bytes (including the leading flag byte).</summary>
    public byte[] Data { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// True if this is a header block (flag byte 0x00),
    /// false if it is a data block (flag byte 0xFF).
    /// </summary>
    public bool IsHeader => Data.Length > 0 && Data[0] == 0x00;

    public override void Parse(BinaryReader reader)
    {
        PauseAfterMs = reader.ReadUInt16();
        ushort len = reader.ReadUInt16();
        Data = reader.ReadBytes(len);
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        // Scale timing constants from 3.5 MHz to the actual target CPU clock
        double scale = cpuClockHz / 3_500_000.0;

        long Scale(int tstates) => Math.Max(1, (long)(tstates * scale));

        // 1. Leader (pilot) tone
        int pilotCount = IsHeader ? HEADER_PILOT : DATA_PILOT;
        for (int i = 0; i < pilotCount; i++)
            yield return Scale(PILOT_PULSE);

        // 2. Two sync pulses
        yield return Scale(SYNC1_PULSE);
        yield return Scale(SYNC2_PULSE);

        // 3. Data bits (MSB first, two pulses per bit)
        foreach (byte b in Data)
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                long pulseDuration = ((b >> bit) & 1) == 1
                    ? Scale(BIT1_PULSE)
                    : Scale(BIT0_PULSE);
                yield return pulseDuration;
                yield return pulseDuration; // Each bit is encoded as two equal pulses
            }
        }

        // 4. Trailing pause
        if (PauseAfterMs > 0)
        {
            long tStates = (long)PauseAfterMs * cpuClockHz / 1000;
            yield return tStates;
        }
    }
}
