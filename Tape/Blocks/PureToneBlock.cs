using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x12 — Pure Tone.
/// A sequence of <see cref="PulseCount"/> pulses, each lasting <see cref="PulseDurationTStates"/> T-states.
/// Used for leader tones and calibration.
/// </summary>
public class PureToneBlock : TapeBlock
{
    public override byte BlockId => 0x12;
    public override string Description => $"Pure Tone: {PulseCount} x {PulseDurationTStates} T-states";

    /// <summary>Duration of each individual pulse in T-states.</summary>
    public ushort PulseDurationTStates { get; private set; }

    /// <summary>Total number of pulses to emit.</summary>
    public ushort PulseCount { get; private set; }

    public override void Parse(BinaryReader reader)
    {
        PulseDurationTStates = reader.ReadUInt16();
        PulseCount = reader.ReadUInt16();
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        double scale = cpuClockHz / 3_500_000.0;
        long pulseDuration = Math.Max(1, (long)Math.Round(PulseDurationTStates * scale));
        for (int i = 0; i < PulseCount; i++)
            yield return pulseDuration;
    }
}
