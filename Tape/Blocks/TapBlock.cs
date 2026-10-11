using System;
using System.Collections.Generic;

namespace Tape.Blocks;

public class TapBlock : TapeBlock
{
    public override byte BlockId => 0x00;

    public override string Description =>
        $"TAP Data: {Data?.Length ?? 0} bytes";

    public byte[] Data { get; private set; } = Array.Empty<byte>();

    public bool IsHeader => Data.Length > 0 && Data[0] == 0x00;

    public override void Parse(BinaryReader reader)
    {
        ushort len = reader.ReadUInt16();
        Data = reader.ReadBytes(len);
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        double scale = cpuClockHz / 3_500_000.0;
        long Scale(int tstates) => Math.Max(1, (long)(tstates * scale));

        const int PILOT_PULSE = 2168;
        const int SYNC1_PULSE = 667;
        const int SYNC2_PULSE = 735;
        const int BIT0_PULSE = 855;
        const int BIT1_PULSE = 1710;
        const int HEADER_PILOT = 8063;
        const int DATA_PILOT = 3223;

        int pilotCount = IsHeader ? HEADER_PILOT : DATA_PILOT;
        for (int i = 0; i < pilotCount; i++)
            yield return Scale(PILOT_PULSE);

        yield return Scale(SYNC1_PULSE);
        yield return Scale(SYNC2_PULSE);

        foreach (byte b in Data)
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                long pulseDuration = ((b >> bit) & 1) == 1
                    ? Scale(BIT1_PULSE)
                    : Scale(BIT0_PULSE);
                yield return pulseDuration;
                yield return pulseDuration;
            }
        }
    }
}