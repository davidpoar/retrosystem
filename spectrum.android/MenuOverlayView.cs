using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class MenuOverlayView : FrameLayout
{
    public delegate void MenuActionHandler(MenuAction action);
    public event MenuActionHandler? ActionRequested;
    public event Action? DismissRequested;

    public enum MenuAction
    {
        LoadTape,
        SearchOnline,
        TypeLoad,
        ToggleFastLoad,
        ToggleKeyboard,
        Reset,
        Help
    }

    private static readonly Color BgOverlay = Color.Argb(0xCC, 0x05, 0x05, 0x15);
    private static readonly Color BgPanel  = Color.Argb(0xFF, 0x18, 0x18, 0x30);
    private static readonly Color BgItem   = Color.Argb(0xEE, 0x30, 0x30, 0x50);
    private static readonly Color BgItemPressed = Color.Argb(0xFF, 0x60, 0xE0, 0x70);
    private static readonly Color BgClose  = Color.Argb(0xEE, 0xC0, 0x30, 0x30);
    private static readonly Color TxtWhite = Color.White;

    private bool _fastLoadEnabled;

    public MenuOverlayView(Context context) : base(context) { Init(); }
    public MenuOverlayView(Context context, IAttributeSet? attrs) : base(context, attrs) { Init(); }
    public MenuOverlayView(Context context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { Init(); }

    public void SetFastLoadState(bool enabled)
    {
        _fastLoadEnabled = enabled;
        if (_btnFastLoad != null)
        {
            _btnFastLoad.Text = $"Fast Load: {(enabled ? "ON" : "OFF")}";
        }
    }

    public bool FastLoadEnabled => _fastLoadEnabled;

    private Button? _btnFastLoad;
    private TextView? _lblTitle;

    private void Init()
    {
        SetBackgroundColor(BgOverlay);
        Clickable = true;
        Focusable = true;

        var container = new LinearLayout(Context);
        container.Orientation = Orientation.Vertical;
        container.SetBackgroundColor(BgPanel);
        container.SetPadding(dp(20), dp(16), dp(20), dp(16));

        var containerParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent,
            GravityFlags.Center)
        {
            LeftMargin = dp(20),
            RightMargin = dp(20)
        };
        container.LayoutParameters = containerParams;

        _lblTitle = new TextView(Context)
        {
            Text = "ZX SPECTRUM 48K",
            TextSize = 18,
            Gravity = GravityFlags.Center
        };
        _lblTitle.SetTextColor(Color.Argb(0xFF, 0x60, 0xE0, 0x70));
        _lblTitle.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        _lblTitle.SetPadding(0, 0, 0, dp(16));
        container.AddView(_lblTitle);

        var statusLine = new TextView(Context)
        {
            Text = "(c) 1982 Sinclair Research Ltd",
            TextSize = 11,
            Gravity = GravityFlags.Center
        };
        statusLine.SetTextColor(Color.Argb(0xFF, 0x80, 0x80, 0x80));
        statusLine.SetPadding(0, 0, 0, dp(12));
        container.AddView(statusLine);

        container.AddView(MakeItem("📂  Cargar cinta local (.tap/.tzx)...", 0xFF, 0x40, 0xA0, () => ActionRequested?.Invoke(MenuAction.LoadTape)));
        container.AddView(MakeSpacer());
        container.AddView(MakeItem("🌐  Buscar y descargar online", 0xFF, 0x60, 0x90, () => ActionRequested?.Invoke(MenuAction.SearchOnline)));
        container.AddView(MakeSpacer());
        container.AddView(MakeItem("⌨️  Teclear LOAD\"\" + Enter", 0xFF, 0x40, 0xC0, () => ActionRequested?.Invoke(MenuAction.TypeLoad)));
        container.AddView(MakeSpacer());

        _btnFastLoad = MakeItem("Fast Load: OFF", 0xFF, 0xC0, 0x40, () => ActionRequested?.Invoke(MenuAction.ToggleFastLoad));
        _btnFastLoad.Text = "Fast Load: " + (_fastLoadEnabled ? "ON" : "OFF");
        container.AddView(_btnFastLoad);
        container.AddView(MakeSpacer());

        container.AddView(MakeItem("🔄  Reset Spectrum", 0xFF, 0xC0, 0x80, () => ActionRequested?.Invoke(MenuAction.Reset)));
        container.AddView(MakeSpacer());
        container.AddView(MakeItem("⌨️  Mostrar/ocultar teclado", 0xFF, 0x80, 0xC0, () => ActionRequested?.Invoke(MenuAction.ToggleKeyboard)));
        container.AddView(MakeSpacer());
        container.AddView(MakeItem("❓  Ayuda", 0xFF, 0x80, 0x80, () => ActionRequested?.Invoke(MenuAction.Help)));
        container.AddView(MakeSpacer());

        var btnClose = new Button(Context) { Text = "✕  Cerrar" };
        btnClose.SetBackgroundColor(BgClose);
        btnClose.SetTextColor(TxtWhite);
        btnClose.SetTextSize(Android.Util.ComplexUnitType.Sp, 16);
        btnClose.SetPadding(0, dp(14), 0, dp(14));
        btnClose.SetAllCaps(false);
        btnClose.Click += (s, e) => DismissRequested?.Invoke();
        container.AddView(btnClose);

        AddView(container);

        Visibility = ViewStates.Gone;
    }

    private Button MakeItem(string text, byte r, byte g, byte b, Action onClick)
    {
        var btn = new Button(Context) { Text = text };
        btn.SetBackgroundColor(BgItem);
        btn.SetTextColor(Color.Argb(0xFF, r, g, b));
        btn.SetTextSize(Android.Util.ComplexUnitType.Sp, 16);
        btn.SetAllCaps(false);
        btn.SetPadding(dp(12), dp(14), dp(12), dp(14));
        btn.Gravity = GravityFlags.CenterVertical | GravityFlags.Start;
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private View MakeSpacer()
    {
        var v = new View(Context);
        v.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, dp(6));
        return v;
    }

    private int dp(int v)
    {
        float density = Context.Resources.DisplayMetrics.Density;
        return (int)(v * density + 0.5f);
    }

    public void Show()
    {
        Visibility = ViewStates.Visible;
        BringToFront();
        RequestLayout();
    }

    public void Hide()
    {
        Visibility = ViewStates.Gone;
    }

    public bool IsVisible => Visibility == ViewStates.Visible;
}
