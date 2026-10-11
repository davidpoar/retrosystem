using Android.Util;

namespace retro.spectrum.android;

public sealed class AndroidLogProvider : ILogger
{
    private const string Tag = "Spectrum";

    public void Info(string message) => Log.Info(Tag, message);
    public void Warning(string message) => Log.Warn(Tag, message);
    public void Error(string message) => Log.Error(Tag, message);
}
