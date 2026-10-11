using Android.App;
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
        Icon = "@drawable/icon",
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
        private View? _gameView;
        private FrameLayout? _gameContainer;
        private LinearLayout? _topRow;
        private LinearLayout? _root;

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
            root.AddView(_topRow, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0, 1f));
            root.AddView(_keyboard, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0, 0.45f));

            SetContentView(root);

            _gameContainer.Post(ApplyLayout);

            _game.Run();
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
            if (_keyboard.IsVisible) _keyboard.Hide();
            else _keyboard.Show();
            ApplyLayout();
        }

        protected override void OnPause()
        {
            _dpad?.ReleaseAll();
            _buttons?.ReleaseAll();
            base.OnPause();
        }

        protected override void OnDestroy()
        {
            _dpad?.ReleaseAll();
            _buttons?.ReleaseAll();
            _game?.Exit();
            base.OnDestroy();
        }
    }
}
