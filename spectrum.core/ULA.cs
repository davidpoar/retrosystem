using System;
using System.Collections.Generic;

namespace retro.spectrum;

public class ULA
{
    private readonly SpectrumMemoryBus memoryBus;

    public int FlashFrameCounter { get; private set; } = 0;
    public bool FlashState { get; private set; } = false;
    public byte BorderColor { get; private set; } = 7;
    public int FrameStartCycles { get; set; } = 0;

    private readonly List<(int Cycle, byte Color)> borderEvents = new List<(int Cycle, byte Color)>();

    public static readonly uint[] ColorPalette = new uint[16] {
        0xFF000000, 0xFFC80000, 0xFF0000C8, 0xFFC800C8,
        0xFF00C800, 0xFFC8C800, 0xFF00C8C8, 0xFFC8C8C8,
        0xFF000000, 0xFFFF0000, 0xFF0000FF, 0xFFFF00FF,
        0xFF00FF00, 0xFFFFFF00, 0xFF00FFFF, 0xFFFFFFFF
    };

    public ULA(SpectrumMemoryBus memoryBus)
    {
        this.memoryBus = memoryBus;
    }

    public void OnFrameStart(int cycle)
    {
        FlashFrameCounter++;
        if (FlashFrameCounter >= 25)
        {
            FlashFrameCounter = 0;
            FlashState = !FlashState;
        }

        FrameStartCycles = cycle;
        if (memoryBus != null) memoryBus.FrameStartCycles = FrameStartCycles;

        borderEvents.Clear();
        borderEvents.Add((0, BorderColor));
    }

    public void UpdateBorderColor(byte newColor, int cycle)
    {
        if (borderEvents.Count == 0 || borderEvents[borderEvents.Count - 1].Color != newColor)
        {
            int relativeCycle = cycle - FrameStartCycles;
            borderEvents.Add((relativeCycle, newColor));
        }
        BorderColor = newColor;
    }

    public byte GetBorderColorAtCycle(int targetCycle)
    {
        if (borderEvents.Count == 0)
            return BorderColor;

        int low = 0;
        int high = borderEvents.Count - 1;
        int ans = 0;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (borderEvents[mid].Cycle <= targetCycle)
            {
                ans = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return borderEvents[ans].Color;
    }

    public void RenderScreen(uint[] textureBuffer)
    {
        for (int y = 0; y < 256; y++)
        {
            int ulaLine = y + 32;
            int lineCycleBase = ulaLine * 224;

            for (int x = 0; x < 320; x++)
            {
                int pixelCycle = lineCycleBase + 8 + x / 2;

                if (y < 32 || y >= 224 || x < 32 || x >= 288)
                {
                    byte borderCol = GetBorderColorAtCycle(pixelCycle);
                    textureBuffer[y * 320 + x] = ColorPalette[borderCol & 0x07];
                }
                else
                {
                    int screenX = x - 32;
                    int screenY = y - 32;

                    int charRow = screenY / 8;
                    int pixelOffset = ((screenY & 0xC0) << 5) | ((screenY & 0x07) << 8) | ((screenY & 0x38) << 2);
                    int charCol = screenX / 8;

                    int dummyCycles = pixelCycle;
                    byte attr = memoryBus.Read((ushort)(0x5800 + (charRow * 32) + charCol), ref dummyCycles);

                    byte ink = (byte)(attr & 0x07);
                    byte paper = (byte)((attr >> 3) & 0x07);
                    bool bright = (attr & 0x40) != 0;
                    bool flash = (attr & 0x80) != 0;

                    byte pixelByte = memoryBus.Read((ushort)(0x4000 + pixelOffset + charCol), ref dummyCycles);
                    int bitIndex = 7 - (screenX & 0x07);
                    bool isForeground = (pixelByte & (1 << bitIndex)) != 0;

                    if (flash && FlashState)
                    {
                        isForeground = !isForeground;
                    }

                    byte colorIndex = isForeground ? ink : paper;
                    if (bright && colorIndex != 0)
                    {
                        colorIndex += 8;
                    }

                    textureBuffer[y * 320 + x] = ColorPalette[colorIndex];
                }
            }
        }
    }
}
