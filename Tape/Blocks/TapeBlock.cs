using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Abstract base class for all TZX/TSX tape blocks.
/// Each subclass represents a block type identified by its ID byte.
/// </summary>
public abstract class TapeBlock
{
    /// <summary>The 1-byte block identifier from the TZX/TSX specification.</summary>
    public abstract byte BlockId { get; }

    /// <summary>Human-readable description of this block type.</summary>
    public abstract string Description { get; }

    /// <summary>Parse the block payload (after the ID byte has already been consumed).</summary>
    public abstract void Parse(BinaryReader reader);

    /// <summary>
    /// Generate an ordered sequence of pulse durations in T-states (CPU clock cycles)
    /// that this block represents on the tape signal line.
    /// </summary>
    /// <param name="cpuClockHz">CPU clock frequency of the target machine in Hz.</param>
    public abstract IEnumerable<long> GeneratePulses(int cpuClockHz);
}
