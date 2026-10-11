using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

[Activity(
    Label = "ZX Spectrum 48K",
    Theme = "@style/AppTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize,
    ScreenOrientation = ScreenOrientation.Landscape
)]
public class TapeLibraryActivity : Activity
{
    public const string ExtraTapePath = "extra_tape_path";
    public const string ResultTapePath = "result_tape_path";
    public const string ResultTapeName = "result_tape_name";

    private ListView? _listView;
    private TextView? _emptyView;
    private TapeListAdapter? _adapter;
    private RecentsManager? _recents;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
        }
        catch (Exception ex)
        {
            Dbg.W($"TapeLibraryActivity: RequestWindowFeature fallo: {ex.Message}");
        }

        SetContentView(Resource.Layout.activity_tape_library);

        try
        {
            var window = Window;
            if (window != null)
            {
                window.AddFlags(WindowManagerFlags.Fullscreen);
            }
        }
        catch (Exception ex)
        {
            Dbg.W($"TapeLibraryActivity: AddFlags fallo: {ex.Message}");
        }

        try
        {
            _recents = new RecentsManager(this);
            _listView = FindViewById<ListView>(Resource.Id.listTapes);
            _emptyView = FindViewById<TextView>(Resource.Id.emptyView);

            var btnBack = FindViewById<ImageButton>(Resource.Id.btnBack);
            if (btnBack != null) btnBack.Click += (s, e) => Finish();

            var btnBrowse = FindViewById<Button>(Resource.Id.btnBrowse);
            if (btnBrowse != null) btnBrowse.Click += (s, e) => LaunchFilePicker();

            LoadTapes();
        }
        catch (Exception ex)
        {
            Dbg.E($"TapeLibraryActivity: init fallo: {ex.Message}\n{ex.StackTrace}");
            Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
            Finish();
        }
    }

    private void LoadTapes()
    {
        try
        {
            var allTapes = TapeScanner.ScanAll(this);

            var recents = _recents?.GetAll() ?? new List<RecentEntry>();
            var recentPaths = new HashSet<string>(recents.Select(r => r.Path), StringComparer.OrdinalIgnoreCase);

            var items = new List<TapeInfo>();
            if (recents.Count > 0)
            {
                items.Add(new TapeInfo { DisplayName = "★  Recientes" });
                foreach (var r in recents)
                {
                    if (System.IO.File.Exists(r.Path))
                    {
                        var info = TapeInfoFactory.FromFile(r.Path, isRecent: true,
                            lastPlayed: DateTimeOffset.FromUnixTimeSeconds(r.LastPlayedUtc).LocalDateTime);
                        items.Add(info);
                    }
                }
            }

            if (allTapes.Count > 0)
            {
                items.Add(new TapeInfo { DisplayName = "📂  Todas las cintas" });
                foreach (var tape in allTapes)
                {
                    if (!recentPaths.Contains(tape.FilePath))
                    {
                        items.Add(tape);
                    }
                }
            }

            _adapter = new TapeListAdapter(this, items);
            _adapter.TapeClicked += OnTapeClicked;
            if (_listView != null) _listView.Adapter = _adapter;

            if (items.Count == 0 || (recents.Count == 0 && allTapes.Count == 0))
            {
                if (_emptyView != null)
                {
                    _emptyView.Visibility = ViewStates.Visible;
                    _emptyView.Text = "No se encontraron cintas.\n\n" +
                                      "Para empezar:\n" +
                                      "1. Copia un archivo .tap o .tzx a la carpeta\n" +
                                      "   Download de tu dispositivo, o\n" +
                                      "2. Pulsa '📂 Buscar...' para elegir cualquier archivo.";
                }
                if (_listView != null) _listView.Visibility = ViewStates.Gone;
            }
            else
            {
                if (_emptyView != null) _emptyView.Visibility = ViewStates.Gone;
                if (_listView != null) _listView.Visibility = ViewStates.Visible;
            }
        }
        catch (Exception ex)
        {
            Dbg.E($"TapeLibraryActivity: LoadTapes fallo: {ex.Message}\n{ex.StackTrace}");
            if (_emptyView != null)
            {
                _emptyView.Visibility = ViewStates.Visible;
                _emptyView.Text = $"Error al escanear cintas:\n{ex.Message}";
            }
            if (_listView != null) _listView.Visibility = ViewStates.Gone;
        }
    }

    private void LaunchFilePicker()
    {
        FilePickerHelper.LaunchTapePicker(this);
    }

    private void OnTapeClicked(object? sender, TapeInfo info)
    {
        if (string.IsNullOrEmpty(info.FilePath)) return;
        if (!System.IO.File.Exists(info.FilePath))
        {
            Toast.MakeText(this, $"No se encuentra: {info.DisplayName}", ToastLength.Short)?.Show();
            return;
        }

        _recents?.Add(info.FilePath, info.DisplayName);

        var result = new Intent();
        result.PutExtra(ResultTapePath, info.FilePath);
        result.PutExtra(ResultTapeName, info.DisplayName);
        SetResult(Result.Ok, result);
        Finish();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == FilePickerHelper.RequestCodeLoadTape)
        {
            var (ok, fileName, localPath, error) = FilePickerHelper.HandleResult(this, requestCode, resultCode, data);
            if (ok && localPath != null)
            {
                _recents?.Add(localPath, fileName ?? System.IO.Path.GetFileNameWithoutExtension(localPath));
                var result = new Intent();
                result.PutExtra(ResultTapePath, localPath);
                result.PutExtra(ResultTapeName, fileName ?? System.IO.Path.GetFileNameWithoutExtension(localPath));
                SetResult(Result.Ok, result);
                Finish();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                Toast.MakeText(this, $"Error: {error}", ToastLength.Long)?.Show();
            }
        }
    }

    public override void OnBackPressed()
    {
        SetResult(Result.Canceled);
        base.OnBackPressed();
    }
}
