using System.Collections.Generic;
using System.IO;

namespace Tape.Blocks;

/// <summary>
/// Used for block IDs that are known to the TZX spec but produce no audio
/// (text descriptions, archive info, hardware type, etc.).
/// All parsing is done sequentially in the Parse method based on the block ID.
/// </summary>
public class SkipBlock : TapeBlock
{
    private readonly byte _id;
    private int _parsedBytesCount = 0;

    public override byte BlockId => _id;
    public override string Description => $"Skip block 0x{_id:X2} ({_parsedBytesCount} bytes)";

    public SkipBlock(byte id)
    {
        _id = id;
    }

    public override void Parse(BinaryReader reader)
    {
        long startPos = reader.BaseStream.Position;
        switch (_id)
        {
            // 0x14 Pure Data Block:
            //   00h (2 bytes): Length of ZERO bit pulse
            //   02h (2 bytes): Length of ONE bit pulse
            //   04h (1 byte):  Used bits in last byte
            //   05h (2 bytes): Pause after this block
            //   07h (3 bytes): Length of data (N)
            //   0Ah (N bytes): Data
            case 0x14:
                {
                    reader.ReadBytes(7); // Skip ZERO/ONE pulses, Used bits, Pause
                    int lo = reader.ReadByte();
                    int mi = reader.ReadByte();
                    int hi = reader.ReadByte();
                    int dataLen = lo | (mi << 8) | (hi << 16);
                    reader.ReadBytes(dataLen);
                }
                break;

            // 0x15 Direct Recording:
            //   00h (2 bytes): T-states per sample-bit
            //   02h (2 bytes): Pause after this block
            //   04h (1 byte):  Used bits in last byte
            //   05h (3 bytes): Length of data (N)
            //   08h (N bytes): Data
            case 0x15:
                {
                    reader.ReadBytes(5); // Skip T-states, Pause, Used bits
                    int lo = reader.ReadByte();
                    int mi = reader.ReadByte();
                    int hi = reader.ReadByte();
                    int dataLen = lo | (mi << 8) | (hi << 16);
                    reader.ReadBytes(dataLen);
                }
                break;

            // 0x23 Jump to Block (2 bytes)
            // 0x24 Loop Start (2 bytes)
            case 0x23:
            case 0x24:
                reader.ReadBytes(2);
                break;

            // 0x25 Loop End (0 bytes)
            // 0x27 Return from Sequence (0 bytes)
            case 0x25:
            case 0x27:
                break;

            // 0x26 Call Sequence: 2 bytes count (N), then N * 2 bytes
            case 0x26:
                {
                    int count = reader.ReadUInt16();
                    reader.ReadBytes(count * 2);
                }
                break;

            // 0x28 Select Block: 2 bytes length (N), then N bytes
            case 0x28:
                {
                    int len = reader.ReadUInt16();
                    reader.ReadBytes(len);
                }
                break;

            // 0x2A Stop the Tape if in 48K mode (4 bytes)
            case 0x2A:
                reader.ReadBytes(4);
                break;

            // 0x2B Set Signal Level: 4 bytes length (N), then N bytes payload (usually N=1)
            case 0x2B:
                {
                    int len = (int)reader.ReadUInt32();
                    reader.ReadBytes(len);
                }
                break;

            // 0x30 Text Description: 1 byte length (N), then N bytes ASCII
            case 0x30:
                {
                    int len = reader.ReadByte();
                    reader.ReadBytes(len);
                }
                break;

            // 0x31 Message Block: 1 byte time + 1 byte length (N) + N bytes ASCII
            case 0x31:
                {
                    reader.ReadByte(); // time
                    int len = reader.ReadByte();
                    reader.ReadBytes(len);
                }
                break;

            // 0x32 Archive Info: 2 bytes length (N) + N bytes ASCII
            case 0x32:
                {
                    int len = reader.ReadUInt16();
                    reader.ReadBytes(len);
                }
                break;

            // 0x33 Hardware Type: 1 byte count (N) + N * 3 bytes
            case 0x33:
                {
                    int count = reader.ReadByte();
                    reader.ReadBytes(count * 3);
                }
                break;

            // 0x35 Custom Info: 16 bytes ID + 4 bytes length (N) + N bytes
            case 0x35:
                {
                    reader.ReadBytes(16);
                    int len = (int)reader.ReadUInt32();
                    reader.ReadBytes(len);
                }
                break;

            // 0x5A Glue Block: 9 bytes
            case 0x5A:
                reader.ReadBytes(9);
                break;

            default:
                break;
        }
        _parsedBytesCount = (int)(reader.BaseStream.Position - startPos);
    }

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        yield break;
    }
}
