using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tape.Blocks;

namespace Tape;

/// <summary>
/// Parses a TZX (ZX Spectrum) or TSX (MSX) tape file into a structured list of
/// <see cref="TapeBlock"/> objects.
///
/// TZX block size reference: https://worldofspectrum.net/TZXformat.html
/// Each block ID has a fixed or variable-length payload defined in the spec.
/// Unknown blocks with a 4-byte length prefix are skipped gracefully.
/// </summary>
public class TapeFile
{
    private const string TzxSignature = "ZXTape!";
    private const string TsxSignature = "MSXTape!";

    /// <summary>Which machine this tape belongs to.</summary>
    public TapeSystem System { get; internal set; } = TapeSystem.Unknown;

    /// <summary>File format major version.</summary>
    public byte VersionMajor { get; internal set; }

    /// <summary>File format minor version.</summary>
    public byte VersionMinor { get; internal set; }

    /// <summary>Ordered list of parsed tape blocks.</summary>
    public List<TapeBlock> Blocks { get; } = new();

    /// <summary>Load and parse a .tap, .tzx, .tsx, or .cas file from disk.</summary>
    public static TapeFile Load(string path)
    {
        using var stream = File.OpenRead(path);

        Tape.Loaders.ITapeLoader loader;
        if (path.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
        {
            loader = new Tape.Loaders.TapLoader();
        }
        else if (path.EndsWith(".cas", StringComparison.OrdinalIgnoreCase))
        {
            loader = new Tape.Loaders.CasLoader();
        }
        else
        {
            loader = new Tape.Loaders.TzxLoader();
        }

        var tape = new TapeFile();
        loader.Load(stream, tape);

        if (path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".cas", StringComparison.OrdinalIgnoreCase))
        {
            tape.System = TapeSystem.Msx;
        }

        return tape;
    }

    /// <summary>Load and parse from an arbitrary stream. Assumes TZX/TSX format.</summary>
    public static TapeFile Load(Stream stream)
    {
        var tape = new TapeFile();
        var loader = new Tape.Loaders.TzxLoader();
        loader.Load(stream, tape);
        return tape;
    }

    /// <summary>Load and parse a tape from a byte buffer. File name (with extension) is
    /// used to select the right loader and to assign the machine system when the
    /// format is ambiguous from the bytes alone (.tsx, .cas).</summary>
    public static TapeFile Load(byte[] bytes, string fileName)
    {
        Tape.Loaders.ITapeLoader loader;
        if (fileName.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
        {
            loader = new Tape.Loaders.TapLoader();
        }
        else if (fileName.EndsWith(".cas", StringComparison.OrdinalIgnoreCase))
        {
            loader = new Tape.Loaders.CasLoader();
        }
        else
        {
            loader = new Tape.Loaders.TzxLoader();
        }

        var tape = new TapeFile();
        using var stream = new MemoryStream(bytes);
        loader.Load(stream, tape);

        if (fileName.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".cas", StringComparison.OrdinalIgnoreCase))
        {
            tape.System = TapeSystem.Msx;
        }

        return tape;
    }
}
