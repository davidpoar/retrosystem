using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace retro.spectrum.android;

public sealed class DpadView : View
{
    public delegate void KeyEventHandler(int keyId, bool pressed);
    public event KeyEventHandler? KeyChanged;

    private static readonly Color PadColor = Color.Argb(0xEE, 0x1F, 0x24, 0x36);
    private static readonly Color PadHighlight = Color.Argb(0xEE, 0x33, 0x3B, 0x55);
    private static readonly Color Pressed = Color.Argb(0xFF, 0x60, 0xE0, 0x70);
    private static readonly Color IconColor = Color.Argb(0xFF, 0xC8, 0xD0, 0xE0);
    private static readonly Color LabelColor = Color.Argb(0xFF, 0x90, 0xC0, 0xFF);
    private static readonly Color HubColor = Color.Argb(0xFF, 0x14, 0x18, 0x28);

    private readonly Paint _paintPad = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintPressed = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintIcon = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintLabel = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintHub = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintHint = new Paint(PaintFlags.AntiAlias);

    private readonly HashSet<int> _pressedKeys = new();
    private readonly Context _ctx;
    private readonly int _upKey, _downKey, _leftKey, _rightKey;
    private float _cx, _cy, _arm, _thick, _corner;
    private float _labelTextSize;
    private float _hintTextSize;
    private string _hintLabel = "";

    public DpadView(Context context) : this(context, SpectrumKeyMap.Q, SpectrumKeyMap.A, SpectrumKeyMap.O, SpectrumKeyMap.P) { }
    public DpadView(Context context, int upKey, int downKey, int leftKey, int rightKey, string hint = "QAOP") : base(context)
    {
        _ctx = context;
        _upKey = upKey;
        _downKey = downKey;
        _leftKey = leftKey;
        _rightKey = rightKey;
        _hintLabel = hint;
        SetBackgroundColor(Color.Transparent);
    }

    private int Dp(float v) => (int)(v * Resources.DisplayMetrics.Density + 0.5f);

    private RectF? _upRect, _downRect, _leftRect, _rightRect;

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        int w = MeasureSpec.GetSize(widthMeasureSpec);
        int h = MeasureSpec.GetSize(heightMeasureSpec);
        int side = Math.Min(w, h);
        SetMeasuredDimension(w, h);

        _cx = w / 2f;
        _cy = h / 2f;
        _arm = side * 0.42f;
        _thick = _arm * 0.62f;
        _corner = _thick * 0.18f;
        _labelTextSize = _arm * 0.20f;
        _hintTextSize = side * 0.07f;

        _paintLabel.TextSize = _labelTextSize;
        _paintLabel.TextAlign = Paint.Align.Center;
        _paintLabel.SetTypeface(Android.Graphics.Typeface.Create("sans-serif", Android.Graphics.TypefaceStyle.Bold));
        _paintLabel.Color = LabelColor;

        _paintIcon.TextSize = _arm * 0.32f;
        _paintIcon.TextAlign = Paint.Align.Center;
        _paintIcon.Color = IconColor;

        _paintHub.Color = HubColor;
        _paintHub.SetShadowLayer(Dp(3), 0, Dp(2), Color.Argb(0x80, 0, 0, 0));

        _paintHint.TextSize = _hintTextSize;
        _paintHint.TextAlign = Paint.Align.Center;
        _paintHint.Color = Color.Argb(0xAA, 0x80, 0x80, 0x80);

        _upRect = new RectF(_cx - _thick / 2, _cy - _arm, _cx + _thick / 2, _cy);
        _downRect = new RectF(_cx - _thick / 2, _cy, _cx + _thick / 2, _cy + _arm);
        _leftRect = new RectF(_cx - _arm, _cy - _thick / 2, _cx, _cy + _thick / 2);
        _rightRect = new RectF(_cx, _cy - _thick / 2, _cx + _arm, _cy + _thick / 2);
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        _paintPad.Color = PadColor;
        _paintPad.SetShadowLayer(Dp(6), 0, Dp(2), Color.Argb(0x80, 0, 0, 0));

        DrawArm(canvas, _upRect!, _upKey);
        DrawArm(canvas, _downRect!, _downKey);
        DrawArm(canvas, _leftRect!, _leftKey);
        DrawArm(canvas, _rightRect!, _rightKey);

        canvas.DrawCircle(_cx, _cy, _thick * 0.42f, _paintHub);

        _paintLabel.Color = Color.Argb(0x66, 0x60, 0xE0, 0x70);
        canvas.DrawText(_hintLabel, _cx, h() - Dp(8), _paintHint);
    }

    private float h() => Height;

    private void DrawArm(Canvas canvas, RectF r, int keyId)
    {
        bool pressed = _pressedKeys.Contains(keyId);
        _paintPad.Color = pressed ? Pressed : PadColor;
        _paintPressed.Color = Pressed;

        float notch = _thick * 0.10f;
        Path path = BuildCrossPath(_cx, _cy, _arm, _thick, _corner, notch);
        canvas.DrawPath(path, _paintPad);
        if (pressed) canvas.DrawPath(path, _paintPressed);

        string label = KeyLabel(keyId);
        string icon = IconForKey(keyId);
        float labelOffset = _arm * 0.62f;

        float cx = r.CenterX();
        float cy = r.CenterY();
        if (keyId == _upKey) cy += _labelTextSize * 0.3f;
        if (keyId == _downKey) cy -= _labelTextSize * 0.3f;
        if (keyId == _leftKey) cx -= _arm * 0.55f;
        if (keyId == _rightKey) cx += _arm * 0.55f;

        _paintIcon.Color = pressed ? Color.Argb(0xFF, 0x1A, 0x1A, 0x1A) : IconColor;
        canvas.DrawText(icon, cx, cy - _labelTextSize * 0.05f, _paintIcon);

        _paintLabel.Color = pressed ? Color.Argb(0xFF, 0x1A, 0x1A, 0x1A) : LabelColor;
        canvas.DrawText(label, cx, cy + _labelTextSize * 0.95f, _paintLabel);
    }

    private static Path BuildCrossPath(float cx, float cy, float arm, float thick, float corner, float notch)
    {
        float halfT = thick / 2f;
        float inner = halfT - notch;
        Path p = new Path();
        p.MoveTo(cx - halfT + corner, cy - arm);
        p.LineTo(cx + halfT - corner, cy - arm);
        p.QuadTo(cx + halfT, cy - arm, cx + halfT, cy - arm + corner);
        p.LineTo(cx + halfT, cy - inner);
        p.LineTo(cx + arm - corner, cy - inner);
        p.QuadTo(cx + arm, cy - inner, cx + arm, cy - inner + corner);
        p.LineTo(cx + arm, cy + inner - corner);
        p.QuadTo(cx + arm, cy + inner, cx + arm - corner, cy + inner);
        p.LineTo(cx + halfT, cy + inner);
        p.LineTo(cx + halfT, cy + arm - corner);
        p.QuadTo(cx + halfT, cy + arm, cx + halfT - corner, cy + arm);
        p.LineTo(cx - halfT + corner, cy + arm);
        p.QuadTo(cx - halfT, cy + arm, cx - halfT, cy + arm - corner);
        p.LineTo(cx - halfT, cy + inner);
        p.LineTo(cx - arm + corner, cy + inner);
        p.QuadTo(cx - arm, cy + inner, cx - arm, cy + inner - corner);
        p.LineTo(cx - arm, cy - inner + corner);
        p.QuadTo(cx - arm, cy - inner, cx - arm + corner, cy - inner);
        p.LineTo(cx - halfT, cy - inner);
        p.LineTo(cx - halfT, cy - arm + corner);
        p.QuadTo(cx - halfT, cy - arm, cx - halfT + corner, cy - arm);
        p.Close();
        return p;
    }

    private static string KeyLabel(int keyId)
    {
        if (keyId == SpectrumKeyMap.Q) return "Q";
        if (keyId == SpectrumKeyMap.A) return "A";
        if (keyId == SpectrumKeyMap.O) return "O";
        if (keyId == SpectrumKeyMap.P) return "P";
        return "";
    }

    private static string IconForKey(int keyId)
    {
        if (keyId == SpectrumKeyMap.Q) return "▲";
        if (keyId == SpectrumKeyMap.A) return "▼";
        if (keyId == SpectrumKeyMap.O) return "◀";
        if (keyId == SpectrumKeyMap.P) return "▶";
        return "";
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
        if (_upRect == null) return false;
        var action = e.ActionMasked;
        if (action == MotionEventActions.Down)
        {
            int? hit = HitTest(e.GetX(), e.GetY());
            if (hit.HasValue)
            {
                Press(hit.Value);
                Haptic.Tap(_ctx);
                return true;
            }
        }
        else if (action == MotionEventActions.Move)
        {
            int? hit = HitTest(e.GetX(), e.GetY());
            var current = new HashSet<int>(_pressedKeys);
            foreach (var k in current)
            {
                if (!hit.HasValue || hit.Value != k) Release(k);
            }
            if (hit.HasValue && !_pressedKeys.Contains(hit.Value))
            {
                Press(hit.Value);
                Haptic.Tap(_ctx);
            }
            return true;
        }
        else if (action == MotionEventActions.Up || action == MotionEventActions.Cancel)
        {
            foreach (var k in new List<int>(_pressedKeys)) Release(k);
            return true;
        }
        return base.OnTouchEvent(e);
    }

    private int? HitTest(float x, float y)
    {
        if (_upRect!.Contains(x, y)) return _upKey;
        if (_downRect!.Contains(x, y)) return _downKey;
        if (_leftRect!.Contains(x, y)) return _leftKey;
        if (_rightRect!.Contains(x, y)) return _rightKey;
        return null;
    }

    private void Press(int keyId)
    {
        if (_pressedKeys.Add(keyId)) { Invalidate(); KeyChanged?.Invoke(keyId, true); }
    }

    private void Release(int keyId)
    {
        if (_pressedKeys.Remove(keyId)) { Invalidate(); KeyChanged?.Invoke(keyId, false); }
    }

    public void ReleaseAll()
    {
        foreach (var id in _pressedKeys) KeyChanged?.Invoke(id, false);
        _pressedKeys.Clear();
        Invalidate();
    }
}
