using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class GameFrameLayout : FrameLayout
{
    public GameFrameLayout(Context context) : base(context) { }
    public GameFrameLayout(Context context, IAttributeSet? attrs) : base(context, attrs) { }
    public GameFrameLayout(Context context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }
}
