namespace Z80;

public partial class Z80
{
    private byte[] ram = new byte[65536];
    // Registros Especiales y de Control
    public ushort PC;
    public ushort SP;
    public ushort IX;
    public ushort IY;
    public byte I;
    public byte R;

    // Registros principales
    public byte A, F;
    public byte B, C;
    public byte D, E;
    public byte H, L;

    // Registros alternativos (shadow)
    public byte A_prime, F_prime;
    public byte B_prime, C_prime;
    public byte D_prime, E_prime;
    public byte H_prime, L_prime;

    // Propiedades combinadas de 16 bits (principales)
    public ushort AF 
    {
        get => (ushort)((A << 8) | F);
        set { A = (byte)(value >> 8); F = (byte)(value & 0xFF); }
    }
    public ushort BC 
    { 
        get => (ushort)((B << 8) | C); 
        set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
    }
    public ushort DE 
    { 
        get => (ushort)((D << 8) | E); 
        set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
    }
    public ushort HL 
    { 
        get => (ushort)((H << 8) | L); 
        set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
    }
    
    // Propiedades combinadas de 16 bits (alternativos)
    public ushort AF_prime 
    {
        get => (ushort)((A_prime << 8) | F_prime);
        set { A_prime = (byte)(value >> 8); F_prime = (byte)(value & 0xFF); }
    }
    public ushort BC_prime 
    { 
        get => (ushort)((B_prime << 8) | C_prime); 
        set { B_prime = (byte)(value >> 8); C_prime = (byte)(value & 0xFF); }
    }
    public ushort DE_prime 
    { 
        get => (ushort)((D_prime << 8) | E_prime); 
        set { D_prime = (byte)(value >> 8); E_prime = (byte)(value & 0xFF); }
    }
    public ushort HL_prime 
    { 
        get => (ushort)((H_prime << 8) | L_prime); 
        set { H_prime = (byte)(value >> 8); L_prime = (byte)(value & 0xFF); }
    }
    
    // Máscaras de bits para el registro de banderas (F)
    private const byte FlagMaskS  = 0x80; // Sign Flag (Bit 7)
    private const byte FlagMaskZ  = 0x40; // Zero Flag (Bit 6)
    private const byte FlagMaskY  = 0x20; // B5 indocumentado (Bit 5)
    private const byte FlagMaskH  = 0x10; // Half Carry Flag (Bit 4)
    private const byte FlagMaskX  = 0x08; // B3 indocumentado (Bit 3)
    private const byte FlagMaskPV = 0x04; // Parity or Overflow Flag (Bit 2)
    private const byte FlagMaskN  = 0x02; // Add/Subtract Flag (Bit 1)
    private const byte FlagMaskC  = 0x01; // Carry Flag (Bit 0)

    // Propiedades de acceso rápido a las banderas
    public bool FlagS
    {
        get => (F & FlagMaskS) != 0;
        set => F = (byte)(value ? (F | FlagMaskS) : (F & ~FlagMaskS));
    }

    public bool FlagZ
    {
        get => (F & FlagMaskZ) != 0;
        set => F = (byte)(value ? (F | FlagMaskZ) : (F & ~FlagMaskZ));
    }

    public bool FlagY
    {
        get => (F & FlagMaskY) != 0;
        set => F = (byte)(value ? (F | FlagMaskY) : (F & ~FlagMaskY));
    }

    public bool FlagH
    {
        get => (F & FlagMaskH) != 0;
        set => F = (byte)(value ? (F | FlagMaskH) : (F & ~FlagMaskH));
    }

    public bool FlagX
    {
        get => (F & FlagMaskX) != 0;
        set => F = (byte)(value ? (F | FlagMaskX) : (F & ~FlagMaskX));
    }

    public bool FlagPV
    {
        get => (F & FlagMaskPV) != 0;
        set => F = (byte)(value ? (F | FlagMaskPV) : (F & ~FlagMaskPV));
    }

    public bool FlagN
    {
        get => (F & FlagMaskN) != 0;
        set => F = (byte)(value ? (F | FlagMaskN) : (F & ~FlagMaskN));
    }

    public bool FlagC
    {
        get => (F & FlagMaskC) != 0;
        set => F = (byte)(value ? (F | FlagMaskC) : (F & ~FlagMaskC));
    }
    
    // Propiedades adicionales
    public bool Halted { get; private set; }
    public byte InterruptMode { get; private set; }
    public bool IFF1 { get; set; }
    public bool IFF2 { get; set; }
    public bool InterruptPending { get; set; }
    public int InterruptsServiced { get; set; }
    public int Cycles => cycles;

    // Tabla de acciones para cada uno de los 256 opcodes posibles
    private readonly Action[] opcodes;

    // Delegados para Entrada/Salida (I/O) de puertos
    public delegate byte InputDelegate(byte port, int currentCycles);
    public delegate void OutputDelegate(byte port, byte value, int currentCycles);

    public InputDelegate? OnInputPort;
    public OutputDelegate? OnOutputPort;

    public Z80()
    {
        opcodes = new Action[256];
        InitializeOpcodeTable();
        MemoryBus = new LegacyMemoryBus(this);
        Reset();
    }
    
    public void Reset()
    {
        PC = 0;
        SP = 0xFFFF; // Estado inicial común para el puntero de pila
        IX = 0;
        IY = 0;
        I = 0;
        R = 0;
        cycles = 0;
        Halted = false;
        InterruptMode = 0;
        IFF1 = false;
        IFF2 = false;
        InterruptPending = false;
        SlotRegister = 0;
        InterruptsServiced = 0;
    }
    private int cycles = 0;
    public void Run()
    {
        Halted = false;
        while (!Halted) 
        {
            byte opcode = ReadMemory(PC++);
            opcodes[opcode]();
        }
    }

    public int Step()
    {
        int startCycles = cycles;
        if (InterruptPending && IFF1)
        {
            InterruptsServiced++;
            InterruptPending = false;
            IFF1 = false;
            IFF2 = false;
            Halted = false;

            if (InterruptMode == 1 || InterruptMode == 0)
            {
                Push16(PC);
                PC = 0x0038;
                cycles += 13;
            }
            else if (InterruptMode == 2)
            {
                Push16(PC);
                byte dataBus = 0xFF; // En ZX Spectrum el bus de datos suele leer 0xFF si ningún dispositivo responde
                ushort vectorAddress = (ushort)((I << 8) | dataBus);
                byte low = ReadMemory(vectorAddress);
                byte high = ReadMemory((ushort)(vectorAddress + 1));
                PC = (ushort)((high << 8) | low);
                cycles += 19;
            }
            return cycles - startCycles;
        }

        if (Halted) return 0;
        byte opcode = Fetch8();
        opcodes[opcode]();
        return cycles - startCycles;
    }

    // Métodos para cargar programas y acceder a memoria
    public IMemoryBus MemoryBus { get; set; } = null!;
    public bool EnableSlotMapping { get; set; } = false;
    public byte[] Bios = new byte[32768];
    public byte[] Cartridge = new byte[32768];
    public byte SlotRegister = 0;

    public void LoadBios(byte[] data)
    {
        Array.Clear(Bios, 0, Bios.Length);
        Array.Copy(data, Bios, Math.Min(data.Length, Bios.Length));
    }

    public void LoadCartridge(byte[] data)
    {
        Array.Clear(Cartridge, 0, Cartridge.Length);
        Array.Copy(data, Cartridge, Math.Min(data.Length, Cartridge.Length));
    }

    public byte ReadRamDirect(ushort address) => ram[address];
    public void WriteRamDirect(ushort address, byte value) => ram[address] = value;

    public byte ReadMemory(ushort address)
    {
        // Data read usually happens at M2/M3, approx T+3
        int tempCycles = cycles + 3;
        byte result = MemoryBus.Read(address, ref tempCycles);
        cycles = tempCycles - 3;
        return result;
    }

    public void WriteMemory(ushort address, byte value)
    {
        // Data write usually happens at M2/M3, approx T+3
        int tempCycles = cycles + 3;
        MemoryBus.Write(address, value, ref tempCycles);
        cycles = tempCycles - 3;
    }
    
    public void LoadProgram(ushort address, byte[] code)
    {
        for (int i = 0; i < code.Length; i++)
        {
            ram[(ushort)(address + i)] = code[i];
        }
    }

    // Métodos auxiliares para la decodificación de opcodes
    public byte Fetch8()
    {
        // Opcode fetch (M1) contention is evaluated at T+1
        int tempCycles = cycles + 1;
        byte result = MemoryBus.Read(PC++, ref tempCycles);
        cycles = tempCycles - 1;
        return result;
    }
    
    public ushort Fetch16()
    {
        byte low = ReadMemory(PC++);
        byte high = ReadMemory(PC++);
        return (ushort)((high << 8) | low);
    }

    private int indexMode = 0; // 0: None, 1: IX, 2: IY
    private sbyte indexDisplacement = 0;
    private bool isDisplacementFetched = false;

    public byte IXh
    {
        get => (byte)(IX >> 8);
        set => IX = (ushort)((value << 8) | (IX & 0xFF));
    }
    public byte IXl
    {
        get => (byte)(IX & 0xFF);
        set => IX = (ushort)((IX & 0xFF00) | value);
    }
    public byte IYh
    {
        get => (byte)(IY >> 8);
        set => IY = (ushort)((value << 8) | (IY & 0xFF));
    }
    public byte IYl
    {
        get => (byte)(IY & 0xFF);
        set => IY = (ushort)((IY & 0xFF00) | value);
    }

    private byte ReadMemoryIndex()
    {
        if (indexMode == 0)
        {
            return ReadMemory(HL);
        }
        else if (indexMode == 1)
        {
            if (!isDisplacementFetched)
            {
                indexDisplacement = (sbyte)Fetch8();
                isDisplacementFetched = true;
            }
            return ReadMemory((ushort)(IX + indexDisplacement));
        }
        else // indexMode == 2
        {
            if (!isDisplacementFetched)
            {
                indexDisplacement = (sbyte)Fetch8();
                isDisplacementFetched = true;
            }
            return ReadMemory((ushort)(IY + indexDisplacement));
        }
    }

    private void WriteMemoryIndex(byte value)
    {
        if (indexMode == 0)
        {
            WriteMemory(HL, value);
        }
        else if (indexMode == 1)
        {
            if (!isDisplacementFetched)
            {
                indexDisplacement = (sbyte)Fetch8();
                isDisplacementFetched = true;
            }
            WriteMemory((ushort)(IX + indexDisplacement), value);
        }
        else // indexMode == 2
        {
            if (!isDisplacementFetched)
            {
                indexDisplacement = (sbyte)Fetch8();
                isDisplacementFetched = true;
            }
            WriteMemory((ushort)(IY + indexDisplacement), value);
        }
    }

    public byte GetRegister8(int index) => index switch
    {
        0 => B,
        1 => C,
        2 => D,
        3 => E,
        4 => (indexMode == 1) ? IXh : ((indexMode == 2) ? IYh : H),
        5 => (indexMode == 1) ? IXl : ((indexMode == 2) ? IYl : L),
        6 => ReadMemoryIndex(),
        7 => A,
        _ => 0
    };

    public void SetRegister8(int index, byte value)
    {
        switch (index)
        {
            case 0: B = value; break;
            case 1: C = value; break;
            case 2: D = value; break;
            case 3: E = value; break;
            case 4:
                if (indexMode == 1) IXh = value;
                else if (indexMode == 2) IYh = value;
                else H = value;
                break;
            case 5:
                if (indexMode == 1) IXl = value;
                else if (indexMode == 2) IYl = value;
                else L = value;
                break;
            case 6: WriteMemoryIndex(value); break;
            case 7: A = value; break;
        }
    }
}
