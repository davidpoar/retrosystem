using System.IO;
using Tape.Blocks;

namespace Tape.Loaders;

public class TapLoader : ITapeLoader
{
    public void Load(Stream stream, TapeFile tapeFile)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        tapeFile.System = TapeSystem.ZxSpectrum;
        tapeFile.VersionMajor = 1;
        tapeFile.VersionMinor = 0;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var block = new TapBlock();
            block.Parse(reader);
            tapeFile.Blocks.Add(block);
        }
    }
}