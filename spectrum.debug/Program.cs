using System;
using System.IO;
using System.Linq;
using System.Threading;
using retro;
using retro.spectrum;

namespace retro.spectrum.debug;

internal static class Program
{
    private static SpectrumGame? _game;
    private static string? _romPath;
    private static string? _tapePath;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("===========================================");
        Console.WriteLine(" ZX Spectrum 48K - Debug Console");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        string? romPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-rom" && i + 1 < args.Length) romPath = args[++i];
            else if (args[i] == "-tape" && i + 1 < args.Length) _tapePath = args[++i];
        }

        if (romPath == null) romPath = FindRom();

        if (romPath == null || !File.Exists(romPath))
        {
            Console.WriteLine("[ERROR] No se encontró 48.rom. Colócalo junto al .exe o pasa -rom <ruta>");
            return 1;
        }

        _romPath = romPath;
        Console.WriteLine($"[OK] ROM: {Path.GetFileName(romPath)}");

        _game = new SpectrumGame(
            tzxPath: _tapePath,
            fastLoad: false,
            romName: Path.GetFileName(romPath),
            logger: new ConsoleLogger(),
            assets: new FileSystemLoggerAssets(Directory.GetCurrentDirectory()));

        _game.InitializeEmulation();

        Console.WriteLine("[OK] Spectrum inicializado");
        Console.WriteLine($"[OK] Cinta: {(_tapePath ?? "(ninguna)")}");
        Console.WriteLine();

        if (_tapePath != null && File.Exists(_tapePath))
        {
            bool ok = _game.LoadTapeFromPath(_tapePath);
            Console.WriteLine(ok
                ? "[OK] Cinta cargada correctamente"
                : "[ERROR] No se pudo cargar la cinta");
        }

        ShowMainMenu();
        return 0;
    }

    private static string? FindRom()
    {
        string[] candidates = {
            "48.rom", "spectrum/48.rom",
            @"C:\Dev\retrosystem\spectrum\48.rom",
            @"C:\Dev\retrosystem\spectrum.android\Assets\spectrum\48.rom",
            @"C:\Dev\z80emu\spectrum\48.rom"
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }
        return null;
    }

    private static void ShowMainMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== MENÚ DEBUG ===");
            Console.WriteLine("  1) Ver estado del Spectrum (PC, A, pantalla)");
            Console.WriteLine("  2) Ver buffer de entrada 0x5C00-0x5C04");
            Console.WriteLine("  3) Inyectar LOAD \"\" + Enter en buffer (directo)");
            Console.WriteLine("  4) Llamar TypeLoadCommand() (auto-typing con teclado virtual)");
            Console.WriteLine("  5) Llamar LoadTapeAndAutoType() con cinta");
            Console.WriteLine("  6) Cargar cinta manualmente");
            Console.WriteLine("  7) Reset CPU");
            Console.WriteLine("  8) Avanzar 100 frames (2 segundos)");
            Console.WriteLine("  9) Avanzar 500 frames (10 segundos)");
            Console.WriteLine("  r) Dump pantalla 32x24 a consola");
            Console.WriteLine("  m) Dump memoria de pantalla 0x4000-0x5AFF");
            Console.WriteLine("  x) Salir");
            Console.WriteLine();
            Console.Write("Opción: ");

            var key = Console.ReadLine()?.Trim().ToLowerInvariant();
            Console.WriteLine();

            switch (key)
            {
                case "1": ShowState(); break;
                case "2": ShowInputBuffer(); break;
                case "3": InjectLoadCommand(); break;
                case "4": TestTypeLoadCommand(); break;
                case "5": TestLoadTapeAndAutoType(); break;
                case "6": LoadTapeManual(); break;
                case "7": ResetCpu(); break;
                case "8": AdvanceFrames(100); break;
                case "9": AdvanceFrames(500); break;
                case "r": DumpScreen(); break;
                case "m": DumpMemory(); break;
                case "x": return;
                default: Console.WriteLine("[ERROR] Opción inválida"); break;
            }
        }
    }

    private static void ShowState()
    {
        if (_game == null) return;
        Console.WriteLine($"PC:  0x{_game.GetPC():X4}");
        Console.WriteLine($"SP:  0x{_game.GetSP():X4}");
        Console.WriteLine($"A:   0x{_game.GetA():X2}");
        Console.WriteLine($"BC:  0x{_game.GetBC():X4}");
        Console.WriteLine($"DE:  0x{_game.GetDE():X4}");
        Console.WriteLine($"HL:  0x{_game.GetHL():X4}");
        Console.WriteLine($"IX:  0x{_game.GetIX():X4}");
        Console.WriteLine($"IY:  0x{_game.GetIY():X4}");
        Console.WriteLine($"Cycles: {_game.GetCycles()}");
    }

    private static void ShowInputBuffer()
    {
        if (_game == null) return;
        Console.WriteLine("[Buffer 0x5C00-0x5C04]:");
        Console.WriteLine("  " + _game.DumpInputBuffer());
    }

    private static void InjectLoadCommand()
    {
        if (_game == null) return;
        Console.WriteLine("[*] Inyectando LOAD \"\" + Enter en buffer 0x5C00...");
        _game.InjectLoadToBuffer();
        Thread.Sleep(100);
        Console.WriteLine("[OK] Inyectado. Estado del buffer:");
        ShowInputBuffer();
    }

    private static void TestTypeLoadCommand()
    {
        if (_game == null) return;
        Console.WriteLine("[*] Llamando a TypeLoadCommand() (auto-typing con teclado virtual)...");
        _game.TypeLoadCommand();
        Console.WriteLine("[OK] Comando enviado");
        ShowState();
    }

    private static void TestLoadTapeAndAutoType()
    {
        if (_game == null) return;
        if (_tapePath == null || !File.Exists(_tapePath))
        {
            Console.WriteLine("[ERROR] No hay cinta cargada. Usa opción 6 primero.");
            return;
        }
        Console.WriteLine($"[*] Llamando a LoadTapeAndAutoType({Path.GetFileName(_tapePath)})...");
        bool ok = _game.LoadTapeAndAutoType(_tapePath);
        Console.WriteLine(ok
            ? "[OK] Cinta cargada y auto-typing iniciado"
            : "[ERROR] No se pudo cargar la cinta");
    }

    private static void LoadTapeManual()
    {
        if (_game == null) return;
        Console.Write("Ruta del .tap/.tzx: ");
        var path = Console.ReadLine()?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.WriteLine("[ERROR] Archivo no encontrado");
            return;
        }
        _tapePath = path;
        bool ok = _game.LoadTapeFromPath(path);
        Console.WriteLine(ok
            ? $"[OK] Cinta cargada: {Path.GetFileName(path)}"
            : "[ERROR] No se pudo cargar");
    }

    private static void ResetCpu()
    {
        if (_game == null) return;
        _game.ResetCpu();
        Console.WriteLine("[OK] CPU reseteada");
    }

    private static void AdvanceFrames(int count)
    {
        if (_game == null) return;
        Console.WriteLine($"[*] Avanzando {count} frames...");
        _game.AdvanceFrames(count);
        Console.WriteLine("[OK] Listo");
    }

    private static void DumpScreen()
    {
        if (_game == null) return;
        var screen = _game.DumpScreenText();
        Console.WriteLine("[Pantalla 32x24]");
        Console.WriteLine("+--------------------------------+");
        for (int i = 0; i < 24; i++)
        {
            string row = screen.Substring(i * 32, 32);
            Console.WriteLine("|" + row + "|");
        }
        Console.WriteLine("+--------------------------------+");
    }

    private static void DumpMemory()
    {
        if (_game == null) return;
        Console.WriteLine(_game.DumpScreenMemory());
    }
}

internal sealed class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine($"[INFO ] {message}");
    public void Warning(string message) => Console.WriteLine($"[WARN ] {message}");
    public void Error(string message) => Console.Error.WriteLine($"[ERROR] {message}");
}

internal sealed class FileSystemLoggerAssets : IAssetProvider
{
    private readonly string _basePath;
    public FileSystemLoggerAssets(string basePath) { _basePath = basePath; }
    public bool Exists(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath)) return true;
        return File.Exists(Path.Combine(_basePath, relativePath));
    }
    public byte[]? ReadAllBytes(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath)) return File.ReadAllBytes(relativePath);
        var full = Path.Combine(_basePath, relativePath);
        return File.Exists(full) ? File.ReadAllBytes(full) : null;
    }
    public IEnumerable<string> ListFiles(string relativeSubdir) => Enumerable.Empty<string>();
}
