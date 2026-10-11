using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class SystemPanelView : LinearLayout
{
    public event Action? KeyboardToggleRequested;
    public event Action? TapeToggleRequested;
    public event Action? MenuRequested;

    private static readonly Color BgNormal = Color.Argb(0xEE, 0x18, 0x1F, 0x33);
    private static readonly Color BgPressed = Color.Argb(0xFF, 0x60, 0xE0, 0x70);

    private readonly Context _ctx;
    private ImageButton? _btnKbd, _btnTape, _btnMenu;

    public SystemPanelView(Context context) : base(context)
    {
        _ctx = context;
        Initialize();
    }

    private void Initialize()
    {
        Orientation = Orientation.Horizontal;
        SetBackgroundColor(Color.Argb(0xCC, 0x10, 0x14, 0x22));
        SetPadding(dp(4), dp(4), dp(4), dp(4));

        _btnKbd = MakeIconButton("KBD", 0xE0, 0xA0);
        _btnTape = MakeIconButton("TAPE", 0x40, 0xC0);
        _btnMenu = MakeIconButton("☰", 0xC0, 0xC0);

        if (_btnKbd != null)
        {
            _btnKbd.Click += (s, e) => KeyboardToggleRequested?.Invoke();
        }
        if (_btnTape != null)
        {
            _btnTape.Click += (s, e) => TapeToggleRequested?.Invoke();
        }
        if (_btnMenu != null)
        {
            _btnMenu.Click += (s, e) => MenuRequested?.Invoke();
        }
    }

    private ImageButton MakeIconButton(string label, byte r, byte g)
    {
        var btn = new ImageButton(_ctx);
        btn.SetBackgroundColor(Color.Argb(0x00, 0, 0, 0));
        btn.SetImageDrawable(TextDrawable(label, Color.Argb(0xFF, r, g, (byte)0x60)));
        btn.SetPadding(dp(6), dp(6), dp(6), dp(6));
        var lp = new LayoutParams(dp(40), dp(40));
        lp.SetMargins(dp(2), 0, dp(2), 0);
        btn.LayoutParameters = lp;
        AddView(btn);
        return btn;
    }

    private Drawable TextDrawable(string text, Color color)
    {
        var drawable = new TextDrawableImpl(text, color, dp(11), 0, 0, 0);
        return drawable;
    }

    public void SetTapePlaying(bool playing)
    {
        if (_btnTape == null) return;
        var drawable = playing
            ? new TextDrawableImpl("❚❚", Color.Argb(0xFF, 0xE0, 0x80, 0x40), dp(11), 0, 0, 0)
            : new TextDrawableImpl("TAPE", Color.Argb(0xFF, 0x40, 0xC0, 0x60), dp(11), 0, 0, 0);
        _btnTape.SetImageDrawable(drawable);
        _btnTape.Invalidate();
    }

    private int dp(int v) => (int)(v * Resources.DisplayMetrics.Density + 0.5f);
}

internal sealed class TextDrawableImpl : Drawable
{
    private readonly string _text;
    private readonly Color _color;
    private readonly float _textSize;
    private readonly Paint _paint;

    public TextDrawableImpl(string text, Color color, float textSize, int leftPad, int topPad, int rightPad)
    {
        _text = text;
        _color = color;
        _textSize = textSize;
        _paint = new Paint(PaintFlags.AntiAlias);
        _paint.Color = color;
        _paint.TextSize = textSize;
        _paint.TextAlign = Paint.Align.Center;
        _paint.SetTypeface(Android.Graphics.Typeface.Create("sans-serif", Android.Graphics.TypefaceStyle.Bold));
    }

    public override void Draw(Canvas canvas)
    {
        var bounds = Bounds;
        canvas.DrawText(_text, bounds.ExactCenterX(), bounds.ExactCenterY() + _textSize * 0.35f, _paint);
    }

    public override void SetAlpha(int alpha) { _paint.Alpha = alpha; InvalidateSelf(); }
    public override void SetColorFilter(ColorFilter? colorFilter) { _paint.SetColorFilter(colorFilter); InvalidateSelf(); }
    public override int Opacity => _paint.Alpha;
}
