using System.IO;

namespace Tape.Loaders;

public interface ITapeLoader
{
    void Load(Stream stream, TapeFile tapeFile);
}
