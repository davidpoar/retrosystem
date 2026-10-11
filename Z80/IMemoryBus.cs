namespace Z80;

public interface IMemoryBus
{
    byte Read(ushort address, ref int currentCycles);
    void Write(ushort address, byte value, ref int currentCycles);
}

public class LegacyMemoryBus : IMemoryBus
{
    private readonly Z80 cpu;
    
    public LegacyMemoryBus(Z80 cpu)
    {
        this.cpu = cpu;
    }

    public byte Read(ushort address, ref int currentCycles)
    {
        if (cpu.EnableSlotMapping)
        {
            int page = address >> 14;
            int slot = (cpu.SlotRegister >> (page * 2)) & 0x03;
            ushort offset = (ushort)(address & 0x3FFF);
            
            switch (slot)
            {
                case 0:
                    if (page == 0) return cpu.Bios[offset];
                    if (page == 1) return cpu.Bios[16384 + offset];
                    return 0xFF;
                case 1:
                    if (page == 1) return cpu.Cartridge[offset];
                    if (page == 2) return cpu.Cartridge[16384 + offset];
                    return 0xFF;
                case 3:
                    return cpu.ReadRamDirect(address);
                default:
                    return 0xFF;
            }
        }
        return cpu.ReadRamDirect(address);
    }

    public void Write(ushort address, byte value, ref int currentCycles)
    {
        if (cpu.EnableSlotMapping)
        {
            int page = address >> 14;
            int slot = (cpu.SlotRegister >> (page * 2)) & 0x03;
            if (slot == 3)
            {
                cpu.WriteRamDirect(address, value);
            }
            return;
        }
        cpu.WriteRamDirect(address, value);
    }
}
