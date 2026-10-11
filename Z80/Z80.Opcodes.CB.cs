using System;

namespace Z80;

public partial class Z80
{
    private void ExecuteCB()
    {
        byte op = Fetch8();
        int x = (op >> 6) & 0x03;
        int y = (op >> 3) & 0x07;
        int z = op & 0x07;

        switch (x)
        {
            case 0: // Desplazamiento / Rotación
                byte val = GetRegister8(z);
                byte res = y switch
                {
                    0 => RLC(val),
                    1 => RRC(val),
                    2 => RL(val),
                    3 => RR(val),
                    4 => SLA(val),
                    5 => SRA(val),
                    6 => SLL(val),
                    7 => SRL(val),
                    _ => 0
                };
                SetRegister8(z, res);
                cycles += (z == 6) ? 15 : 8; // (HL) toma 15 ciclos, otros 8
                break;
                
            case 1: // BIT (Testeo de bit)
                BitTest(y, GetRegister8(z), z == 6);
                cycles += (z == 6) ? 12 : 8;
                break;
                
            case 2: // RES (Resetear bit)
                SetRegister8(z, (byte)(GetRegister8(z) & ~(1 << y)));
                cycles += (z == 6) ? 15 : 8;
                break;
                
            case 3: // SET (Establecer bit)
                SetRegister8(z, (byte)(GetRegister8(z) | (1 << y)));
                cycles += (z == 6) ? 15 : 8;
                break;
        }
    }

    // --- Implementaciones Auxiliares para Operaciones del Prefijo CB ---

    private bool GetParity(byte val)
    {
        int bits = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((val & (1 << i)) != 0) bits++;
        }
        return bits % 2 == 0;
    }

    private byte RLC(byte val)
    {
        byte carry = (byte)((val & 0x80) >> 7);
        byte res = (byte)((val << 1) | carry);
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private byte RRC(byte val)
    {
        byte carry = (byte)(val & 0x01);
        byte res = (byte)((val >> 1) | (carry << 7));
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private byte RL(byte val)
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        byte newCarry = (byte)((val & 0x80) >> 7);
        byte res = (byte)((val << 1) | oldCarry);
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = newCarry != 0;
        return res;
    }

    private byte RR(byte val)
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        byte newCarry = (byte)(val & 0x01);
        byte res = (byte)((val >> 1) | (oldCarry << 7));
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = newCarry != 0;
        return res;
    }

    private byte SLA(byte val)
    {
        byte carry = (byte)((val & 0x80) >> 7);
        byte res = (byte)(val << 1);
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private byte SRA(byte val)
    {
        byte carry = (byte)(val & 0x01);
        byte res = (byte)((val >> 1) | (val & 0x80));
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private byte SLL(byte val)
    {
        // Instrucción indocumentada pero ampliamente emulada (hace shift e inserta un 1)
        byte carry = (byte)((val & 0x80) >> 7);
        byte res = (byte)((val << 1) | 1);
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private byte SRL(byte val)
    {
        byte carry = (byte)(val & 0x01);
        byte res = (byte)(val >> 1);
        FlagS = (res & 0x80) != 0;
        FlagZ = res == 0;
        FlagH = false;
        FlagPV = GetParity(res);
        FlagN = false;
        FlagC = carry != 0;
        return res;
    }

    private void BitTest(int bit, byte val, bool isHL)
    {
        bool bitActive = (val & (1 << bit)) != 0;
        FlagS = (bit == 7) && bitActive;
        FlagZ = !bitActive;
        FlagH = true; // BIT siempre setea H en Z80
        FlagPV = !bitActive; // PV copia Z en esta instrucción
        FlagN = false;
    }
}
