using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class SpectrumKeyboardView : FrameLayout
{
    public delegate void KeyEventHandler(int keyId, bool pressed);
    public event KeyEventHandler? KeyChanged;
    public event Action? CloseRequested;

    private static readonly Color BgNormal   = Color.Argb(0xEE, 0x22, 0x22, 0x44);
    private static readonly Color BgPressed  = Color.Argb(0xFF, 0x80, 0xE0, 0x80);
    private static readonly Color BgModifier = Color.Argb(0xEE, 0x80, 0x40, 0x10);
    private static readonly Color BgModOn    = Color.Argb(0xFF, 0xFF, 0xA0, 0x40);
    private static readonly Color BgSpecial  = Color.Argb(0xEE, 0x40, 0x40, 0x70);

    private readonly Context _ctx;
    private readonly List<Button> _allButtons = new();
    private readonly Dictionary<int, Button> _keyButtons = new();
    private Button? _btnShift;
    private Button? _btnSym;
    private Button? _btnEnter;
    private Button? _btnSpace;
    private Button? _btnClose;

    private bool _shiftDown = false;
    private bool _symDown = false;

    private static readonly (string label, int unshiftedId, int shiftedId, int symId)[][] KeyLayout = new[]
    {
        new (string, int, int, int)[] {
            ("1", SpectrumKeyMap.Key1, -1, SpectrumKeyMap.SymbolShift),
            ("2", SpectrumKeyMap.Key2, -1, SpectrumKeyMap.SymbolShift),
            ("3", SpectrumKeyMap.Key3, -1, SpectrumKeyMap.SymbolShift),
            ("4", SpectrumKeyMap.Key4, -1, SpectrumKeyMap.SymbolShift),
            ("5", SpectrumKeyMap.Key5, -1, SpectrumKeyMap.SymbolShift),
            ("6", SpectrumKeyMap.Key6, -1, SpectrumKeyMap.SymbolShift),
            ("7", SpectrumKeyMap.Key7, -1, SpectrumKeyMap.SymbolShift),
            ("8", SpectrumKeyMap.Key8, -1, SpectrumKeyMap.SymbolShift),
            ("9", SpectrumKeyMap.Key9, -1, SpectrumKeyMap.SymbolShift),
            ("0", SpectrumKeyMap.Key0, -1, SpectrumKeyMap.SymbolShift),
        },
        new (string, int, int, int)[] {
            ("Q", SpectrumKeyMap.Q, -1, SpectrumKeyMap.SymbolShift),
            ("W", SpectrumKeyMap.W, -1, SpectrumKeyMap.SymbolShift),
            ("E", SpectrumKeyMap.E, -1, SpectrumKeyMap.SymbolShift),
            ("R", SpectrumKeyMap.R, -1, SpectrumKeyMap.SymbolShift),
            ("T", SpectrumKeyMap.T, -1, SpectrumKeyMap.SymbolShift),
            ("Y", SpectrumKeyMap.Y, -1, SpectrumKeyMap.SymbolShift),
            ("U", SpectrumKeyMap.U, -1, SpectrumKeyMap.SymbolShift),
            ("I", SpectrumKeyMap.I, -1, SpectrumKeyMap.SymbolShift),
            ("O", SpectrumKeyMap.O, -1, SpectrumKeyMap.SymbolShift),
            ("P", SpectrumKeyMap.P, -1, SpectrumKeyMap.SymbolShift),
        },
        new (string, int, int, int)[] {
            ("A", SpectrumKeyMap.A, -1, SpectrumKeyMap.SymbolShift),
            ("S", SpectrumKeyMap.S, -1, SpectrumKeyMap.SymbolShift),
            ("D", SpectrumKeyMap.D, -1, SpectrumKeyMap.SymbolShift),
            ("F", SpectrumKeyMap.F, -1, SpectrumKeyMap.SymbolShift),
            ("G", SpectrumKeyMap.G, -1, SpectrumKeyMap.SymbolShift),
            ("H", SpectrumKeyMap.H, -1, SpectrumKeyMap.SymbolShift),
            ("J", SpectrumKeyMap.J, -1, SpectrumKeyMap.SymbolShift),
            ("K", SpectrumKeyMap.K, -1, SpectrumKeyMap.SymbolShift),
            ("L", SpectrumKeyMap.L, -1, SpectrumKeyMap.SymbolShift),
        },
        new (string, int, int, int)[] {
            ("Z", SpectrumKeyMap.Z, -1, SpectrumKeyMap.SymbolShift),
            ("X", SpectrumKeyMap.X, -1, SpectrumKeyMap.SymbolShift),
            ("C", SpectrumKeyMap.C, -1, SpectrumKeyMap.SymbolShift),
            ("V", SpectrumKeyMap.V, -1, SpectrumKeyMap.SymbolShift),
            ("B", SpectrumKeyMap.B, -1, SpectrumKeyMap.SymbolShift),
            ("N", SpectrumKeyMap.N, -1, SpectrumKeyMap.SymbolShift),
            ("M", SpectrumKeyMap.M, -1, SpectrumKeyMap.SymbolShift),
        },
    };

    public SpectrumKeyboardView(Context context) : base(context)
    {
        _ctx = context;
        Initialize();
    }

    private void Initialize()
    {
        SetBackgroundColor(Color.Argb(0xEE, 0x10, 0x10, 0x20));
        Visibility = ViewStates.Gone;

        var main = new TableLayout(Context);
        main.SetBackgroundColor(Color.Argb(0xEE, 0x10, 0x10, 0x20));
        main.LayoutParameters = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent,
            GravityFlags.Center);

        var lp = new TableLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(4, 4, 4, 4);
        main.LayoutParameters = lp;

        foreach (var row in KeyLayout)
        {
            var tr = new TableRow(Context);
            tr.LayoutParameters = new TableLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);

            foreach (var (label, baseId, _, _) in row)
            {
                var btn = CreateKeyButton(label, baseId, BgNormal);
                tr.AddView(btn);
            }
            main.AddView(tr);
        }

        var modifierRow = new TableRow(Context);
        modifierRow.LayoutParameters = new TableLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        _btnShift = CreateKeyButton("SHIFT", SpectrumKeyMap.CapsShift, BgModifier);
        _btnSym   = CreateKeyButton("SYM", SpectrumKeyMap.SymbolShift, BgModifier);
        _btnEnter = CreateKeyButton("ENTER", SpectrumKeyMap.Enter, BgSpecial);
        _btnSpace = CreateKeyButton("SPACE", SpectrumKeyMap.Space, BgSpecial);
        _btnClose = new Button(Context) { Text = "X" };
        _btnClose.SetBackgroundColor(Color.Argb(0xEE, 0xC0, 0x30, 0x30));
        _btnClose.SetTextColor(Color.White);
        _btnClose.SetShadowLayer(2, 1, 1, Color.Black);

        _btnClose.Touch += (s, e) =>
        {
            var ev = e.Event;
            if (ev == null) return;
            if (ev.ActionMasked == MotionEventActions.Up)
                CloseRequested?.Invoke();
        };

        modifierRow.AddView(_btnShift);
        modifierRow.AddView(_btnSpace);
        modifierRow.AddView(_btnEnter);
        modifierRow.AddView(_btnSym);
        modifierRow.AddView(_btnClose);

        main.AddView(modifierRow);

        AddView(main);
    }

    private Button CreateKeyButton(string label, int keyId, Color bg)
    {
        var btn = new Button(Context) { Text = label };
        btn.SetAllCaps(false);
        btn.SetBackgroundColor(bg);
        btn.SetTextColor(Color.White);
        btn.SetShadowLayer(2, 1, 1, Color.Black);
        btn.Elevation = 4;

        var param = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        param.SetMargins(2, 2, 2, 2);
        btn.LayoutParameters = param;

        btn.Touch += (s, e) =>
        {
            var ev = e.Event;
            if (ev == null) return;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                    btn.SetBackgroundColor(BgPressed);
                    Haptic.Tap(_ctx);
                    FireKey(keyId, true);
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    if (keyId == SpectrumKeyMap.CapsShift || keyId == SpectrumKeyMap.SymbolShift)
                    {
                        btn.SetBackgroundColor(_shiftDown || _symDown ? BgModOn : BgModifier);
                    }
                    else
                    {
                        btn.SetBackgroundColor(bg);
                    }
                    FireKey(keyId, false);
                    break;
            }
        };

        _allButtons.Add(btn);
        _keyButtons[keyId] = btn;
        return btn;
    }

    private void FireKey(int keyId, bool pressed)
    {
        if (keyId == SpectrumKeyMap.CapsShift)
        {
            _shiftDown = pressed;
            if (_btnShift != null) _btnShift.SetBackgroundColor(pressed ? BgModOn : BgModifier);
        }
        else if (keyId == SpectrumKeyMap.SymbolShift)
        {
            _symDown = pressed;
            if (_btnSym != null) _btnSym.SetBackgroundColor(pressed ? BgModOn : BgModifier);
        }
        KeyChanged?.Invoke(keyId, pressed);
    }

    public void Show()
    {
        Visibility = ViewStates.Visible;
        RequestLayout();
    }

    public void Hide()
    {
        _shiftDown = false;
        _symDown = false;
        if (_btnShift != null) _btnShift.SetBackgroundColor(BgModifier);
        if (_btnSym != null) _btnSym.SetBackgroundColor(BgModifier);
        Visibility = ViewStates.Gone;
    }

    public bool IsVisible => Visibility == ViewStates.Visible;

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        int parentW = MeasureSpec.GetSize(widthMeasureSpec);
        int parentH = MeasureSpec.GetSize(heightMeasureSpec);
        SetMeasuredDimension(parentW, parentH);
    }
}
