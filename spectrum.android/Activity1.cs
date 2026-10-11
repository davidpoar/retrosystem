using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using retro.spectrum;

namespace retro.spectrum.android
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@mipmap/ic_launcher",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class Activity1 : AndroidGameActivity
    {
        private SpectrumGame? _game;
        private DpadView? _dpad;
        private ActionButtonsView? _buttons;
        private SpectrumKeyboardView? _keyboard;
        private MenuOverlayView? _menu;
        private View? _gameView;
        private FrameLayout? _gameContainer;
        private LinearLayout? _topRow;
        private TextView? _statusTape;
        private LinearLayout? _root;

        private bool _fastLoadEnabled = false;

        protected override void OnCreate(Bundle? bundle)
        {
            base.OnCreate(bundle);

            RequestWindowFeature(WindowFeatures.NoTitle);
            var window = Window;
            if (window != null)
            {
                window.AddFlags(WindowManagerFlags.Fullscreen);
                window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }

            _game = new SpectrumGame(
                tzxPath: null,
                fastLoad: false,
                romName: null,
                logger: new AndroidLogProvider(),
                assets: new AndroidAssetProvider(this));

            var gameView = _game.Services.GetService(typeof(View)) as View;
            _gameView = gameView;

            _dpad = new DpadView(this);
            _dpad.KeyChanged += OnKeyChanged;

            _buttons = new ActionButtonsView(this);
            _buttons.KeyChanged += OnKeyChanged;
            _buttons.KeyboardToggleRequested += ToggleKeyboard;
            _buttons.TapeToggleRequested += () => _game?.ToggleTape();

            _keyboard = new SpectrumKeyboardView(this);
            _keyboard.KeyChanged += OnKeyChanged;
            _keyboard.CloseRequested += () =>
            {
                _keyboard.Hide();
                ApplyLayout();
            };

            _menu = new MenuOverlayView(this);
            _menu.ActionRequested += OnMenuAction;
            _menu.DismissRequested += () => _menu.Hide();
            _menu.SetFastLoadState(_fastLoadEnabled);

            _statusTape = new TextView(this)
            {
                TextSize = 12,
                Gravity = GravityFlags.Start | GravityFlags.CenterVertical
            };
            _statusTape.SetTextColor(Android.Graphics.Color.Argb(0xFF, 0x60, 0xE0, 0x70));
            _statusTape.SetPadding(dp(12), 0, 0, 0);
            _statusTape.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
            _statusTape.Text = "⏹ (sin cinta)";

            var btnMenuTopBar = new Button(this) { Text = "☰ LOAD" };
            btnMenuTopBar.SetBackgroundColor(Android.Graphics.Color.Argb(0xEE, 0x40, 0xA0, 0x40));
            btnMenuTopBar.SetTextColor(Android.Graphics.Color.White);
            btnMenuTopBar.SetTextSize(Android.Util.ComplexUnitType.Sp, 12);
            btnMenuTopBar.SetAllCaps(false);
            btnMenuTopBar.SetPadding(dp(8), 0, dp(8), 0);
            btnMenuTopBar.SetShadowLayer(2, 1, 1, Android.Graphics.Color.Black);
            btnMenuTopBar.Click += (s, e) => FilePickerHelper.LaunchTapePicker(this);

            var btnMenuButton = new Button(this) { Text = "☰" };
            btnMenuButton.SetBackgroundColor(Android.Graphics.Color.Argb(0xEE, 0x40, 0x40, 0x70));
            btnMenuButton.SetTextColor(Android.Graphics.Color.White);
            btnMenuButton.SetTextSize(Android.Util.ComplexUnitType.Sp, 18);
            btnMenuButton.SetAllCaps(false);
            btnMenuButton.SetShadowLayer(2, 1, 1, Android.Graphics.Color.Black);
            btnMenuButton.Click += (s, e) => _menu?.Show();

            var statusBar = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal
            };
            statusBar.SetGravity(GravityFlags.CenterVertical);
            statusBar.SetBackgroundColor(Android.Graphics.Color.Argb(0xEE, 0x05, 0x05, 0x15));
            statusBar.LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dp(40));
            statusBar.AddView(_statusTape, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 1f));
            statusBar.AddView(btnMenuTopBar, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.MatchParent));
            statusBar.AddView(btnMenuButton, new LinearLayout.LayoutParams(
                dp(44), ViewGroup.LayoutParams.MatchParent));

            _gameContainer = new FrameLayout(this);
            if (_gameView != null)
            {
                _gameContainer.AddView(_gameView, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent,
                    GravityFlags.Center));
            }

            _topRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            _dpad.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 0.22f);
            _buttons.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 0.22f);
            _gameContainer.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 0.56f);

            _topRow.AddView(_dpad);
            _topRow.AddView(_gameContainer);
            _topRow.AddView(_buttons);

            var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
            _root = root;
            root.AddView(statusBar, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            root.AddView(_topRow, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 1f));
            root.AddView(_keyboard, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 0.40f));

            var frameRoot = new FrameLayout(this);
            frameRoot.AddView(root, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
            frameRoot.AddView(_menu, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

            SetContentView(frameRoot);

            _gameContainer.Post(ApplyLayout);

            _game.Run();

            _root.Post(UpdateStatusBar);
        }

        private void OnMenuAction(MenuOverlayView.MenuAction action)
        {
            switch (action)
            {
                case MenuOverlayView.MenuAction.LoadTape:
                    _menu?.Hide();
                    FilePickerHelper.LaunchTapePicker(this);
                    break;
                case MenuOverlayView.MenuAction.TypeLoad:
                    _game?.TypeLoadCommand();
                    _menu?.Hide();
                    break;
                case MenuOverlayView.MenuAction.ToggleFastLoad:
                    _fastLoadEnabled = !_fastLoadEnabled;
                    _game?.SetFastLoad(_fastLoadEnabled);
                    _menu?.SetFastLoadState(_fastLoadEnabled);
                    break;
                case MenuOverlayView.MenuAction.ToggleKeyboard:
                    ToggleKeyboard();
                    _menu?.Hide();
                    break;
                case MenuOverlayView.MenuAction.Reset:
                    _game?.ResetCpu();
                    _menu?.Hide();
                    break;
                case MenuOverlayView.MenuAction.Help:
                    ShowHelp();
                    break;
            }
        }

        private void ShowHelp()
        {
            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Ayuda - ZX Spectrum 48K")
                .SetMessage(
                    "CONTROLES TÁCTILES\n" +
                    "  D-pad:  ↑Q  ↓A  ←O  →P  (joystick QAOP)\n" +
                    "  A (verde):  SPACE   (acción principal)\n" +
                    "  B (rojo):   M        (acción secundaria)\n" +
                    "  KBD:  muestra/oculta el teclado QWERTY\n" +
                    "  TAPE:  play/pause de la cinta\n\n" +
                    "MENÚ (icono arriba a la derecha)\n" +
                    "  Cargar cinta:  abre el selector de archivos\n" +
                    "  Teclear LOAD:  auto-escribe LOAD\"\" + Enter\n" +
                    "  Fast Load:  carga cintas sin esperar el audio\n" +
                    "  Reset:  reinicia el Spectrum\n\n" +
                    "ATAJOS\n" +
                    "  Para cargar un juego:\n" +
                    "  1) Pulsa 'Cargar cinta' y elige el .tap/.tzx\n" +
                    "  2) Activa 'Fast Load: ON' para carga rápida\n" +
                    "  3) Pulsa 'Teclear LOAD' o escribe LOAD\"\" + Enter\n" +
                    "  4) La pantalla mostrará 'Loading...'\n\n" +
                    "(c) 1982 Sinclair Research Ltd (Amstrad plc)\n" +
                    "ROM distribuida con permiso de Amstrad")
                .SetPositiveButton("Cerrar", (s, e) => { })
                .Create();
            dlg.Show();
        }

        private void UpdateStatusBar()
        {
            if (_statusTape == null || _game == null) return;
            if (!_game.IsRomLoaded)
            {
                _statusTape.Text = "⚠ ROM no encontrada";
                _statusTape.SetTextColor(Android.Graphics.Color.Argb(0xFF, 0xFF, 0x80, 0x40));
            }
            else if (_game.HasTape)
            {
                string icon = _game.IsTapePlaying ? "▶" : "⏸";
                _statusTape.Text = $"{icon} {_game.LoadedTapeName}";
                _statusTape.SetTextColor(Android.Graphics.Color.Argb(0xFF, 0x60, 0xE0, 0x70));
            }
            else
            {
                _statusTape.Text = "⏹ (sin cinta) — pulsa LOAD";
                _statusTape.SetTextColor(Android.Graphics.Color.Argb(0xFF, 0xC0, 0xC0, 0xC0));
            }
            _root?.PostDelayed(UpdateStatusBar, 500);
        }

        private void ApplyLayout()
        {
            var container = _gameContainer;
            var view = _gameView;
            if (container == null || view == null) return;
            int w = container.Width;
            int h = container.Height;
            if (w <= 0 || h <= 0) return;

            float targetRatio = 4f / 3f;
            int newW = w;
            int newH = (int)(w / targetRatio);
            if (newH > h)
            {
                newH = h;
                newW = (int)(h * targetRatio);
            }
            var lp = new FrameLayout.LayoutParams(newW, newH, GravityFlags.Center);
            view.LayoutParameters = lp;
        }

        private void OnKeyChanged(int keyId, bool pressed)
        {
            if (_game == null) return;
            if (pressed) _game.PressVirtualKeyById(keyId);
            else _game.ReleaseVirtualKeyById(keyId);
        }

        private void ToggleKeyboard()
        {
            if (_keyboard == null) return;
            if (_keyboard.IsVisible)
            {
                _keyboard.Hide();
            }
            else
            {
                _keyboard.Show();
            }
            ApplyLayout();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == FilePickerHelper.RequestCodeLoadTape)
            {
                var (ok, fileName, localPath, error) = FilePickerHelper.HandleResult(this, requestCode, resultCode, data);
                if (ok && localPath != null)
                {
                    Dbg.I($"Cinta seleccionada: {fileName} -> {localPath}");
                    if (_game != null)
                    {
                        bool loaded = _game.LoadTapeFromPath(localPath);
                        if (loaded)
                        {
                            _game.ToggleTape();
                        }
                    }
                    UpdateStatusBar();
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    Dbg.E($"Error al cargar cinta: {error}");
                    ShowToast($"Error: {error}");
                }
            }
        }

        private void ShowToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Short)?.Show();
        }

        protected override void OnPause()
        {
            _dpad?.ReleaseAll();
            _buttons?.ReleaseAll();
            _menu?.Hide();
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _root?.Post(UpdateStatusBar);
        }

        protected override void OnDestroy()
        {
            _dpad?.ReleaseAll();
            _buttons?.ReleaseAll();
            _menu?.Hide();
            _game?.Exit();
            base.OnDestroy();
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                if (_menu != null && _menu.IsVisible)
                {
                    _menu.Hide();
                    return true;
                }
                if (_keyboard != null && _keyboard.IsVisible)
                {
                    _keyboard.Hide();
                    ApplyLayout();
                    return true;
                }
            }
            return base.OnKeyDown(keyCode, e);
        }

        private int dp(int v)
        {
            float density = Resources.DisplayMetrics.Density;
            return (int)(v * density + 0.5f);
        }
    }
}
