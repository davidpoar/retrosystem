using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class ActionButtonsView : FrameLayout
{
    public delegate void KeyEventHandler(int keyId, bool pressed);
    public event KeyEventHandler? KeyChanged;
    public event Action? KeyboardToggleRequested;
    public event Action? TapeToggleRequested;

    private static readonly Color BgNormalA = Color.Argb(0xEE, 0x40, 0xC0, 0x40);
    private static readonly Color BgPressedA = Color.Argb(0xFF, 0xA0, 0xFF, 0xA0);
    private static readonly Color BgNormalB = Color.Argb(0xEE, 0xC0, 0x30, 0x30);
    private static readonly Color BgPressedB = Color.Argb(0xFF, 0xFF, 0x80, 0x80);
    private static readonly Color BgSmall   = Color.Argb(0xEE, 0x40, 0x40, 0x80);
    private static readonly Color BgPressedSmall = Color.Argb(0xFF, 0xA0, 0xA0, 0xFF);

    private readonly HashSet<int> _pressedKeys = new();
    private readonly Context _ctx;

    private Button _btnA = null!;
    private Button _btnB = null!;
    private Button _btnKbd = null!;
    private Button _btnTape = null!;

    public ActionButtonsView(Context context) : base(context)
    {
        _ctx = context;
        Initialize();
    }
    public ActionButtonsView(Context context, IAttributeSet? attrs) : base(context, attrs)
    {
        _ctx = context;
        Initialize();
    }
    public ActionButtonsView(Context context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
    {
        _ctx = context;
        Initialize();
    }

    private void Initialize()
    {
        SetBackgroundColor(Color.Transparent);
        LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        _btnA    = CreateKeyButton("A", SpectrumKeyMap.Space, BgNormalA, BgPressedA);
        _btnB    = CreateKeyButton("B", SpectrumKeyMap.M,      BgNormalB, BgPressedB);
        _btnKbd  = CreateActionButton("KBD", BgSmall, BgPressedSmall, () => KeyboardToggleRequested?.Invoke());
        _btnTape = CreateActionButton("TAPE", BgSmall, BgPressedSmall, () => TapeToggleRequested?.Invoke());

        AddView(_btnA);
        AddView(_btnB);
        AddView(_btnKbd);
        AddView(_btnTape);
    }

    private Button CreateKeyButton(string label, int keyId, Color bgNormal, Color bgPressed)
    {
        var btn = new Button(Context) { Text = label };
        btn.SetBackgroundColor(bgNormal);
        btn.SetTextColor(Color.White);
        btn.Elevation = 8;
        btn.SetShadowLayer(6, 3, 3, Color.Black);
        btn.Tag = keyId;

        btn.Touch += (s, e) =>
        {
            var ev = e.Event;
            if (ev == null) return;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                    btn.SetBackgroundColor(bgPressed);
                    Haptic.Tap(_ctx);
                    Press(keyId);
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    btn.SetBackgroundColor(bgNormal);
                    Release(keyId);
                    break;
            }
        };
        return btn;
    }

    private Button CreateActionButton(string label, Color bgNormal, Color bgPressed, Action onClick)
    {
        var btn = new Button(Context) { Text = label };
        btn.SetBackgroundColor(bgNormal);
        btn.SetTextColor(Color.White);
        btn.Elevation = 6;
        btn.SetShadowLayer(4, 2, 2, Color.Black);

        btn.Touch += (s, e) =>
        {
            var ev = e.Event;
            if (ev == null) return;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                    btn.SetBackgroundColor(bgPressed);
                    Haptic.Tap(_ctx);
                    break;
                case MotionEventActions.Up:
                    btn.SetBackgroundColor(bgNormal);
                    if (ev.EventTime - ev.DownTime < 500) onClick();
                    break;
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    btn.SetBackgroundColor(bgNormal);
                    break;
            }
        };
        return btn;
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

        int parentW = MeasureSpec.GetSize(widthMeasureSpec);
        int parentH = MeasureSpec.GetSize(heightMeasureSpec);
        if (parentW <= 0 || parentH <= 0) return;

        int baseSize = Math.Min(parentW, parentH);
        int btnSize = (int)(baseSize * 0.18f);
        int smallSize = (int)(baseSize * 0.10f);

        int spec = MeasureSpec.MakeMeasureSpec(btnSize, MeasureSpecMode.Exactly);
        int smallSpec = MeasureSpec.MakeMeasureSpec(smallSize, MeasureSpecMode.Exactly);
        _btnA.Measure(spec, spec);
        _btnB.Measure(spec, spec);
        _btnKbd.Measure(smallSpec, smallSpec);
        _btnTape.Measure(smallSpec, smallSpec);

        SetMeasuredDimension(parentW, parentH);
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        int parentW = right - left;
        int parentH = bottom - top;
        if (parentW <= 0 || parentH <= 0) return;

        int baseSize = Math.Min(parentW, parentH);
        int btnSize = (int)(baseSize * 0.18f);
        int smallSize = (int)(baseSize * 0.10f);
        int gap = btnSize / 2;

        int aLeft = parentW - btnSize - smallSize;
        int aTop = (parentH / 2) - btnSize - gap / 2;

        int bLeft = aLeft - btnSize - gap;
        int bTop = (parentH / 2) + gap / 2;

        _btnA.Layout(aLeft, aTop, aLeft + btnSize, aTop + btnSize);
        _btnB.Layout(bLeft, bTop, bLeft + btnSize, bTop + btnSize);

        _btnA.TextSize = btnSize * 0.40f;
        _btnB.TextSize = btnSize * 0.40f;

        int kbdLeft = parentW - smallSize - smallSize / 4;
        int kbdTop = parentH - smallSize - smallSize / 4;
        _btnKbd.Layout(kbdLeft, kbdTop, kbdLeft + smallSize, kbdTop + smallSize);
        _btnKbd.TextSize = smallSize * 0.22f;

        int tapeLeft = kbdLeft - smallSize - smallSize / 4;
        int tapeTop = kbdTop;
        _btnTape.Layout(tapeLeft, tapeTop, tapeLeft + smallSize, tapeTop + smallSize);
        _btnTape.TextSize = smallSize * 0.22f;
    }

    private void Press(int keyId)
    {
        if (_pressedKeys.Add(keyId)) KeyChanged?.Invoke(keyId, true);
    }

    private void Release(int keyId)
    {
        if (_pressedKeys.Remove(keyId)) KeyChanged?.Invoke(keyId, false);
    }

    public void ReleaseAll()
    {
        foreach (var id in _pressedKeys) KeyChanged?.Invoke(id, false);
        _pressedKeys.Clear();

        _btnA?.SetBackgroundColor(BgNormalA);
        _btnB?.SetBackgroundColor(BgNormalB);
    }
}
