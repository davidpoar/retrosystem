using System.Collections.Generic;
using System.Text;

namespace Tape.Blocks;

/// <summary>
/// Block ID 0x21 — Group Start.
/// Marks the beginning of a named group of blocks.
/// Groups are informational only; they produce no pulses.
/// </summary>
public class GroupStartBlock : TapeBlock
{
    public override byte BlockId => 0x21;
    public override string Description => $"Group Start: {GroupName}";

    public string GroupName { get; private set; } = string.Empty;

    public override void Parse(BinaryReader reader)
    {
        byte len = reader.ReadByte();
        GroupName = Encoding.ASCII.GetString(reader.ReadBytes(len));
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        yield break; // No audio output
    }
}
