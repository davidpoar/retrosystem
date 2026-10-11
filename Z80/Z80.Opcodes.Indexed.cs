using System;

namespace Z80;

public partial class Z80
{
    private void ExecuteED()
    {
        void OutC(byte val)
        {
            if ((C & 1) == 0)
            {
                int tempCycles = cycles + 9;
                MemoryBus.Read(0x4000, ref tempCycles); // 0x4000 for ULA contention
                cycles = tempCycles - 9;
            }
            OnOutputPort?.Invoke(C, val, cycles + 9); // M3 I/O write happens approx 9 cycles in
            cycles += 12;
        }

        byte InC()
        {
            if ((C & 1) == 0)
            {
                int tempCycles = cycles + 9;
                MemoryBus.Read(0x4000, ref tempCycles);
                cycles = tempCycles - 9;
            }
            byte val = OnInputPort?.Invoke(C, cycles + 9) ?? 0xFF; // M3 I/O read happens approx 9 cycles in
            FlagS = (val & 0x80) != 0;
            FlagZ = val == 0;
            FlagH = false;
            FlagPV = GetParity(val);
            FlagN = false;
            cycles += 12;
            return val;
        }

        void SbcHL(ushort val)
        {
            int hl = HL;
            int res = hl - val - (FlagC ? 1 : 0);
            
            FlagS = (res & 0x8000) != 0;
            FlagZ = (res & 0xFFFF) == 0;
            FlagH = (((hl ^ val ^ res) >> 12) & 1) != 0;
            FlagPV = (((hl ^ val) & 0x8000) != 0) && (((hl ^ res) & 0x8000) != 0);
            FlagN = true;
            FlagC = res < 0;
            
            HL = (ushort)res;
            cycles += 15;
        }

        void AdcHL(ushort val)
        {
            int hl = HL;
            int carry = FlagC ? 1 : 0;
            int res = hl + val + carry;
            
            FlagS = (res & 0x8000) != 0;
            FlagZ = (res & 0xFFFF) == 0;
            FlagH = (((hl ^ val ^ res) >> 12) & 1) != 0;
            FlagPV = (((hl ^ val) & 0x8000) == 0) && (((hl ^ res) & 0x8000) != 0);
            FlagN = false;
            FlagC = res > 0xFFFF;
            
            HL = (ushort)res;
            cycles += 15;
        }

        byte op = Fetch8();
        switch (op)
        {
            case 0xB0: // LDIR (Block Memory Copy)
                // HL apunta a origen, DE apunta a destino, BC es contador de bytes
                WriteMemory(DE, ReadMemory(HL));
                DE++;
                HL++;
                BC--;
                
                if (BC != 0)
                {
                    PC -= 2; // Repetir la instrucción LDIR (apuntar al prefijo ED 0xB0 de nuevo)
                    cycles += 21;
                }
                else
                {
                    cycles += 16;
                }
                break;

            case 0x46: // IM 0
                InterruptMode = 0;
                cycles += 8;
                break;

            case 0x56: // IM 1
                InterruptMode = 1;
                cycles += 8;
                break;

            case 0x5E: // IM 2
                InterruptMode = 2;
                cycles += 8;
                break;

            case 0x4B: // LD BC, (nn)
                {
                    ushort addr = Fetch16();
                    byte low = ReadMemory(addr);
                    byte high = ReadMemory((ushort)(addr + 1));
                    BC = (ushort)((high << 8) | low);
                    cycles += 20;
                }
                break;

            case 0x5B: // LD DE, (nn)
                {
                    ushort addr = Fetch16();
                    byte low = ReadMemory(addr);
                    byte high = ReadMemory((ushort)(addr + 1));
                    DE = (ushort)((high << 8) | low);
                    cycles += 20;
                }
                break;

            case 0x7B: // LD SP, (nn)
                {
                    ushort addr = Fetch16();
                    byte low = ReadMemory(addr);
                    byte high = ReadMemory((ushort)(addr + 1));
                    SP = (ushort)((high << 8) | low);
                    cycles += 20;
                }
                break;

            case 0x43: // LD (nn), BC
                {
                    ushort addr = Fetch16();
                    WriteMemory(addr, (byte)(BC & 0xFF));
                    WriteMemory((ushort)(addr + 1), (byte)(BC >> 8));
                    cycles += 20;
                }
                break;

            case 0x53: // LD (nn), DE
                {
                    ushort addr = Fetch16();
                    WriteMemory(addr, (byte)(DE & 0xFF));
                    WriteMemory((ushort)(addr + 1), (byte)(DE >> 8));
                    cycles += 20;
                }
                break;

            case 0x73: // LD (nn), SP
                {
                    ushort addr = Fetch16();
                    WriteMemory(addr, (byte)(SP & 0xFF));
                    WriteMemory((ushort)(addr + 1), (byte)(SP >> 8));
                    cycles += 20;
                }
                break;

            // OUT (C), r
            case 0x41: OutC(B); break;
            case 0x49: OutC(C); break;
            case 0x51: OutC(D); break;
            case 0x59: OutC(E); break;
            case 0x61: OutC(H); break;
            case 0x69: OutC(L); break;
            case 0x71: OutC(0); break;
            case 0x79: OutC(A); break;

            // IN r, (C)
            case 0x40: B = InC(); break;
            case 0x48: C = InC(); break;
            case 0x50: D = InC(); break;
            case 0x58: E = InC(); break;
            case 0x60: H = InC(); break;
            case 0x68: L = InC(); break;
            case 0x70: InC(); break;
            case 0x78: A = InC(); break;

            // NEG
            case 0x44:
            case 0x4C:
            case 0x54:
            case 0x5C:
            case 0x64:
            case 0x6C:
            case 0x74:
            case 0x7C:
                {
                    byte temp = A;
                    A = (byte)(0 - temp);
                    FlagS = (A & 0x80) != 0;
                    FlagZ = A == 0;
                    FlagH = (temp & 0x0F) != 0;
                    FlagPV = temp == 0x80;
                    FlagN = true;
                    FlagC = temp != 0;
                    cycles += 8;
                }
                break;

            // RETN / RETI
            case 0x45:
            case 0x4D:
                PC = Pop16();
                IFF1 = IFF2;
                cycles += 14;
                break;

            // LD utility
            case 0x47: I = A; cycles += 9; break;
            case 0x4F: R = A; cycles += 9; break;
            case 0x57:
                A = I;
                FlagS = (A & 0x80) != 0;
                FlagZ = A == 0;
                FlagH = false;
                FlagPV = IFF2;
                FlagN = false;
                cycles += 9;
                break;
            case 0x5F:
                A = R;
                FlagS = (A & 0x80) != 0;
                FlagZ = A == 0;
                FlagH = false;
                FlagPV = IFF2;
                FlagN = false;
                cycles += 9;
                break;

            // SBC HL, ss
            case 0x42: SbcHL(BC); break;
            case 0x52: SbcHL(DE); break;
            case 0x62: SbcHL(HL); break;
            case 0x72: SbcHL(SP); break;

            // ADC HL, ss
            case 0x4A: AdcHL(BC); break;
            case 0x5A: AdcHL(DE); break;
            case 0x6A: AdcHL(HL); break;
            case 0x7A: AdcHL(SP); break;

            // Block instructions
            case 0xA0: // LDI
                {
                    byte val = ReadMemory(HL);
                    WriteMemory(DE, val);
                    HL++;
                    DE++;
                    BC--;
                    FlagH = false;
                    FlagPV = BC != 0;
                    FlagN = false;
                    cycles += 16;
                }
                break;

            case 0xA8: // LDD
                {
                    byte val = ReadMemory(HL);
                    WriteMemory(DE, val);
                    HL--;
                    DE--;
                    BC--;
                    FlagH = false;
                    FlagPV = BC != 0;
                    FlagN = false;
                    cycles += 16;
                }
                break;

            case 0xB8: // LDDR
                {
                    byte val = ReadMemory(HL);
                    WriteMemory(DE, val);
                    HL--;
                    DE--;
                    BC--;
                    FlagH = false;
                    FlagPV = BC != 0;
                    FlagN = false;
                    if (BC != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            case 0xA1: // CPI
                {
                    byte val = ReadMemory(HL);
                    int res = A - val;
                    HL++;
                    BC--;
                    FlagS = (res & 0x80) != 0;
                    FlagZ = res == 0;
                    FlagH = ((A ^ val ^ res) & 0x10) != 0;
                    FlagPV = BC != 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xA9: // CPD
                {
                    byte val = ReadMemory(HL);
                    int res = A - val;
                    HL--;
                    BC--;
                    FlagS = (res & 0x80) != 0;
                    FlagZ = res == 0;
                    FlagH = ((A ^ val ^ res) & 0x10) != 0;
                    FlagPV = BC != 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xB1: // CPIR
                {
                    byte val = ReadMemory(HL);
                    int res = A - val;
                    HL++;
                    BC--;
                    FlagS = (res & 0x80) != 0;
                    FlagZ = res == 0;
                    FlagH = ((A ^ val ^ res) & 0x10) != 0;
                    FlagPV = BC != 0;
                    FlagN = true;
                    if (BC != 0 && res != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            case 0xB9: // CPDR
                {
                    byte val = ReadMemory(HL);
                    int res = A - val;
                    HL--;
                    BC--;
                    FlagS = (res & 0x80) != 0;
                    FlagZ = res == 0;
                    FlagH = ((A ^ val ^ res) & 0x10) != 0;
                    FlagPV = BC != 0;
                    FlagN = true;
                    if (BC != 0 && res != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            // Block I/O instructions
            case 0xA2: // INI
                {
                    byte val = OnInputPort?.Invoke(C, cycles + 9) ?? 0xFF;
                    WriteMemory(HL, val);
                    HL++;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xB2: // INIR
                {
                    byte val = OnInputPort?.Invoke(C, cycles + 9) ?? 0xFF;
                    WriteMemory(HL, val);
                    HL++;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    if (B != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            case 0xAA: // IND
                {
                    byte val = OnInputPort?.Invoke(C, cycles + 9) ?? 0xFF;
                    WriteMemory(HL, val);
                    HL--;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xBA: // INDR
                {
                    byte val = OnInputPort?.Invoke(C, cycles + 9) ?? 0xFF;
                    WriteMemory(HL, val);
                    HL--;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    if (B != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            case 0xA3: // OUTI
                {
                    byte val = ReadMemory(HL);
                    OnOutputPort?.Invoke(C, val, cycles + 11);
                    HL++;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xB3: // OTIR
                {
                    byte val = ReadMemory(HL);
                    OnOutputPort?.Invoke(C, val, cycles + 11);
                    HL++;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    if (B != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            case 0xAB: // OUTD
                {
                    byte val = ReadMemory(HL);
                    OnOutputPort?.Invoke(C, val, cycles + 11);
                    HL--;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    cycles += 16;
                }
                break;

            case 0xBB: // OTDR
                {
                    byte val = ReadMemory(HL);
                    OnOutputPort?.Invoke(C, val, cycles + 11);
                    HL--;
                    B--;
                    FlagZ = B == 0;
                    FlagN = true;
                    if (B != 0)
                    {
                        PC -= 2;
                        cycles += 21;
                    }
                    else
                    {
                        cycles += 16;
                    }
                }
                break;

            // RRD (Rotate Right Digit)
            case 0x67:
                {
                    byte temp = ReadMemory(HL);
                    byte new_val = (byte)(((A & 0x0F) << 4) | (temp >> 4));
                    A = (byte)((A & 0xF0) | (temp & 0x0F));
                    WriteMemory(HL, new_val);
                    FlagS = (A & 0x80) != 0;
                    FlagZ = A == 0;
                    FlagH = false;
                    FlagPV = GetParity(A);
                    FlagN = false;
                    cycles += 18;
                }
                break;

            // RLD (Rotate Left Digit)
            case 0x6F:
                {
                    byte temp = ReadMemory(HL);
                    byte new_val = (byte)(((temp & 0x0F) << 4) | (A & 0x0F));
                    A = (byte)((A & 0xF0) | (temp >> 4));
                    WriteMemory(HL, new_val);
                    FlagS = (A & 0x80) != 0;
                    FlagZ = A == 0;
                    FlagH = false;
                    FlagPV = GetParity(A);
                    FlagN = false;
                    cycles += 18;
                }
                break;
                
            default:
                throw new Exception($"Opcode ED no implementado: ED {op:X2} en PC: {PC-2:X4}");
        }
    }

    private void ExecuteDD()
    {
        byte op = Fetch8();
        switch (op)
        {
            // Opcodes 16-bit para IX
            case 0x21: IX = Fetch16(); cycles += 14; break;
            case 0x22: { ushort addr = Fetch16(); WriteMemory(addr, (byte)(IX & 0xFF)); WriteMemory((ushort)(addr + 1), (byte)(IX >> 8)); cycles += 20; } break;
            case 0x2A: { ushort addr = Fetch16(); byte low = ReadMemory(addr); byte high = ReadMemory((ushort)(addr + 1)); IX = (ushort)((high << 8) | low); cycles += 20; } break;
            case 0x23: IX++; cycles += 10; break;
            case 0x2B: IX--; cycles += 10; break;
            case 0x09: { int res = IX + BC; FlagH = ((IX & 0x0FFF) + (BC & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IX = (ushort)res; cycles += 15; } break;
            case 0x19: { int res = IX + DE; FlagH = ((IX & 0x0FFF) + (DE & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IX = (ushort)res; cycles += 15; } break;
            case 0x29: { int res = IX + IX; FlagH = ((IX & 0x0FFF) + (IX & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IX = (ushort)res; cycles += 15; } break;
            case 0x39: { int res = IX + SP; FlagH = ((IX & 0x0FFF) + (SP & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IX = (ushort)res; cycles += 15; } break;
            case 0xE5: Push16(IX); cycles += 15; break;
            case 0xE1: IX = Pop16(); cycles += 14; break;
            case 0xF9: SP = IX; cycles += 10; break;
            case 0xE9: PC = IX; cycles += 8; break;
            case 0xE3: { byte low = ReadMemory(SP); byte high = ReadMemory((ushort)(SP + 1)); WriteMemory(SP, (byte)(IX & 0xFF)); WriteMemory((ushort)(SP + 1), (byte)(IX >> 8)); IX = (ushort)((high << 8) | low); cycles += 23; } break;
            
            // Opcodes especiales
            case 0x36: { sbyte d = (sbyte)Fetch8(); byte n = Fetch8(); WriteMemory((ushort)(IX + d), n); cycles += 19; } break;
            
            // Opcodes CB-prefixed para IX
            case 0xCB:
                {
                    sbyte d = (sbyte)Fetch8();
                    byte cbOp = Fetch8();
                    int x = (cbOp >> 6) & 0x03;
                    int y = (cbOp >> 3) & 0x07;
                    int z = cbOp & 0x07;
                    ushort addr = (ushort)(IX + d);
                    byte val = ReadMemory(addr);
                    if (x == 0)
                    {
                        byte res = y switch {
                            0 => RLC(val), 1 => RRC(val), 2 => RL(val), 3 => RR(val),
                            4 => SLA(val), 5 => SRA(val), 6 => SLL(val), 7 => SRL(val), _ => 0
                        };
                        WriteMemory(addr, res);
                        if (z != 6) SetRegister8(z, res);
                        cycles += 23;
                    }
                    else if (x == 1) { BitTest(y, val, true); cycles += 20; }
                    else if (x == 2) { byte res = (byte)(val & ~(1 << y)); WriteMemory(addr, res); if (z != 6) SetRegister8(z, res); cycles += 23; }
                    else if (x == 3) { byte res = (byte)(val | (1 << y)); WriteMemory(addr, res); if (z != 6) SetRegister8(z, res); cycles += 23; }
                }
                break;

            default:
                indexMode = 1;
                isDisplacementFetched = false;
                opcodes[op]();
                indexMode = 0;
                break;
        }
    }

    private void ExecuteFD()
    {
        byte op = Fetch8();
        switch (op)
        {
            // Opcodes 16-bit para IY
            case 0x21: IY = Fetch16(); cycles += 14; break;
            case 0x22: { ushort addr = Fetch16(); WriteMemory(addr, (byte)(IY & 0xFF)); WriteMemory((ushort)(addr + 1), (byte)(IY >> 8)); cycles += 20; } break;
            case 0x2A: { ushort addr = Fetch16(); byte low = ReadMemory(addr); byte high = ReadMemory((ushort)(addr + 1)); IY = (ushort)((high << 8) | low); cycles += 20; } break;
            case 0x23: IY++; cycles += 10; break;
            case 0x2B: IY--; cycles += 10; break;
            case 0x09: { int res = IY + BC; FlagH = ((IY & 0x0FFF) + (BC & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IY = (ushort)res; cycles += 15; } break;
            case 0x19: { int res = IY + DE; FlagH = ((IY & 0x0FFF) + (DE & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IY = (ushort)res; cycles += 15; } break;
            case 0x29: { int res = IY + IY; FlagH = ((IY & 0x0FFF) + (IY & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IY = (ushort)res; cycles += 15; } break;
            case 0x39: { int res = IY + SP; FlagH = ((IY & 0x0FFF) + (SP & 0x0FFF)) > 0x0FFF; FlagN = false; FlagC = res > 0xFFFF; IY = (ushort)res; cycles += 15; } break;
            case 0xE5: Push16(IY); cycles += 15; break;
            case 0xE1: IY = Pop16(); cycles += 14; break;
            case 0xF9: SP = IY; cycles += 10; break;
            case 0xE9: PC = IY; cycles += 8; break;
            case 0xE3: { byte low = ReadMemory(SP); byte high = ReadMemory((ushort)(SP + 1)); WriteMemory(SP, (byte)(IY & 0xFF)); WriteMemory((ushort)(SP + 1), (byte)(IY >> 8)); IY = (ushort)((high << 8) | low); cycles += 23; } break;
            
            // Opcodes especiales
            case 0x36: { sbyte d = (sbyte)Fetch8(); byte n = Fetch8(); WriteMemory((ushort)(IY + d), n); cycles += 19; } break;
            
            // Opcodes CB-prefixed para IY
            case 0xCB:
                {
                    sbyte d = (sbyte)Fetch8();
                    byte cbOp = Fetch8();
                    int x = (cbOp >> 6) & 0x03;
                    int y = (cbOp >> 3) & 0x07;
                    int z = cbOp & 0x07;
                    ushort addr = (ushort)(IY + d);
                    byte val = ReadMemory(addr);
                    if (x == 0)
                    {
                        byte res = y switch {
                            0 => RLC(val), 1 => RRC(val), 2 => RL(val), 3 => RR(val),
                            4 => SLA(val), 5 => SRA(val), 6 => SLL(val), 7 => SRL(val), _ => 0
                        };
                        WriteMemory(addr, res);
                        if (z != 6) SetRegister8(z, res);
                        cycles += 23;
                    }
                    else if (x == 1) { BitTest(y, val, true); cycles += 20; }
                    else if (x == 2) { byte res = (byte)(val & ~(1 << y)); WriteMemory(addr, res); if (z != 6) SetRegister8(z, res); cycles += 23; }
                    else if (x == 3) { byte res = (byte)(val | (1 << y)); WriteMemory(addr, res); if (z != 6) SetRegister8(z, res); cycles += 23; }
                }
                break;

            default:
                indexMode = 2;
                isDisplacementFetched = false;
                opcodes[op]();
                indexMode = 0;
                break;
        }
    }
}
