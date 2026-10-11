using System.IO;
using System.Text;
using Tape.Blocks;

namespace Tape.Loaders;

public class TzxLoader : ITapeLoader
{
    private const string TzxSignature = "ZXTape!";
    private const string TsxSignature = "MSXTape!";

    public void Load(Stream stream, TapeFile tapeFile)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        ParseHeader(reader, tapeFile);
        ParseBlocks(reader, tapeFile);
    }

    private void ParseHeader(BinaryReader reader, TapeFile tapeFile)
    {
        // Try TZX signature first ("ZXTape!" = 7 bytes)
        byte[] sigBytes = reader.ReadBytes(7);
        string sig7 = Encoding.ASCII.GetString(sigBytes);

        if (sig7 == TzxSignature)
        {
            reader.ReadByte(); // 0x1A (EOF character)
            tapeFile.System = TapeSystem.ZxSpectrum;
        }
        else
        {
            // Try TSX: "MSXTape!" = 8 bytes — we already read 7, read one more
            byte extra = reader.ReadByte();
            string sig8 = sig7 + (char)extra;
            if (sig8 == TsxSignature)
            {
                reader.ReadByte(); // 0x1A
                tapeFile.System = TapeSystem.Msx;
            }
            else
            {
                throw new InvalidDataException(
                    $"Unrecognised tape file signature: '{sig7}'. " +
                    "Expected 'ZXTape!' (TZX) or 'MSXTape!' (TSX).");
            }
        }

        tapeFile.VersionMajor = reader.ReadByte();
        tapeFile.VersionMinor = reader.ReadByte();
    }

    private void ParseBlocks(BinaryReader reader, TapeFile tapeFile)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte id = reader.ReadByte();
            if (id == 0x4B)
            {
                tapeFile.System = TapeSystem.Msx;
            }
            TapeBlock block = CreateBlock(id, reader);
            block.Parse(reader);
            tapeFile.Blocks.Add(block);
        }
    }

    private static TapeBlock CreateBlock(byte id, BinaryReader reader)
    {
        return id switch
        {
            // ── Audio data blocks ───────────────────────────────────────────
            0x10 => new StandardDataBlock(),
            0x11 => new TurboDataBlock(),
            0x12 => new PureToneBlock(),
            0x13 => new PulseSequenceBlock(),
            0x14 => new SkipBlock(id),
            0x15 => new SkipBlock(id),

            // ── Control / info blocks ────────────────────────────────────────
            0x20 => new PauseBlock(),
            0x21 => new GroupStartBlock(),
            0x22 => new GroupEndBlock(),
            0x23 => new SkipBlock(id),
            0x24 => new SkipBlock(id),
            0x25 => new SkipBlock(id),
            0x26 => new SkipBlock(id),
            0x27 => new SkipBlock(id),
            0x28 => new SkipBlock(id),
            0x2A => new SkipBlock(id),
            0x2B => new SkipBlock(id),

            // ── Text / meta blocks ───────────────────────────────────────────
            0x30 => new SkipBlock(id),
            0x31 => new SkipBlock(id),
            0x32 => new SkipBlock(id),
            0x33 => new SkipBlock(id),
            0x35 => new SkipBlock(id),
            0x5A => new SkipBlock(id),

            // ── MSX specific ────────────────────────────────────────────────
            0x4B => new MsxKansasCityBlock(),

            // ── Fallback ────────────────────────────────────────────────────
            _ => new UnknownBlock(id)
        };
    }
}
