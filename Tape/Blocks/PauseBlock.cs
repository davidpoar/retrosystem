using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x20 — Pause (silence) or stop tape.
/// A pause of <see cref="DurationMs"/> milliseconds of silence.
/// A duration of 0 means "stop the tape".
/// </summary>
public class PauseBlock : TapeBlock
{
    public override byte BlockId => 0x20;
    public override string Description => $"Pause {DurationMs} ms";

    /// <summary>Pause duration in milliseconds. 0 = stop tape.</summary>
    public ushort DurationMs { get; private set; }

    public override void Parse(BinaryReader reader)
    {
        DurationMs = reader.ReadUInt16();
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        if (DurationMs == 0)
            yield break;
        // Cap duration to at most 1000 ms to avoid emulator loading timeouts
        int duration = DurationMs;
        if (duration > 1000)
            duration = 1000;
        // Convert ms to T-states: cycles = (ms / 1000) * cpuClockHz
        long tStates = (long)duration * cpuClockHz / 1000;
        // A pause is a single long low-level pulse
        yield return tStates;
    }
}
