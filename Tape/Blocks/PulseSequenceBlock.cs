using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x13 — Pulse Sequence.
/// An arbitrary sequence of pulses, each with its own explicit duration in T-states.
/// Used for sync pulses and custom loaders.
/// </summary>
public class PulseSequenceBlock : TapeBlock
{
    public override byte BlockId => 0x13;
    public override string Description => $"Pulse Sequence: {Pulses?.Length ?? 0} pulses";

    /// <summary>Array of pulse durations in T-states.</summary>
    public ushort[] Pulses { get; private set; } = Array.Empty<ushort>();

    public override void Parse(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        Pulses = new ushort[count];
        for (int i = 0; i < count; i++)
            Pulses[i] = reader.ReadUInt16();
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        double scale = cpuClockHz / 3_500_000.0;
        foreach (var p in Pulses)
            yield return Math.Max(1, (long)Math.Round(p * scale));
    }
}
