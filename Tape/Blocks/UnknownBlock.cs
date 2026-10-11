using System.Collections.Generic;
using System.IO;

namespace Tape.Blocks;

/// <summary>
/// Fallback for unrecognised block IDs.
/// The block is skipped by reading and discarding its data.
/// </summary>
public class UnknownBlock : TapeBlock
{
    private readonly byte _id;
    private int _skipLength = 0;

    public override byte BlockId => _id;
    public override string Description => $"Unknown block 0x{_id:X2} ({_skipLength} bytes)";

    public UnknownBlock(byte id)
    {
        _id = id;
    }

    public override void Parse(BinaryReader reader)
    {
        // Try reading 4-byte length prefix (standard TZX extension rule)
        if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
        {
            long savedPos = reader.BaseStream.Position;
            int skipLen = reader.ReadInt32();
            if (skipLen >= 0 && skipLen < 1_000_000 &&
                reader.BaseStream.Position + skipLen <= reader.BaseStream.Length)
            {
                reader.ReadBytes(skipLen);
                _skipLength = skipLen;
                return;
            }
            reader.BaseStream.Position = savedPos;
        }
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        yield break;
    }
}
