using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Z80;
using Tape;
using Tape.Blocks;
using retro;

namespace retro.spectrum;

public class SpectrumGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch = null!;
    private Texture2D displayTexture = null!;
    private uint[] textureBuffer = null!;

    private Z80.Z80 cpu = null!;
    private SpectrumMemoryBus memoryBus = null!;
    private ULA ula = null!;
    private bool isRomLoaded = false;

    private readonly byte[] keyboardMatrix = new byte[8];
    private readonly byte[] virtualKeyState = new byte[8]
    {
        0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F
    };
    private int currentFrameCycles = 0;
    private byte lastPortFeValue = 0;

    private TapePlayer? tapePlayer;
    private TapeFile? tapeFile;
    private string? loadedTzxPath;
    private byte[]? loadedTapeBytes;
    private int fastLoadBlockIndex;
    private int lastKeyboardPollMs = 0;

    private readonly ILogger logger;
    private readonly IAssetProvider assets;

    private readonly string? initialTzxPath;
    private bool fastLoad;
    private readonly string? romName;

    public string? LoadedTapeName => loadedTzxPath != null ? System.IO.Path.GetFileNameWithoutExtension(loadedTzxPath) : null;
    public bool IsTapePlaying => tapePlayer != null && tapePlayer.IsPlaying;
    public bool HasTape => tapePlayer != null;
    public string Title => $"ZX Spectrum 48K — {(IsTapePlaying ? "▶" : (HasTape ? "⏸" : "⏹"))} {LoadedTapeName ?? "(sin cinta)"}";

    public bool FastLoadEnabled => fastLoad;
    public bool IsRomLoaded => isRomLoaded;
    public string? LastTapeError { get; private set; }

    private DynamicSoundEffectInstance? soundInstance;
    private bool audioEnabled = false;
    private readonly List<short> audioBuffer = new List<short>();
    private double sampleAccumulator = 0;
    private bool beeperState = false;
    private const double CyclesPerSample = 3500000.0 / 44100.0;

    private const double BeeperVoltageMax = 3.753;
    private const double BeeperDiodeThreshold = 1.4;
    private double beeperFilterState = 0.0;
    private const double BeeperCutoffHz = 3000.0;
    private static readonly double BeeperRcAlpha =
        1.0 - Math.Exp(-BeeperCutoffHz * (CyclesPerSample / 3500000.0));

    public SpectrumGame(
        string? tzxPath = null,
        bool fastLoad = false,
        string? romName = null,
        ILogger? logger = null,
        IAssetProvider? assets = null)
    {
        initialTzxPath = tzxPath;
        this.fastLoad = fastLoad;
        this.romName = romName;
        this.logger = logger ?? new NullLogger();
        this.assets = assets ?? new NullAssetProvider();
        fastLoadBlockIndex = 0;

        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 320 * 3;
        graphics.PreferredBackBufferHeight = 256 * 3;
        graphics.SynchronizeWithVerticalRetrace = false;
        IsMouseVisible = true;
        Window.Title = "Sinclair ZX Spectrum 48K";

        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 50.0);
        IsFixedTimeStep = true;
    }

    protected override void Initialize()
    {
        ClearKeyboardMatrix();
        ClearVirtualKeys();

        byte[]? romData = LoadRom();
        if (romData != null && romData.Length >= 16384)
        {
            memoryBus = new SpectrumMemoryBus(romData);
            cpu = new Z80.Z80();
            cpu.MemoryBus = memoryBus;
            ula = new ULA(memoryBus);

            cpu.OnOutputPort = (port, val, cycle) =>
            {
                ushort port16 = Get16BitPort(port);
                if ((port16 & 0x01) == 0)
                {
                    byte newColor = (byte)(val & 0x07);
                    ula.UpdateBorderColor(newColor, cycle);
                    beeperState = (val & 0x18) != 0;
                    lastPortFeValue = val;
                }
            };

            cpu.OnInputPort = (port, cycle) =>
            {
                ushort port16 = Get16BitPort(port);
                if ((port16 & 0x01) == 0)
                {
                    int nowMs = Environment.TickCount;
                    if (nowMs - lastKeyboardPollMs >= 5)
                    {
                        UpdateKeyboardState();
                        lastKeyboardPollMs = nowMs;
                    }

                    byte result = 0x1F;
                    byte rowSelect = (byte)(port16 >> 8);

                    for (int i = 0; i < 8; i++)
                    {
                        if ((rowSelect & (1 << i)) == 0)
                            result &= keyboardMatrix[i];
                    }

                    if (tapePlayer != null && tapePlayer.CurrentSignalLevel)
                        result |= 0x40;

                    return (byte)(result | 0xA0);
                }
                return 0xFF;
            };

            cpu.Reset();
            isRomLoaded = true;
            logger.Info($"[INFO] ROM cargada: {romName}");

            LoadInitialTape();
        }
        else
        {
            logger.Info("======================================================================");
            logger.Info("[ERROR] ROM del ZX Spectrum (48.rom) no encontrada.");
            logger.Info("Coloque el archivo '48.rom' (16 KB) en la carpeta spectrum/ junto al ejecutable.");
            logger.Info("======================================================================");
            Exit();
        }

        displayTexture = new Texture2D(GraphicsDevice, 320, 256);
        textureBuffer = new uint[320 * 256];

        TryInitAudio();

        base.Initialize();
    }

    private byte[]? LoadRom()
    {
        string[] candidates = new[] { romName ?? "48.rom" };
        if (string.IsNullOrEmpty(romName)) candidates = new[] { "48.rom", "spectrum/48.rom" };
        else candidates = new[] { romName, $"spectrum/{romName}" };

        foreach (var name in candidates)
        {
            if (assets.Exists(name))
            {
                var data = assets.ReadAllBytes(name);
                if (data != null && data.Length >= 16384) return data;
            }
        }
        return null;
    }

    private void LoadInitialTape()
    {
        string? tzxPath = null;

        if (initialTzxPath != null && assets.Exists(initialTzxPath))
        {
            tzxPath = initialTzxPath;
        }
        else
        {
            var tap = assets.ListFiles(".")
                .FirstOrDefault(f => f.EndsWith(".tap", StringComparison.OrdinalIgnoreCase));
            if (tap != null) tzxPath = tap;
            else
            {
                var tzx = assets.ListFiles(".")
                    .FirstOrDefault(f => f.EndsWith(".tzx", StringComparison.OrdinalIgnoreCase));
                if (tzx != null) tzxPath = tzx;
            }
        }

        if (tzxPath == null) return;

        try
        {
            var tapeBytes = assets.ReadAllBytes(tzxPath);
            if (tapeBytes == null) return;

            tapeFile = TapeFile.Load(tapeBytes, tzxPath);
            tapePlayer = new TapePlayer(3_500_000);
            tapePlayer.Insert(tapeFile);
            loadedTzxPath = tzxPath;
            loadedTapeBytes = tapeBytes;
            string tapeName = System.IO.Path.GetFileNameWithoutExtension(tzxPath);
            logger.Info($"[TAPE] Sistema: {tapeFile.System}, Versión: {tapeFile.VersionMajor}.{tapeFile.VersionMinor}");
            logger.Info($"[TAPE] Bloques en la cinta: {tapeFile.Blocks.Count}");
            foreach (var blk in tapeFile.Blocks)
                logger.Info($"  [0x{blk.BlockId:X2}] {blk.Description}");
            logger.Info($"[TAPE] HasData={tapePlayer.HasData}");
            if (fastLoad) logger.Info("[FASTLOAD] Activado");
            logger.Info($"[TAPE] Cargado: {tapeName}  |  F1 = Play/Pause  |  F2 = Rebobinar");
            Window.Title = $"ZX Spectrum 48K — ⏹ {tapeName}";
        }
        catch (Exception ex)
        {
            logger.Error($"[TAPE] Error cargando cinta: {ex.Message}");
        }
    }

    private void TryInitAudio()
    {
        try
        {
            soundInstance = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
            soundInstance.Play();
            audioEnabled = true;
            logger.Info("[INFO] Spectrum Beeper Audio inicializado con éxito.");
        }
        catch (Exception ex)
        {
            logger.Info($"[WARNING] No se pudo inicializar el audio del beeper: {ex.Message}");
            soundInstance = null;
            audioEnabled = false;
        }
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        UpdateKeyboardState();

        var prev = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
        }

        RunTapeControlHotkeys();

        if (isRomLoaded)
        {
            ula.OnFrameStart(cpu.Cycles);
            currentFrameCycles = 0;
            bool interruptFired = false;

            const int targetCycles = 69888;
            const int intCycle = 1360;

            while (currentFrameCycles < targetCycles)
            {
                if (!interruptFired && currentFrameCycles >= intCycle)
                {
                    cpu.InterruptPending = true;
                    interruptFired = true;
                }

                if (fastLoad && cpu.PC == 0x0556 && tapeFile != null)
                {
                    TryFastLoadBlock();
                    continue;
                }

                int c = cpu.Step();
                int elapsed = c <= 0 ? 4 : c;
                currentFrameCycles += elapsed;

                tapePlayer?.AdvanceCycles(elapsed);

                if (audioEnabled)
                {
                    sampleAccumulator += elapsed;
                    while (sampleAccumulator >= CyclesPerSample)
                    {
                        bool earIn = tapePlayer != null && tapePlayer.IsPlaying && tapePlayer.CurrentSignalLevel;
                        bool bit4 = (lastPortFeValue & 0x10) != 0;
                        bool bit3 = (lastPortFeValue & 0x08) != 0;

                        double voltage;
                        if (bit4 && bit3)       voltage = 3.753;
                        else if (bit4 && !bit3) voltage = 3.591;
                        else if (!bit4 && bit3) voltage = 0.652;
                        else                    voltage = 0.342;

                        double speakerV = voltage > BeeperDiodeThreshold ? voltage : 0.0;
                        double micV = voltage;
                        double earV = earIn ? 0.7 : 0.0;

                        double targetVol =
                            (speakerV / BeeperVoltageMax) * 1.6 +
                            (micV / BeeperVoltageMax) * 0.2 +
                            (earV / BeeperVoltageMax) * 0.2
                            - 1.0;

                        beeperFilterState += BeeperRcAlpha * (targetVol - beeperFilterState);
                        short sample = (short)(beeperFilterState * 12000);
                        audioBuffer.Add(sample);
                        sampleAccumulator -= CyclesPerSample;
                    }
                }
            }

            if (audioEnabled && audioBuffer.Count > 0)
            {
                try
                {
                    byte[] byteBuffer = new byte[audioBuffer.Count * 2];
                    for (int i = 0; i < audioBuffer.Count; i++)
                    {
                        short val = audioBuffer[i];
                        byteBuffer[i * 2] = (byte)(val & 0xFF);
                        byteBuffer[i * 2 + 1] = (byte)(val >> 8);
                    }

                    if (soundInstance != null)
                    {
                        if (soundInstance.State != SoundState.Playing)
                        {
                            soundInstance.Play();
                        }
                        if (soundInstance.PendingBufferCount < 5)
                        {
                            soundInstance.SubmitBuffer(byteBuffer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"[WARNING] Error al reproducir audio beeper: {ex.Message}");
                    audioEnabled = false;
                }
                audioBuffer.Clear();
            }
        }

        base.Update(gameTime);
    }

    private bool WasKeyDown(Microsoft.Xna.Framework.Input.KeyboardState s, Microsoft.Xna.Framework.Input.Keys k) => s.IsKeyDown(k);

    private void RunTapeControlHotkeys()
    {
        var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1))
        {
            if (tapePlayer != null)
            {
                if (tapePlayer.IsPlaying) tapePlayer.Pause();
                else tapePlayer.Play();
                Window.Title = Title;
            }
        }
        if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F2))
        {
            if (tapePlayer != null && loadedTapeBytes != null && loadedTzxPath != null)
            {
                var tape = TapeFile.Load(loadedTapeBytes, loadedTzxPath);
                tapePlayer.Insert(tape);
                fastLoadBlockIndex = 0;
                Window.Title = Title;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (isRomLoaded)
        {
            ula.RenderScreen(textureBuffer);
        }

        displayTexture.SetData(textureBuffer);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        spriteBatch.Draw(displayTexture, new Rectangle(0, 0, 320 * 3, 256 * 3), Color.White);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private ushort Get16BitPort(byte port)
    {
        ushort pc = cpu.PC;
        int dummyCycles = 0;
        byte op = memoryBus.Read((ushort)(pc - 2), ref dummyCycles);

        if (op == 0xDB || op == 0xD3)
        {
            return (ushort)((cpu.A << 8) | port);
        }
        else if (op == 0xED)
        {
            return (ushort)((cpu.B << 8) | port);
        }
        return (ushort)((cpu.B << 8) | port);
    }

    private void ClearKeyboardMatrix()
    {
        for (int i = 0; i < 8; i++) keyboardMatrix[i] = 0x1F;
    }

    private void ClearVirtualKeys()
    {
        for (int i = 0; i < 8; i++) virtualKeyState[i] = 0x1F;
    }

    public void PressKey(int row, int bit)
    {
        if (row < 0 || row >= 8 || bit < 0 || bit >= 5) return;
        keyboardMatrix[row] &= (byte)~(1 << bit);
    }

    public void PressVirtualKey(int row, int bit)
    {
        if (row < 0 || row >= 8 || bit < 0 || bit >= 5) return;
        virtualKeyState[row] &= (byte)~(1 << bit);
    }

    public void PressVirtualKeyById(int keyId)
    {
        var (r, b) = SpectrumKeyMap.Get(keyId);
        PressVirtualKey(r, b);
    }

    public void ReleaseVirtualKey(int row, int bit)
    {
        if (row < 0 || row >= 8 || bit < 0 || bit >= 5) return;
        virtualKeyState[row] |= (byte)(1 << bit);
    }

    public void ReleaseVirtualKeyById(int keyId)
    {
        var (r, b) = SpectrumKeyMap.Get(keyId);
        ReleaseVirtualKey(r, b);
    }

    public void ReleaseAllVirtualKeys()
    {
        ClearVirtualKeys();
    }

    public void ToggleTape()
    {
        if (tapePlayer == null) return;
        if (tapePlayer.IsPlaying) tapePlayer.Pause();
        else tapePlayer.Play();
        Window.Title = Title;
    }

    public void RewindTape()
    {
        if (tapePlayer == null || loadedTapeBytes == null || loadedTzxPath == null) return;
        var tape = TapeFile.Load(loadedTapeBytes, loadedTzxPath);
        tapePlayer.Insert(tape);
        fastLoadBlockIndex = 0;
        Window.Title = Title;
    }

    public bool LoadTapeFromPath(string path)
    {
        LastTapeError = null;
        try
        {
            logger.Info($"[TAPE] LoadTapeFromPath: {path}");
            bool directExists = System.IO.File.Exists(path);
            logger.Info($"[TAPE] File.Exists directo: {directExists}");

            byte[]? bytes = null;
            if (directExists)
            {
                try
                {
                    bytes = System.IO.File.ReadAllBytes(path);
                    logger.Info($"[TAPE] Leídos {bytes.Length} bytes directamente");
                }
                catch (Exception exRead)
                {
                    LastTapeError = $"No se pudo leer el archivo: {exRead.Message}";
                    logger.Error($"[TAPE] Lectura directa falló: {exRead.Message}");
                }
            }
            else if (assets.Exists(path))
            {
                bytes = assets.ReadAllBytes(path);
            }
            else
            {
                LastTapeError = $"Archivo no encontrado: {path}";
                logger.Error($"[TAPE] Archivo no existe: {path}");
                return false;
            }

            if (bytes == null || bytes.Length == 0)
            {
                LastTapeError = "Archivo vacío o ilegible";
                logger.Error($"[TAPE] bytes es null o vacío");
                return false;
            }

            return LoadTapeFromBytes(bytes, path);
        }
        catch (Exception ex)
        {
            LastTapeError = ex.Message;
            logger.Error($"[TAPE] Error cargando {path}: {ex.Message}");
            return false;
        }
    }

    public bool LoadTapeFromBytes(byte[] tapeBytes, string name)
    {
        try
        {
            logger.Info($"[TAPE] LoadTapeFromBytes: {name} ({tapeBytes.Length} bytes)");
            tapeFile = TapeFile.Load(tapeBytes, name);
            if (tapeFile == null)
            {
                LastTapeError = "TapeFile.Load devolvió null (¿formato no soportado?)";
                logger.Error("[TAPE] TapeFile.Load devolvió null");
                return false;
            }
            logger.Info($"[TAPE] TapeFile parseado: {tapeFile.Blocks.Count} bloques");
            tapePlayer = new TapePlayer(3_500_000);
            tapePlayer.Insert(tapeFile);
            loadedTzxPath = name;
            loadedTapeBytes = tapeBytes;
            fastLoadBlockIndex = 0;

            string tapeName = System.IO.Path.GetFileNameWithoutExtension(name);
            logger.Info($"[TAPE] Cargado: {tapeName} — {tapeFile.Blocks.Count} bloques (Fast Load: {(fastLoad ? "ON" : "OFF")})");
            Window.Title = Title;
            return true;
        }
        catch (Exception ex)
        {
            LastTapeError = ex.Message;
            logger.Error($"[TAPE] Error cargando {name}: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                LastTapeError += $" → {ex.InnerException.Message}";
                logger.Error($"[TAPE] Inner: {ex.InnerException.Message}");
            }
            return false;
        }
    }

    public void ResetCpu()
    {
        if (!isRomLoaded) return;
        cpu.Reset();
        currentFrameCycles = 0;
        ula?.OnFrameStart(cpu.Cycles);
        if (tapePlayer != null && loadedTapeBytes != null && loadedTzxPath != null)
        {
            tapeFile = TapeFile.Load(loadedTapeBytes, loadedTzxPath);
            tapePlayer.Insert(tapeFile);
            fastLoadBlockIndex = 0;
        }
        ReleaseAllVirtualKeys();
        typeStringBuffer = null;
        Window.Title = Title;
        logger.Info("[RESET] CPU y ULA reiniciados");
    }

    public void SetFastLoad(bool enabled)
    {
        fastLoad = enabled;
        fastLoadBlockIndex = 0;
        logger.Info($"[FASTLOAD] {(enabled ? "Activado" : "Desactivado")}");
    }

    public void TypeLoadCommand()
    {
        ReleaseAllVirtualKeys();
        typeStringBuffer = null;
        typeStringCharIndex = 0;
        typeStringFrameDelay = 10;
        typeActionBuffer = new[] { 1, 2, 2, 3 };
        typeActionIndex = 0;
        typeStringFrameCounter = -100;
        logger.Info("[TYPE] TypeLoadCommand iniciado: 4 acciones (LOAD, \", \", Enter)");
    }

    private const string LOAD_COMMAND_SEQUENCE = "";

    public bool LoadTapeAndAutoType(string path)
    {
        var ok = LoadTapeFromPath(path);
        if (!ok) return false;
        ResetCpu();
        TypeLoadCommand();
        return true;
    }

    public void TypeString(string s, int frameDelayBetweenChars = 8)
    {
        typeStringBuffer = s;
        typeStringFrameCounter = 0;
        typeStringCharIndex = 0;
        typeStringFrameDelay = frameDelayBetweenChars;
    }

    private string? typeStringBuffer = null;
    private int typeStringFrameCounter = 0;
    private int typeStringCharIndex = 0;
    private int typeStringFrameDelay = 8;
    private int[]? typeActionBuffer = null;
    private int typeActionIndex = 0;

    private void ApplyVirtualKeys()
    {
        for (int i = 0; i < 8; i++)
            keyboardMatrix[i] &= virtualKeyState[i];
    }

    private void ProcessTypeBuffer()
    {
        if (typeActionBuffer != null)
        {
            ProcessActionBuffer();
            return;
        }
        if (typeStringBuffer == null) return;
        typeStringFrameCounter++;
        if (typeStringFrameCounter < typeStringFrameDelay) return;
        typeStringFrameCounter = 0;
        if (typeStringCharIndex >= typeStringBuffer.Length)
        {
            logger.Info($"[TYPE] Buffer terminado, total {typeStringBuffer.Length} chars");
            typeStringBuffer = null;
            return;
        }

        char c = typeStringBuffer[typeStringCharIndex++];
        logger.Info($"[TYPE] Char {typeStringCharIndex - 1}/{typeStringBuffer.Length}: U+{(int)c:X4}");
        ReleaseAllVirtualKeys();
        TypeChar(c);
    }

    private void ProcessActionBuffer()
    {
        typeStringFrameCounter++;
        if (typeStringFrameCounter < typeStringFrameDelay) return;
        typeStringFrameCounter = 0;
        if (typeActionBuffer == null || typeActionIndex >= typeActionBuffer.Length)
        {
            logger.Info($"[TYPE] Action buffer terminado, {typeActionIndex} acciones");
            typeActionBuffer = null;
            typeActionIndex = 0;
            return;
        }

        ReleaseAllVirtualKeys();

        int action = typeActionBuffer[typeActionIndex++];
        switch (action)
        {
            case 1:
                PressVirtualKeyById(SpectrumKeyMap.CapsShift);
                PressVirtualKeyById(SpectrumKeyMap.J);
                logger.Info("[TYPE] Action: CapsShift+J (LOAD)");
                break;
            case 2:
                PressVirtualKeyById(SpectrumKeyMap.SymbolShift);
                PressVirtualKeyById(SpectrumKeyMap.P);
                logger.Info("[TYPE] Action: SymbolShift+P (\")");
                break;
            case 3:
                PressVirtualKeyById(SpectrumKeyMap.Enter);
                logger.Info("[TYPE] Action: Enter");
                break;
        }
    }

    private void TypeChar(char c)
    {
        if (c == '\u0001')
        {
            PressVirtualKeyById(SpectrumKeyMap.CapsShift);
            return;
        }
        if (c == '\u0002')
        {
            PressVirtualKeyById(SpectrumKeyMap.SymbolShift);
            return;
        }
        if (c == '\u0003')
        {
            PressVirtualKeyById(SpectrumKeyMap.CapsShift);
            PressVirtualKeyById(SpectrumKeyMap.SymbolShift);
            return;
        }
        if (c == '\u0010')
        {
            PressVirtualKeyById(SpectrumKeyMap.CapsShift);
            PressVirtualKeyById(SpectrumKeyMap.J);
            return;
        }
        if (c == '\u0011')
        {
            PressVirtualKeyById(SpectrumKeyMap.SymbolShift);
            PressVirtualKeyById(SpectrumKeyMap.Key2);
            return;
        }
        if (c == '\n')
        {
            PressVirtualKeyById(SpectrumKeyMap.Enter);
            return;
        }

        bool shift = char.IsUpper(c);
        char lower = char.ToLowerInvariant(c);
        int keyId = lower switch
        {
            'a' => SpectrumKeyMap.A,
            'b' => SpectrumKeyMap.B,
            'c' => SpectrumKeyMap.C,
            'd' => SpectrumKeyMap.D,
            'e' => SpectrumKeyMap.E,
            'f' => SpectrumKeyMap.F,
            'g' => SpectrumKeyMap.G,
            'h' => SpectrumKeyMap.H,
            'i' => SpectrumKeyMap.I,
            'j' => SpectrumKeyMap.J,
            'k' => SpectrumKeyMap.K,
            'l' => SpectrumKeyMap.L,
            'm' => SpectrumKeyMap.M,
            'n' => SpectrumKeyMap.N,
            'o' => SpectrumKeyMap.O,
            'p' => SpectrumKeyMap.P,
            'q' => SpectrumKeyMap.Q,
            'r' => SpectrumKeyMap.R,
            's' => SpectrumKeyMap.S,
            't' => SpectrumKeyMap.T,
            'u' => SpectrumKeyMap.U,
            'v' => SpectrumKeyMap.V,
            'w' => SpectrumKeyMap.W,
            'x' => SpectrumKeyMap.X,
            'y' => SpectrumKeyMap.Y,
            'z' => SpectrumKeyMap.Z,
            '0' => SpectrumKeyMap.Key0,
            '1' => SpectrumKeyMap.Key1,
            '2' => SpectrumKeyMap.Key2,
            '3' => SpectrumKeyMap.Key3,
            '4' => SpectrumKeyMap.Key4,
            '5' => SpectrumKeyMap.Key5,
            '6' => SpectrumKeyMap.Key6,
            '7' => SpectrumKeyMap.Key7,
            '8' => SpectrumKeyMap.Key8,
            '9' => SpectrumKeyMap.Key9,
            ' ' => SpectrumKeyMap.Space,
            _ => -1
        };

        if (keyId < 0)
        {
            switch (c)
            {
                case '!': keyId = SpectrumKeyMap.Key1; shift = true; break;
                case '"': keyId = SpectrumKeyMap.Key2; shift = true; break;
                case '#': keyId = SpectrumKeyMap.Key3; shift = true; break;
                case '$': keyId = SpectrumKeyMap.Key4; shift = true; break;
                case '%': keyId = SpectrumKeyMap.Key5; shift = true; break;
                case '&': keyId = SpectrumKeyMap.Key6; shift = true; break;
                case '\'': keyId = SpectrumKeyMap.Key7; shift = true; break;
                case '(': keyId = SpectrumKeyMap.Key8; shift = true; break;
                case ')': keyId = SpectrumKeyMap.Key9; shift = true; break;
                case ':': keyId = SpectrumKeyMap.Z; shift = true; break;
                case ';': keyId = SpectrumKeyMap.Z; break;
                case ',': keyId = SpectrumKeyMap.M; break;
                case '.': keyId = SpectrumKeyMap.N; shift = true; break;
                case '<': keyId = SpectrumKeyMap.M; shift = true; break;
                case '>': keyId = SpectrumKeyMap.N; shift = true; break;
                case '?': keyId = SpectrumKeyMap.C; shift = true; break;
                case '/': keyId = SpectrumKeyMap.V; shift = true; break;
                case '+': keyId = SpectrumKeyMap.K; shift = true; break;
                case '-': keyId = SpectrumKeyMap.J; shift = true; break;
                case '=': keyId = SpectrumKeyMap.L; shift = true; break;
                case '*': keyId = SpectrumKeyMap.B; shift = true; break;
                case '@': keyId = SpectrumKeyMap.SymbolShift; break;
                default: return;
            }
        }

        if (shift) PressVirtualKeyById(SpectrumKeyMap.CapsShift);
        PressVirtualKeyById(keyId);
    }

    private void UpdateKeyboardState()
    {
        ClearKeyboardMatrix();
        ApplyPhysicalKeyboard();
        ProcessTypeBuffer();
        ApplyVirtualKeys();
    }

    private void ApplyPhysicalKeyboard()
    {
        var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        var keys = state.GetPressedKeys();

        foreach (var key in keys)
        {
            int? keyId = MapMicrosoftXnaKeyToSpectrum(key);
            if (keyId.HasValue)
            {
                var (r, b) = SpectrumKeyMap.Get(keyId.Value);
                if (r >= 0) PressKey(r, b);
            }
        }
    }

    private static int? MapMicrosoftXnaKeyToSpectrum(Microsoft.Xna.Framework.Input.Keys k)
    {
        return k switch
        {
            Microsoft.Xna.Framework.Input.Keys.LeftShift => SpectrumKeyMap.CapsShift,
            Microsoft.Xna.Framework.Input.Keys.RightShift => SpectrumKeyMap.CapsShift,
            Microsoft.Xna.Framework.Input.Keys.LeftControl => SpectrumKeyMap.SymbolShift,
            Microsoft.Xna.Framework.Input.Keys.RightControl => SpectrumKeyMap.SymbolShift,
            Microsoft.Xna.Framework.Input.Keys.Space => SpectrumKeyMap.Space,
            Microsoft.Xna.Framework.Input.Keys.Enter => SpectrumKeyMap.Enter,
            Microsoft.Xna.Framework.Input.Keys.Back => SpectrumKeyMap.CapsShift,
            Microsoft.Xna.Framework.Input.Keys.D0 => SpectrumKeyMap.Key0,
            Microsoft.Xna.Framework.Input.Keys.D1 => SpectrumKeyMap.Key1,
            Microsoft.Xna.Framework.Input.Keys.D2 => SpectrumKeyMap.Key2,
            Microsoft.Xna.Framework.Input.Keys.D3 => SpectrumKeyMap.Key3,
            Microsoft.Xna.Framework.Input.Keys.D4 => SpectrumKeyMap.Key4,
            Microsoft.Xna.Framework.Input.Keys.D5 => SpectrumKeyMap.Key5,
            Microsoft.Xna.Framework.Input.Keys.D6 => SpectrumKeyMap.Key6,
            Microsoft.Xna.Framework.Input.Keys.D7 => SpectrumKeyMap.Key7,
            Microsoft.Xna.Framework.Input.Keys.D8 => SpectrumKeyMap.Key8,
            Microsoft.Xna.Framework.Input.Keys.D9 => SpectrumKeyMap.Key9,
            Microsoft.Xna.Framework.Input.Keys.A => SpectrumKeyMap.A,
            Microsoft.Xna.Framework.Input.Keys.B => SpectrumKeyMap.B,
            Microsoft.Xna.Framework.Input.Keys.C => SpectrumKeyMap.C,
            Microsoft.Xna.Framework.Input.Keys.D => SpectrumKeyMap.D,
            Microsoft.Xna.Framework.Input.Keys.E => SpectrumKeyMap.E,
            Microsoft.Xna.Framework.Input.Keys.F => SpectrumKeyMap.F,
            Microsoft.Xna.Framework.Input.Keys.G => SpectrumKeyMap.G,
            Microsoft.Xna.Framework.Input.Keys.H => SpectrumKeyMap.H,
            Microsoft.Xna.Framework.Input.Keys.I => SpectrumKeyMap.I,
            Microsoft.Xna.Framework.Input.Keys.J => SpectrumKeyMap.J,
            Microsoft.Xna.Framework.Input.Keys.K => SpectrumKeyMap.K,
            Microsoft.Xna.Framework.Input.Keys.L => SpectrumKeyMap.L,
            Microsoft.Xna.Framework.Input.Keys.M => SpectrumKeyMap.M,
            Microsoft.Xna.Framework.Input.Keys.N => SpectrumKeyMap.N,
            Microsoft.Xna.Framework.Input.Keys.O => SpectrumKeyMap.O,
            Microsoft.Xna.Framework.Input.Keys.P => SpectrumKeyMap.P,
            Microsoft.Xna.Framework.Input.Keys.Q => SpectrumKeyMap.Q,
            Microsoft.Xna.Framework.Input.Keys.R => SpectrumKeyMap.R,
            Microsoft.Xna.Framework.Input.Keys.S => SpectrumKeyMap.S,
            Microsoft.Xna.Framework.Input.Keys.T => SpectrumKeyMap.T,
            Microsoft.Xna.Framework.Input.Keys.U => SpectrumKeyMap.U,
            Microsoft.Xna.Framework.Input.Keys.V => SpectrumKeyMap.V,
            Microsoft.Xna.Framework.Input.Keys.W => SpectrumKeyMap.W,
            Microsoft.Xna.Framework.Input.Keys.X => SpectrumKeyMap.X,
            Microsoft.Xna.Framework.Input.Keys.Y => SpectrumKeyMap.Y,
            Microsoft.Xna.Framework.Input.Keys.Z => SpectrumKeyMap.Z,
            _ => null
        };
    }

    private bool TryFastLoadBlock()
    {
        if (tapeFile == null || fastLoadBlockIndex >= tapeFile.Blocks.Count)
            return false;

        var block = tapeFile.Blocks[fastLoadBlockIndex];
        byte[]? data = null;

        if (block is TapBlock tapBlock) data = tapBlock.Data;
        else if (block is StandardDataBlock stdBlock) data = stdBlock.Data;
        if (data == null || data.Length == 0) return false;

        byte flag = data[0];
        int dataLen = data.Length - 1;

        for (int i = 0; i < dataLen; i++)
        {
            int dummyCycles = 0;
            memoryBus.Write(cpu.IX, data[i + 1], ref dummyCycles);
            cpu.IX++;
        }

        cpu.A = flag;
        cpu.FlagC = true;
        cpu.FlagZ = false;
        cpu.FlagS = false;
        cpu.FlagPV = false;

        int dummy = 0;
        byte spL = memoryBus.Read(cpu.SP, ref dummy);
        byte spH = memoryBus.Read((ushort)(cpu.SP + 1), ref dummy);
        cpu.SP += 2;
        cpu.PC = (ushort)((spH << 8) | spL);

        fastLoadBlockIndex++;
        logger.Info($"[FASTLOAD] Block {fastLoadBlockIndex}: flag=0x{flag:X2}, {dataLen} bytes loaded");
        return true;
    }
}
