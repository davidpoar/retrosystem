using Android.Content;
using Android.OS;
using Android.Views;

namespace retro.spectrum.android;

public static class Haptic
{
    private const int DefaultMs = 20;

    public static void Tap(Context? context, int milliseconds = DefaultMs)
    {
        if (context == null) return;
        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                VibrateModern(context, milliseconds);
            }
            else
            {
                VibrateLegacy(context, milliseconds);
            }
        }
        catch
        {
        }
    }

#pragma warning disable CA1416
#pragma warning disable CA1422
    private static void VibrateModern(Context context, int ms)
    {
        var vm = context.GetSystemService(Context.VibratorManagerService) as VibratorManager;
        var v = vm?.DefaultVibrator;
        if (v != null && v.HasVibrator)
        {
            var effect = VibrationEffect.CreateOneShot(ms, VibrationEffect.DefaultAmplitude);
            v.Vibrate(effect);
        }
    }

    private static void VibrateLegacy(Context context, int ms)
    {
        var v = context.GetSystemService(Context.VibratorService) as Vibrator;
        if (v != null && v.HasVibrator)
        {
            v.Vibrate(ms);
        }
    }
#pragma warning restore CA1422
#pragma warning restore CA1416
}
