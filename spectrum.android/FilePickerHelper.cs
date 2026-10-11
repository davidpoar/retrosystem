using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Provider;
using Android.Database;
using Android.Net;
using Android.OS;

namespace retro.spectrum.android;

public static class FilePickerHelper
{
    public const int RequestCodeLoadTape = 1001;
    public const int RequestCodeLoadRom = 1002;

    public static void LaunchTapePicker(Activity activity)
    {
        Launch(activity, RequestCodeLoadTape, "Cargar cinta (.tap / .tzx)", new[] { "*/*" });
    }

    public static void LaunchRomPicker(Activity activity)
    {
        Launch(activity, RequestCodeLoadRom, "Cargar ROM del Spectrum (16 KB)", new[] { "*/*" });
    }

    private static void Launch(Activity activity, int requestCode, string title, string[] mimeTypes)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("*/*");
        intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);

        try
        {
            activity.StartActivityForResult(Intent.CreateChooser(intent, title), requestCode);
        }
        catch (Exception ex)
        {
            Dbg.E($"FilePicker launch failed: {ex.Message}");
        }
    }

    public static (bool ok, string? fileName, string? localPath, string? error) HandleResult(Activity activity, int requestCode, Result resultCode, Intent? data)
    {
        if (resultCode != Result.Ok) return (false, null, null, null);
        if (data == null || data.Data == null) return (false, null, null, "No se recibió archivo");

        Android.Net.Uri uri = data.Data!;
        string? fileName = QueryDisplayName(activity, uri);

        string cacheDir = activity.ExternalCacheDir?.AbsolutePath ?? activity.CacheDir.AbsolutePath;
        if (string.IsNullOrEmpty(fileName)) fileName = $"tape_{DateTime.Now:yyyyMMdd_HHmmss}";

        string destPath = Path.Combine(cacheDir, fileName);
        try
        {
            using var input = activity.ContentResolver.OpenInputStream(uri);
            if (input == null) return (false, null, null, "No se pudo abrir el archivo");
            using var output = File.Create(destPath);
            input.CopyTo(output);
            return (true, fileName, destPath, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    private static string? QueryDisplayName(Activity activity, Android.Net.Uri uri)
    {
        try
        {
            string? name = null;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat && DocumentsContract.IsDocumentUri(activity, uri))
            {
                string docId = DocumentsContract.GetDocumentId(uri) ?? "";
                string[] parts = docId.Split(':');
                if (parts.Length == 2)
                {
                    name = parts[1];
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                var cursor = activity.ContentResolver.Query(uri, null, null, null, null);
                if (cursor != null)
                {
                    try
                    {
                        int idx = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                        if (idx >= 0 && cursor.MoveToFirst())
                        {
                            name = cursor.GetString(idx);
                        }
                    }
                    finally
                    {
                        cursor.Close();
                    }
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                name = uri.LastPathSegment;
            }

            if (name != null)
            {
                name = Android.Net.Uri.Decode(name);
                name = name.Replace('/', '_').Replace('\\', '_');
            }
            return name;
        }
        catch
        {
            return null;
        }
    }
}
