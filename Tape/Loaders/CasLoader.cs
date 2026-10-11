using System;
using System.Collections.Generic;
using System.IO;
using Tape.Blocks;

namespace Tape.Loaders;

public class CasLoader : ITapeLoader
{
    private static readonly byte[] CasSignature = new byte[] { 0x1F, 0xA6, 0xDE, 0xBA, 0xCC, 0x13, 0x7D, 0x74 };

    public void Load(Stream stream, TapeFile tapeFile)
    {
        tapeFile.System = TapeSystem.Msx;
        tapeFile.VersionMajor = 1;
        tapeFile.VersionMinor = 0;

        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        var fileBytes = reader.ReadBytes((int)stream.Length);

        int i = 0;
        while (i < fileBytes.Length)
        {
            // Find signature
            int sigStart = IndexOf(fileBytes, CasSignature, i);
            if (sigStart == -1)
            {
                break; // No more blocks
            }

            // Find the next signature to determine the end of this block
            int nextSig = IndexOf(fileBytes, CasSignature, sigStart + CasSignature.Length);
            int blockEnd = nextSig == -1 ? fileBytes.Length : nextSig;
            
            // The block data EXCLUDES the 8-byte CAS signature — that is just a container marker.
            // Only the bytes AFTER the signature are actual MSX cassette data.
            int dataStart = sigStart + CasSignature.Length;
            int dataLength = blockEnd - dataStart;
            if (dataLength <= 0)
            {
                i = blockEnd;
                continue;
            }
            byte[] blockData = new byte[dataLength];
            Array.Copy(fileBytes, dataStart, blockData, 0, dataLength);

            // The first block (header) is short (~10-16 bytes of type+name).
            // Subsequent blocks are the actual program data.
            bool isHeader = blockData.Length <= 32;

            var kcsBlock = new SynthesizedMsxKansasCityBlock(
                data: blockData,
                pilotCount: (ushort)(isHeader ? 16000 : 4000)
            );
            tapeFile.Blocks.Add(kcsBlock);

            i = blockEnd;
        }
    }

    private int IndexOf(byte[] array, byte[] pattern, int startIndex)
    {
        if (pattern.Length == 0) return startIndex;
        for (int i = startIndex; i <= array.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }
}

/// <summary>
/// A helper class to synthesize a KansasCityBlock from raw bytes.
/// </summary>
public class SynthesizedMsxKansasCityBlock : MsxKansasCityBlock
{
    private byte[] _data;
    private ushort _pilotCount;

    public new byte[] Data => _data;

    public SynthesizedMsxKansasCityBlock(byte[] data, ushort pilotCount)
    {
        _data = data;
        _pilotCount = pilotCount;
    }

    public override byte BlockId => 0x4B;
    public override string Description => $"MSX .cas Block ({_data.Length} bytes)";
    public override void Parse(BinaryReader reader) { } // Not used

    public override IEnumerable<long> GeneratePulses(int cpuClockHz)
    {
        // Setup fields that base.GeneratePulses needs by injecting them via reflection or overriding.
        // Wait, MsxKansasCityBlock properties have `private set`.
        // It's better to just re-implement GeneratePulses here or set them via reflection.
        // For simplicity, we just generate the KCS pulses directly here.

        // En un MSX real, el bucle de TAPION tarda 43 T-states debido a los wait states de M1 en la ROM.
        // En nuestro emulador Z80 (sin wait states), el bucle tarda ~38 T-states.
        // Para que la BIOS mida correctamente la longitud del tono piloto (17.34 bucles por medio ciclo),
        // Compensated for Z80 emulator loop speed (34 cycles per loop instead of 43)
        // Original MSX values for 1200 baud
        long pulseOne = 746;
        long pulseZero = 1492;

        // 1. Leader tone (Pilot is a continuous stream of '1' bits)
        // A '1' bit is 2 full cycles (4 half-waves) of 2400Hz. 
        // So _pilotCount of 4000 means 4000 * 4 = 16000 half-waves.
        for (int i = 0; i < _pilotCount * 4; i++)
        {
            yield return pulseOne;
        }

        // Helper: emit pulses for a bit (0 or 1)
        IEnumerable<long> EmitBit(bool bit)
        {
            if (bit)
            {
                // '1' = 4 pulses (2 full cycles) of 2400 Hz
                for (int i = 0; i < 4; i++) yield return pulseOne;
            }
            else
            {
                // '0' = 2 pulses (1 full cycle) of 1200 Hz
                for (int i = 0; i < 2; i++) yield return pulseZero;
            }
        }

        // Helper: emit a single byte with 1 start bit, 8 data bits, 2 stop bits
        IEnumerable<long> EmitByte(byte b)
        {
            // Start bit (0)
            foreach (var p in EmitBit(false)) yield return p;
            // Data bits (LSB first)
            for (int bit = 0; bit < 8; bit++)
            {
                bool bitValue = ((b >> bit) & 1) == 1;
                foreach (var p in EmitBit(bitValue)) yield return p;
            }
            // Stop bits (1, 1)
            foreach (var p in EmitBit(true)) yield return p;
            foreach (var p in EmitBit(true)) yield return p;
        }

        // 2. Encode each byte
        foreach (byte b in _data)
        {
            foreach (var p in EmitByte(b)) yield return p;
        }

        // 3. Pause after block
        yield return (long)2000 * cpuClockHz / 1000; // 2 seconds pause
    }
}
