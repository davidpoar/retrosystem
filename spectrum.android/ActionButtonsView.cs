using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;

namespace retro.spectrum.android;

public sealed class ActionButtonsView : View
{
    public delegate void KeyEventHandler(int keyId, bool pressed);
    public event KeyEventHandler? KeyChanged;
    public event Action? KeyboardToggleRequested;
    public event Action? TapeToggleRequested;

    private static readonly Color FaceNormal = Color.Argb(0xF0, 0x40, 0xC0, 0x40);
    private static readonly Color FacePressed = Color.Argb(0xFF, 0x80, 0xFF, 0x80);
    private static readonly Color FaceNormalB = Color.Argb(0xF0, 0xC0, 0x40, 0x40);
    private static readonly Color FacePressedB = Color.Argb(0xFF, 0xFF, 0xA0, 0xA0);
    private static readonly Color RimColor = Color.Argb(0xFF, 0x10, 0x10, 0x10);
    private static readonly Color TextNormal = Color.Argb(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color TextPressed = Color.Argb(0xFF, 0x10, 0x10, 0x10);
    private static readonly Color LabelColor = Color.Argb(0xFF, 0xC0, 0xC0, 0xC0);
    private static readonly Color HintColor = Color.Argb(0xAA, 0x80, 0x80, 0x80);

    private readonly Paint _paintFace = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintRim = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintText = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintLabel = new Paint(PaintFlags.AntiAlias);
    private readonly Paint _paintHint = new Paint(PaintFlags.AntiAlias);

    private readonly HashSet<int> _pressedKeys = new();
    private readonly Context _ctx;
    private readonly int _aKey, _bKey;

    private float _cx, _cy;
    private float _btnRadius;
    private float _rimWidth;
    private float _labelSize;
    private float _hintSize;
    private float _aCx, _aCy, _bCx, _bCy;
    private string _aLabel, _bLabel;

    public ActionButtonsView(Context context) : this(context, SpectrumKeyMap.Space, "SPACE", SpectrumKeyMap.M, "M") { }

    public ActionButtonsView(Context context, int aKey, string aLabel, int bKey, string bLabel) : base(context)
    {
        _ctx = context;
        _aKey = aKey;
        _bLabel = bLabel;
        _bKey = bKey;
        _aLabel = aLabel;
        SetBackgroundColor(Color.Transparent);
        _paintText.TextAlign = Paint.Align.Center;
        _paintText.SetTypeface(Android.Graphics.Typeface.Create("sans-serif", Android.Graphics.TypefaceStyle.Bold));
        _paintLabel.TextAlign = Paint.Align.Center;
        _paintHint.TextAlign = Paint.Align.Center;
    }

    private int Dp(float v) => (int)(v * Resources.DisplayMetrics.Density + 0.5f);

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        int w = MeasureSpec.GetSize(widthMeasureSpec);
        int h = MeasureSpec.GetSize(heightMeasureSpec);
        SetMeasuredDimension(w, h);

        _cx = w / 2f;
        _cy = h / 2f;
        _btnRadius = Math.Min(w, h) * 0.22f;
        _rimWidth = Dp(3);
        _labelSize = _btnRadius * 0.20f;
        _hintSize = Math.Min(w, h) * 0.07f;

        _paintText.TextSize = _btnRadius * 0.45f;
        _paintLabel.TextSize = _labelSize;
        _paintHint.TextSize = _hintSize;

        _aCx = _cx + _btnRadius * 0.45f;
        _aCy = _cy - _btnRadius * 0.55f;
        _bCx = _cx - _btnRadius * 0.30f;
        _bCy = _cy + _btnRadius * 0.55f;
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        DrawButton(canvas, _aCx, _aCy, _aKey, FaceNormal, FacePressed, "A", _aLabel, true);
        DrawButton(canvas, _bCx, _bCy, _bKey, FaceNormalB, FacePressedB, "B", _bLabel, false);

        _paintHint.Color = HintColor;
        canvas.DrawText("FIRE", _cx, h() - Dp(8), _paintHint);
    }

    private float h() => Height;

    private void DrawButton(Canvas canvas, float cx, float cy, int keyId,
        Color faceNormal, Color facePressed, string letter, string label, bool isTop)
    {
        bool pressed = _pressedKeys.Contains(keyId);

        _paintRim.Color = RimColor;
        _paintRim.SetShadowLayer(Dp(8), 0, Dp(3), Color.Argb(0xAA, 0, 0, 0));
        canvas.DrawCircle(cx, cy, _btnRadius + _rimWidth, _paintRim);

        _paintFace.Color = pressed ? facePressed : faceNormal;
        _paintFace.SetShadowLayer(0, 0, 0, 0);
        canvas.DrawCircle(cx, cy, _btnRadius, _paintFace);

        _paintRim.Color = Color.Argb(0x60, 0xFF, 0xFF, 0xFF);
        _paintRim.SetShadowLayer(0, 0, 0, 0);
        canvas.DrawCircle(cx, cy - _btnRadius * 0.12f, _btnRadius * 0.85f, _paintRim);

        _paintText.Color = pressed ? TextPressed : TextNormal;
        canvas.DrawText(letter, cx, cy + _btnRadius * 0.18f, _paintText);

        _paintLabel.Color = LabelColor;
        canvas.DrawText(label, cx, cy + _btnRadius * 0.65f, _paintLabel);
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
        var action = e.ActionMasked;
        if (action == MotionEventActions.Down || action == MotionEventActions.Move)
        {
            int? hit = HitTest(e.GetX(), e.GetY());
            if (action == MotionEventActions.Move)
            {
                foreach (var k in new List<int>(_pressedKeys))
                {
                    if (!hit.HasValue || hit.Value != k) Release(k);
                }
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
        if (InCircle(x, y, _aCx, _aCy, _btnRadius)) return _aKey;
        if (InCircle(x, y, _bCx, _bCy, _btnRadius)) return _bKey;
        return null;
    }

    private static bool InCircle(float x, float y, float cx, float cy, float r)
    {
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
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
