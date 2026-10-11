using System;
using Z80;

namespace retro.spectrum;

public class SpectrumMemoryBus : IMemoryBus
{
    private readonly byte[] rom = new byte[16384];
    private readonly byte[] ram = new byte[49152];

    public int FrameStartCycles { get; set; } = 0;

    public byte[] Ram => ram;
    public byte[] Rom => rom;

    public SpectrumMemoryBus(byte[] romData)
    {
        Array.Copy(romData, rom, Math.Min(romData.Length, rom.Length));
    }

    public byte Read(ushort address, ref int currentCycles)
    {
        ApplyContention(address, ref currentCycles);
        if (address < 16384)
        {
            return rom[address];
        }
        return ram[address - 16384];
    }

    public void Write(ushort address, byte value, ref int currentCycles)
    {
        ApplyContention(address, ref currentCycles);
        if (address < 16384)
        {
            return;
        }
        ram[address - 16384] = value;
    }

    private void ApplyContention(ushort address, ref int currentCycles)
    {
        if (address < 0x4000 || address >= 0x8000) return;

        int relativeCycle = currentCycles - FrameStartCycles;
        int normalizedCycle = relativeCycle - 14335;

        if (normalizedCycle < 0 || normalizedCycle >= (192 * 224)) return;

        int cycleInLine = normalizedCycle % 224;
        if (cycleInLine < 128)
        {
            int delayPatternIndex = cycleInLine % 8;
            int[] delays = { 6, 5, 4, 3, 2, 1, 0, 0 };
            currentCycles += delays[delayPatternIndex];
        }
    }
}
