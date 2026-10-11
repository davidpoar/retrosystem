using System.Collections.Generic;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x22 — Group End.
/// Marks the end of a group started by <see cref="GroupStartBlock"/>.
/// Produces no pulses.
/// </summary>
public class GroupEndBlock : TapeBlock
{
    public override byte BlockId => 0x22;
    public override string Description => "Group End";

    public override void Parse(BinaryReader reader) { } // No payload

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        yield break;
    }
}
