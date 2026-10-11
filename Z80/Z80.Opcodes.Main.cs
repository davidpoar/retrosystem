using System;

namespace Z80;

public partial class Z80
{
    private void InitializeOpcodeTable()
    {
        // 1. Inicializar con UnknownOpcode por defecto
        for (int i = 0; i < 256; i++)
        {
            opcodes[i] = UnknownOpcode;
        }

        // 2. Mapear NOP y HALT
        opcodes[0x00] = NOP;
        opcodes[0x76] = HALT;

        // 3. Mapear prefijos
        opcodes[0xCB] = ExecuteCB;
        opcodes[0xED] = ExecuteED;
        opcodes[0xDD] = ExecuteDD;
        opcodes[0xFD] = ExecuteFD;

        // 4. Mapear LD r, r' (63 opcodes)
        for (int op = 0x40; op <= 0x7F; op++)
        {
            if (op == 0x76) continue;
            int dest = (op >> 3) & 0x07;
            int src = op & 0x07;
            int targetDest = dest;
            int targetSrc = src;

            opcodes[op] = () => {
                if (targetDest == 6 || targetSrc == 6)
                {
                    int tempMode = indexMode;
                    if (targetSrc != 6) indexMode = 0;
                    byte val = GetRegister8(targetSrc);
                    indexMode = (targetDest == 6) ? tempMode : 0;
                    SetRegister8(targetDest, val);
                    indexMode = tempMode;
                }
                else
                {
                    SetRegister8(targetDest, GetRegister8(targetSrc));
                }
                cycles += (targetDest == 6 || targetSrc == 6) ? 7 : 4;
            };
        }

        // 5. Mapear LD r, n (8 opcodes)
        for (int reg = 0; reg < 8; reg++)
        {
            int op = (reg << 3) | 0x06;
            int targetReg = reg;

            opcodes[op] = () => {
                byte value = Fetch8();
                SetRegister8(targetReg, value);
                cycles += (targetReg == 6) ? 10 : 7;
            };
        }

        // 6. Mapear cargas de 16 bits
        opcodes[0x01] = () => { BC = Fetch16(); cycles += 10; };
        opcodes[0x11] = () => { DE = Fetch16(); cycles += 10; };
        opcodes[0x21] = () => { HL = Fetch16(); cycles += 10; };
        opcodes[0x31] = () => { SP = Fetch16(); cycles += 10; };

        // 7. Cargas indirectas de A
        opcodes[0x02] = () => { WriteMemory(BC, A); cycles += 7; };
        opcodes[0x12] = () => { WriteMemory(DE, A); cycles += 7; };
        opcodes[0x0A] = () => { A = ReadMemory(BC); cycles += 7; };
        opcodes[0x1A] = () => { A = ReadMemory(DE); cycles += 7; };

        // 8. Cargas directas de memoria (absolutas)
        opcodes[0x22] = () => { ushort addr = Fetch16(); WriteMemory(addr, L); WriteMemory((ushort)(addr + 1), H); cycles += 16; };
        opcodes[0x2A] = () => { ushort addr = Fetch16(); L = ReadMemory(addr); H = ReadMemory((ushort)(addr + 1)); cycles += 16; };
        opcodes[0x32] = () => { ushort addr = Fetch16(); WriteMemory(addr, A); cycles += 13; };
        opcodes[0x3A] = () => { ushort addr = Fetch16(); A = ReadMemory(addr); cycles += 13; };

        // 9. Cargas SP
        opcodes[0xF9] = () => { SP = HL; cycles += 6; };

        // 10. Operaciones PUSH y POP
        opcodes[0xC5] = () => { Push16(BC); cycles += 11; };
        opcodes[0xD5] = () => { Push16(DE); cycles += 11; };
        opcodes[0xE5] = () => { Push16(HL); cycles += 11; };
        opcodes[0xF5] = () => { Push16(AF); cycles += 11; };

        opcodes[0xC1] = () => { BC = Pop16(); cycles += 10; };
        opcodes[0xD1] = () => { DE = Pop16(); cycles += 10; };
        opcodes[0xE1] = () => { HL = Pop16(); cycles += 10; };
        opcodes[0xF1] = () => { AF = Pop16(); cycles += 10; };

        // 11. Mapear ALU de 8 bits con registro (64 opcodes: ADD, ADC, SUB, SBC, AND, XOR, OR, CP)
        for (int reg = 0; reg < 8; reg++)
        {
            int r = reg;
            opcodes[0x80 + r] = () => { Add8(GetRegister8(r), false); cycles += (r == 6) ? 7 : 4; };
            opcodes[0x88 + r] = () => { Add8(GetRegister8(r), FlagC); cycles += (r == 6) ? 7 : 4; };
            opcodes[0x90 + r] = () => { Sub8(GetRegister8(r), false); cycles += (r == 6) ? 7 : 4; };
            opcodes[0x98 + r] = () => { Sub8(GetRegister8(r), FlagC); cycles += (r == 6) ? 7 : 4; };
            opcodes[0xA0 + r] = () => { And8(GetRegister8(r));         cycles += (r == 6) ? 7 : 4; };
            opcodes[0xA8 + r] = () => { Xor8(GetRegister8(r));         cycles += (r == 6) ? 7 : 4; };
            opcodes[0xB0 + r] = () => { Or8(GetRegister8(r));          cycles += (r == 6) ? 7 : 4; };
            opcodes[0xB8 + r] = () => { Cp8(GetRegister8(r));          cycles += (r == 6) ? 7 : 4; };
        }

        // 12. ALU de 8 bits inmediatas (8 opcodes)
        opcodes[0xC6] = () => { Add8(Fetch8(), false); cycles += 7; };
        opcodes[0xCE] = () => { Add8(Fetch8(), FlagC); cycles += 7; };
        opcodes[0xD6] = () => { Sub8(Fetch8(), false); cycles += 7; };
        opcodes[0xDE] = () => { Sub8(Fetch8(), FlagC); cycles += 7; };
        opcodes[0xE6] = () => { And8(Fetch8());         cycles += 7; };
        opcodes[0xEE] = () => { Xor8(Fetch8());         cycles += 7; };
        opcodes[0xF6] = () => { Or8(Fetch8());          cycles += 7; };
        opcodes[0xFE] = () => { Cp8(Fetch8());          cycles += 7; };

        // 13. Incrementar / Decrementar registros de 8 bits (16 opcodes)
        for (int reg = 0; reg < 8; reg++)
        {
            int r = reg;
            opcodes[(r << 3) | 0x04] = () => {
                byte val = GetRegister8(r);
                byte res = (byte)(val + 1);
                SetRegister8(r, res);
                FlagS = (res & 0x80) != 0;
                FlagZ = res == 0;
                FlagH = (val & 0x0F) == 0x0F;
                FlagPV = val == 0x7F;
                FlagN = false;
                cycles += (r == 6) ? 11 : 4;
            };

            opcodes[(r << 3) | 0x05] = () => {
                byte val = GetRegister8(r);
                byte res = (byte)(val - 1);
                SetRegister8(r, res);
                FlagS = (res & 0x80) != 0;
                FlagZ = res == 0;
                FlagH = (val & 0x0F) == 0x00;
                FlagPV = val == 0x80;
                FlagN = true;
                cycles += (r == 6) ? 11 : 4;
            };
        }

        // 14. Aritmética de 16 bits
        opcodes[0x09] = () => { AddHL(BC); cycles += 11; };
        opcodes[0x19] = () => { AddHL(DE); cycles += 11; };
        opcodes[0x29] = () => { AddHL(HL); cycles += 11; };
        opcodes[0x39] = () => { AddHL(SP); cycles += 11; };

        opcodes[0x03] = () => { BC++; cycles += 6; };
        opcodes[0x13] = () => { DE++; cycles += 6; };
        opcodes[0x23] = () => { HL++; cycles += 6; };
        opcodes[0x33] = () => { SP++; cycles += 6; };

        opcodes[0x0B] = () => { BC--; cycles += 6; };
        opcodes[0x1B] = () => { DE--; cycles += 6; };
        opcodes[0x2B] = () => { HL--; cycles += 6; };
        opcodes[0x3B] = () => { SP--; cycles += 6; };

        // 15. Saltos incondicionales y condicionales
        opcodes[0xC3] = () => { PC = Fetch16(); cycles += 10; };
        opcodes[0xE9] = () => { PC = HL; cycles += 4; };

        // JP CC, nn (8 opcodes)
        for (int cond = 0; cond < 8; cond++)
        {
            int c = cond;
            opcodes[0xC2 + (c * 8)] = () => {
                ushort addr = Fetch16();
                if (CheckCondition(c)) PC = addr;
                cycles += 10;
            };
        }

        // DJNZ e (0x10)
        opcodes[0x10] = () => {
            sbyte offset = (sbyte)Fetch8();
            B--;
            if (B != 0)
            {
                PC = (ushort)(PC + offset);
                cycles += 13;
            }
            else
            {
                cycles += 8;
            }
        };

        // JR e (Salto relativo incondicional)
        opcodes[0x18] = () => {
            sbyte offset = (sbyte)Fetch8();
            PC = (ushort)(PC + offset);
            cycles += 12;
        };

        // JR CC, e (Salto relativo condicional - 4 opcodes: NZ, Z, NC, C)
        for (int cond = 0; cond < 4; cond++)
        {
            int c = cond;
            opcodes[0x20 + (c * 8)] = () => {
                sbyte offset = (sbyte)Fetch8();
                if (CheckCondition(c))
                {
                    PC = (ushort)(PC + offset);
                    cycles += 12;
                }
                else
                {
                    cycles += 7;
                }
            };
        }

        // 16. Llamadas y retornos de subrutinas
        opcodes[0xCD] = () => {
            ushort addr = Fetch16();
            Push16(PC);
            PC = addr;
            cycles += 17;
        };

        // CALL CC, nn (8 opcodes)
        for (int cond = 0; cond < 8; cond++)
        {
            int c = cond;
            opcodes[0xC4 + (c * 8)] = () => {
                ushort addr = Fetch16();
                if (CheckCondition(c))
                {
                    Push16(PC);
                    PC = addr;
                    cycles += 17;
                }
                else
                {
                    cycles += 10;
                }
            };
        }

        // RET
        opcodes[0xC9] = () => { PC = Pop16(); cycles += 10; };

        // RET CC (8 opcodes)
        for (int cond = 0; cond < 8; cond++)
        {
            int c = cond;
            opcodes[0xC0 + (c * 8)] = () => {
                if (CheckCondition(c))
                {
                    PC = Pop16();
                    cycles += 11;
                }
                else
                {
                    cycles += 5;
                }
            };
        }

        // RST p (Reinicios rápidos en página 0 - 8 opcodes)
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            opcodes[0xC7 + (index * 8)] = () => {
                Push16(PC);
                PC = (ushort)(index * 8);
                cycles += 11;
            };
        }

        // 17. Rotaciones del acumulador
        opcodes[0x07] = RLCA;
        opcodes[0x0F] = RRCA;
        opcodes[0x17] = RLA;
        opcodes[0x1F] = RRA;

        // 18. Ajuste decimal y banderas
        opcodes[0x27] = DAA;
        opcodes[0x2F] = () => { A = (byte)~A; FlagH = true; FlagN = true; cycles += 4; };
        opcodes[0x37] = () => { FlagH = false; FlagN = false; FlagC = true; cycles += 4; };
        opcodes[0x3F] = () => { FlagH = FlagC; FlagN = false; FlagC = !FlagC; cycles += 4; };

        // 19. Intercambios
        opcodes[0xEB] = () => { ushort tmp = DE; DE = HL; HL = tmp; cycles += 4; };
        opcodes[0x08] = () => { ushort tmp = AF; AF = AF_prime; AF_prime = tmp; cycles += 4; };
        opcodes[0xD9] = () => {
            ushort tmpBC = BC; BC = BC_prime; BC_prime = tmpBC;
            ushort tmpDE = DE; DE = DE_prime; DE_prime = tmpDE;
            ushort tmpHL = HL; HL = HL_prime; HL_prime = tmpHL;
            cycles += 4;
        };
        opcodes[0xE3] = () => {
            byte low = ReadMemory(SP);
            byte high = ReadMemory((ushort)(SP + 1));
            WriteMemory(SP, L);
            WriteMemory((ushort)(SP + 1), H);
            L = low;
            H = high;
            cycles += 19;
        };

        // 20. Control de interrupciones
        opcodes[0xF3] = () => { IFF1 = false; IFF2 = false; cycles += 4; };
        opcodes[0xFB] = () => { IFF1 = true; IFF2 = true; cycles += 4; };

        // 21. Entrada / Salida (I/O)
        opcodes[0xD3] = OUT_N_A;
        opcodes[0xDB] = IN_A_N;
    }

    // --- Auxiliares Aritméticos y Lógicos (ALU) ---

    private void Add8(byte value, bool carry)
    {
        int c = carry ? 1 : 0;
        int result = A + value + c;

        bool h = ((A & 0x0F) + (value & 0x0F) + c) > 0x0F;
        bool pv = ((A ^ result) & (value ^ result) & 0x80) != 0;

        A = (byte)result;

        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagH = h;
        FlagPV = pv;
        FlagN = false;
        FlagC = result > 0xFF;
    }

    private void Sub8(byte value, bool carry)
    {
        int c = carry ? 1 : 0;
        int result = A - value - c;

        bool h = ((A & 0x0F) - (value & 0x0F) - c) < 0;
        bool pv = ((A ^ value) & 0x80) != 0 && ((A ^ result) & 0x80) != 0;

        A = (byte)result;

        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagH = h;
        FlagPV = pv;
        FlagN = true;
        FlagC = result < 0;
    }

    private void And8(byte value)
    {
        A &= value;
        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagH = true; // AND en Z80 siempre setea H en 1
        FlagPV = GetParity(A);
        FlagN = false;
        FlagC = false;
    }

    private void Or8(byte value)
    {
        A |= value;
        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagH = false;
        FlagPV = GetParity(A);
        FlagN = false;
        FlagC = false;
    }

    private void Xor8(byte value)
    {
        A ^= value;
        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagH = false;
        FlagPV = GetParity(A);
        FlagN = false;
        FlagC = false;
    }

    private void Cp8(byte value)
    {
        byte tempA = A;
        Sub8(value, false);
        A = tempA;
    }

    private void AddHL(ushort value)
    {
        int result = HL + value;
        bool h = ((HL & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF;

        FlagH = h;
        FlagN = false;
        FlagC = result > 0xFFFF;

        HL = (ushort)result;
    }

    // --- Auxiliares del Stack (Pila) ---

    private void Push16(ushort val)
    {
        SP -= 2;
        WriteMemory((ushort)(SP + 1), (byte)(val >> 8));
        WriteMemory(SP, (byte)(val & 0xFF));
    }

    private ushort Pop16()
    {
        byte low = ReadMemory(SP);
        byte high = ReadMemory((ushort)(SP + 1));
        SP += 2;
        return (ushort)((high << 8) | low);
    }

    // --- Control Condicional ---

    private bool CheckCondition(int condition) => condition switch
    {
        0 => !FlagZ,  // NZ
        1 => FlagZ,   // Z
        2 => !FlagC,  // NC
        3 => FlagC,   // C
        4 => !FlagPV, // PO
        5 => FlagPV,  // PE
        6 => !FlagS,  // P
        7 => FlagS,   // M
        _ => false
    };

    // --- Rotaciones del Acumulador ---

    private void RLCA()
    {
        byte carry = (byte)((A & 0x80) >> 7);
        A = (byte)((A << 1) | carry);
        FlagH = false;
        FlagN = false;
        FlagC = carry != 0;
        cycles += 4;
    }

    private void RRCA()
    {
        byte carry = (byte)(A & 0x01);
        A = (byte)((A >> 1) | (carry << 7));
        FlagH = false;
        FlagN = false;
        FlagC = carry != 0;
        cycles += 4;
    }

    private void RLA()
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        byte newCarry = (byte)((A & 0x80) >> 7);
        A = (byte)((A << 1) | oldCarry);
        FlagH = false;
        FlagN = false;
        FlagC = newCarry != 0;
        cycles += 4;
    }

    private void RRA()
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        byte newCarry = (byte)(A & 0x01);
        A = (byte)((A >> 1) | (oldCarry << 7));
        FlagH = false;
        FlagN = false;
        FlagC = newCarry != 0;
        cycles += 4;
    }

    private void DAA()
    {
        byte correction = 0;
        bool setCarry = false;

        if (FlagH || (!FlagN && (A & 0x0F) > 9))
        {
            correction |= 0x06;
        }
        if (FlagC || (!FlagN && A > 0x99))
        {
            correction |= 0x60;
            setCarry = true;
        }

        int result = FlagN ? (A - correction) : (A + correction);

        FlagH = (!FlagN && (A & 0x0F) > 9) || (FlagN && FlagH && (A & 0x0F) < 6);
        A = (byte)result;

        FlagS = (A & 0x80) != 0;
        FlagZ = A == 0;
        FlagPV = GetParity(A);
        FlagC = setCarry;
        cycles += 4;
    }

    private void UnknownOpcode() => throw new Exception($"Opcode no implementado: {ReadMemory((ushort)(PC-1)):X2}");

    private void NOP() => cycles += 4;

    private void LDBCNN()
    {
        BC = Fetch16();
        cycles += 10;
    }

    private void LD_BC_A()
    {
        WriteMemory(BC, A);
        cycles += 7;
    }

    private void HALT()
    {
        Halted = true;
        // El PC no se modifica; queda apuntando a la instrucción HALT.
        // Cuando se acepta una interrupción, la dirección PUSHed es la del
        // propio HALT, por lo que tras RETI/RETN la CPU vuelve a ejecutar
        // el HALT. Para que reanude la ejecución de la instrucción
        // SIGUIENTE al HALT, el emulador debe incrementar el PC antes de
        // despachar la interrupción (ver TestCpuHaltAndInterruptWorkaround
        // en SpectrumTests.cs como referencia).
        cycles += 4;
    }

    private void OUT_N_A()
    {
        byte port = Fetch8();
        if ((port & 1) == 0)
        {
            int tempCycles = cycles + 8;
            MemoryBus.Read(0x4000, ref tempCycles); // 0x4000 for ULA contention
            cycles = tempCycles - 8;
        }
        cycles += 11;
        OnOutputPort?.Invoke(port, A, cycles - 3); // M3 I/O write happens approx 8 cycles in
    }

    private void IN_A_N()
    {
        byte port = Fetch8();
        if ((port & 1) == 0)
        {
            int tempCycles = cycles + 8;
            MemoryBus.Read(0x4000, ref tempCycles);
            cycles = tempCycles - 8;
        }
        cycles += 11;
        A = OnInputPort?.Invoke(port, cycles - 3) ?? 0xFF; // M3 I/O read happens approx 8 cycles in
    }
}
