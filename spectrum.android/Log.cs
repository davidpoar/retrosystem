using Android.Util;

namespace retro.spectrum.android;

public static class Dbg
{
    private const string Tag = "SpectrumDBG";
    public static void I(string message) => global::Android.Util.Log.Info(Tag, message);
    public static void W(string message) => global::Android.Util.Log.Warn(Tag, message);
    public static void E(string message) => global::Android.Util.Log.Error(Tag, message);
}
