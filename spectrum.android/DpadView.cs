using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class DpadView : FrameLayout
{
    public delegate void KeyEventHandler(int keyId, bool pressed);
    public event KeyEventHandler? KeyChanged;

    private static readonly Color BgNormal = Color.Argb(0xEE, 0x25, 0x25, 0x55);
    private static readonly Color BgPressed = Color.Argb(0xFF, 0x60, 0xE0, 0x70);
    private static readonly Color BgCenter = Color.Argb(0x88, 0x18, 0x18, 0x30);

    private readonly HashSet<int> _pressedKeys = new();
    private readonly Context _ctx;
    private RelativeLayout _cross = null!;
    private Button _btnUp = null!, _btnDown = null!, _btnLeft = null!, _btnRight = null!;

    public DpadView(Context context) : base(context) { _ctx = context; Initialize(); }
    public DpadView(Context context, IAttributeSet? attrs) : base(context, attrs) { _ctx = context; Initialize(); }
    public DpadView(Context context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { _ctx = context; Initialize(); }

    private void Initialize()
    {
        SetBackgroundColor(Color.Transparent);
        LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        _cross = new RelativeLayout(Context);
        _cross.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Left | GravityFlags.CenterVertical
        };
        AddView(_cross);

        _btnUp    = CreateButton("▲", SpectrumKeyMap.Q);
        _btnDown  = CreateButton("▼", SpectrumKeyMap.A);
        _btnLeft  = CreateButton("◄", SpectrumKeyMap.O);
        _btnRight = CreateButton("►", SpectrumKeyMap.P);

        _cross.AddView(_btnUp);
        _cross.AddView(_btnDown);
        _cross.AddView(_btnLeft);
        _cross.AddView(_btnRight);
    }

    private Button CreateButton(string label, int keyId)
    {
        var btn = new Button(Context) { Text = label };
        btn.SetBackgroundColor(BgNormal);
        btn.SetTextColor(Color.White);
        btn.Elevation = 8;
        btn.SetShadowLayer(4, 2, 2, Color.Black);
        btn.Tag = keyId;

        btn.Touch += (s, e) =>
        {
            var ev = e.Event;
            if (ev == null) return;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                    btn.SetBackgroundColor(BgPressed);
                    Haptic.Tap(_ctx);
                    Press(keyId);
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    btn.SetBackgroundColor(BgNormal);
                    Release(keyId);
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

        int side = (int)(parentW * 0.85f);
        int btnSize = side / 3;
        int gap = btnSize / 4;

        int crossW = btnSize * 3 + gap * 2;
        int crossH = btnSize * 3 + gap * 2;

        _cross.Measure(
            MeasureSpec.MakeMeasureSpec(crossW, MeasureSpecMode.Exactly),
            MeasureSpec.MakeMeasureSpec(crossH, MeasureSpecMode.Exactly));

        SetMeasuredDimension(parentW, parentH);
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        int parentW = right - left;
        int parentH = bottom - top;
        if (parentW <= 0 || parentH <= 0) return;

        int side = (int)(parentW * 0.85f);
        int btnSize = side / 3;
        int gap = btnSize / 4;

        int crossW = btnSize * 3 + gap * 2;
        int crossH = btnSize * 3 + gap * 2;

        int crossLeft = (parentW - crossW) / 2;
        int crossTop = (parentH - crossH) / 2;

        _cross.Layout(crossLeft, crossTop, crossLeft + crossW, crossTop + crossH);

        LayoutButton(_btnUp, btnSize, btnSize + gap, 0);
        LayoutButton(_btnDown, btnSize, btnSize + gap, (btnSize + gap) * 2);
        LayoutButton(_btnLeft, btnSize, 0, btnSize + gap);
        LayoutButton(_btnRight, btnSize, (btnSize + gap) * 2, btnSize + gap);
    }

    private static void LayoutButton(Button btn, int size, int l, int t)
    {
        btn.Layout(l, t, l + size, t + size);
        btn.TextSize = size * 0.35f;
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
        if (_btnUp != null) _btnUp.SetBackgroundColor(BgNormal);
        if (_btnDown != null) _btnDown.SetBackgroundColor(BgNormal);
        if (_btnLeft != null) _btnLeft.SetBackgroundColor(BgNormal);
        if (_btnRight != null) _btnRight.SetBackgroundColor(BgNormal);
    }
}
