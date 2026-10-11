namespace retro.spectrum;

public static class SpectrumKeyMap
{
    public const int Rows = 8;
    public const int Cols = 5;

    public const int CapsShift = 0;
    public const int Z = 1;
    public const int X = 2;
    public const int C = 3;
    public const int V = 4;
    public const int A = 5;
    public const int S = 6;
    public const int D = 7;
    public const int F = 8;
    public const int G = 9;
    public const int Q = 10;
    public const int W = 11;
    public const int E = 12;
    public const int R = 13;
    public const int T = 14;
    public const int Key1 = 15;
    public const int Key2 = 16;
    public const int Key3 = 17;
    public const int Key4 = 18;
    public const int Key5 = 19;
    public const int Key0 = 20;
    public const int Key9 = 21;
    public const int Key8 = 22;
    public const int Key7 = 23;
    public const int Key6 = 24;
    public const int P = 25;
    public const int O = 26;
    public const int I = 27;
    public const int U = 28;
    public const int Y = 29;
    public const int Enter = 30;
    public const int L = 31;
    public const int K = 32;
    public const int J = 33;
    public const int H = 34;
    public const int Space = 35;
    public const int SymbolShift = 36;
    public const int M = 37;
    public const int N = 38;
    public const int B = 39;

    public static (int row, int bit) Get(int keyId) => keyId switch
    {
        CapsShift => (0, 0), Z => (0, 1), X => (0, 2), C => (0, 3), V => (0, 4),
        A => (1, 0), S => (1, 1), D => (1, 2), F => (1, 3), G => (1, 4),
        Q => (2, 0), W => (2, 1), E => (2, 2), R => (2, 3), T => (2, 4),
        Key1 => (3, 0), Key2 => (3, 1), Key3 => (3, 2), Key4 => (3, 3), Key5 => (3, 4),
        Key0 => (4, 0), Key9 => (4, 1), Key8 => (4, 2), Key7 => (4, 3), Key6 => (4, 4),
        P => (5, 0), O => (5, 1), I => (5, 2), U => (5, 3), Y => (5, 4),
        Enter => (6, 0), L => (6, 1), K => (6, 2), J => (6, 3), H => (6, 4),
        Space => (7, 0), SymbolShift => (7, 1), M => (7, 2), N => (7, 3), B => (7, 4),
        _ => (-1, -1)
    };
}
