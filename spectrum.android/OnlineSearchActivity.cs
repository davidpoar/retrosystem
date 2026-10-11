using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.IO;

namespace retro.spectrum.android;

[Activity(
    Label = "🌐 ZXInfo Online",
    Theme = "@style/AppTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize,
    ScreenOrientation = ScreenOrientation.Landscape
)]
public class OnlineSearchActivity : Activity
{
    public const string ResultTapePath = "result_tape_path";
    public const string ResultTapeName = "result_tape_name";

    private EditText? _editSearch;
    private ImageButton? _btnClear;
    private ListView? _listResults;
    private TextView? _emptyView;
    private TextView? _textResultCount;
    private ProgressBar? _progressBar;
    private SearchResultAdapter? _adapter;

    private readonly ZxInfoClient _client = new ZxInfoClient();
    private readonly List<ZxInfoHit> _hits = new List<ZxInfoHit>();
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _downloadCts;

    private static readonly string CacheDirPath = Android.App.Application.Context.CacheDir!.AbsolutePath!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try { RequestWindowFeature(WindowFeatures.NoTitle); } catch { }
        SetContentView(Resource.Layout.activity_online_search);

        try
        {
            var window = Window;
            if (window != null) window.AddFlags(WindowManagerFlags.Fullscreen);
        }
        catch { }

        _editSearch = FindViewById<EditText>(Resource.Id.editSearch);
        _btnClear = FindViewById<ImageButton>(Resource.Id.btnClear);
        _listResults = FindViewById<ListView>(Resource.Id.listResults);
        _emptyView = FindViewById<TextView>(Resource.Id.emptyView);
        _textResultCount = FindViewById<TextView>(Resource.Id.textResultCount);
        _progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);

        var btnBack = FindViewById<ImageButton>(Resource.Id.btnBack);
        if (btnBack != null) btnBack.Click += (s, e) => Finish();

        _adapter = new SearchResultAdapter(this, _hits);
        _adapter.DownloadRequested += OnDownloadRequested;
        if (_listResults != null) _listResults.Adapter = _adapter;

        if (_editSearch != null)
        {
            _editSearch.TextChanged += (s, e) =>
            {
                var text = e.Text?.ToString() ?? "";
                var hasText = !string.IsNullOrEmpty(text);
                if (_btnClear != null) _btnClear.Visibility = hasText ? ViewStates.Visible : ViewStates.Gone;
            };
            _editSearch.EditorAction += (s, e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Search)
                {
                    PerformSearch();
                    e.Handled = true;
                }
            };
        }

        if (_btnClear != null)
        {
            _btnClear.Click += (s, e) =>
            {
                if (_editSearch != null) _editSearch.Text = "";
                _hits.Clear();
                _adapter?.NotifyDataSetChanged();
                ShowEmpty("Busca un juego por título, autor o publisher.\n\nPowered by ZXInfo (zxinfo.dk)");
            };
        }

        ShowEmpty("Busca un juego por título, autor o publisher.\n\nPowered by ZXInfo (zxinfo.dk)");
    }

    private void PerformSearch()
    {
        var query = _editSearch?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(query)) return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        HideKeyboard();
        ShowProgress(true);
        ShowEmpty("", false);

        Task.Run(async () =>
        {
            try
            {
                var response = await _client.SearchAsync(query, 0, 30, ct);
                RunOnUiThread(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    _hits.Clear();
                    if (response.Hits?.Hits != null) _hits.AddRange(response.Hits.Hits);
                    _adapter?.NotifyDataSetChanged();
                    ShowProgress(false);
                    int total = response.Hits?.Total?.Value ?? _hits.Count;
                    if (_textResultCount != null) _textResultCount.Text = $"{_hits.Count} de {total}";

                    if (_hits.Count == 0)
                    {
                        ShowEmpty($"No se encontraron resultados para '{query}'.\n\nIntenta con otro título o palabra clave.");
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    ShowProgress(false);
                    ShowEmpty($"Error al buscar:\n{ex.Message}\n\nVerifica tu conexión a internet.");
                });
            }
        }, ct);
    }

    private async void OnDownloadRequested(object? sender, ZxInfoHit hit)
    {
        if (hit.Source == null) return;

        var file = _client.FindBestDownload(hit.Source);
        if (file == null)
        {
            Toast.MakeText(this, $"No hay archivo descargable para '{hit.Source.Title}'", ToastLength.Long)?.Show();
            return;
        }

        var url = _client.MakeAbsoluteUrl(file.Path);
        var safeName = SanitizeFileName(hit.Source.Title);
        bool isZip = file.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        string ext = isZip
            ? GetInnerExt(file.Path)
            : System.IO.Path.GetExtension(file.Path);
        string localFileName = isZip
            ? $"{safeName}_temp.zip"
            : $"{safeName}{ext}";

        var localPath = System.IO.Path.Combine(CacheDirPath, localFileName);

        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        var ct = _downloadCts.Token;

        ShowDownloadProgress(hit, true);

        try
        {
            var bytes = await _client.DownloadFileAsync(url, null, ct);
            Android.Util.Log.Info("OnlineSearch", $"Descargado: {bytes.Length} bytes de {url}");

            if (bytes.Length < 100)
            {
                string preview = bytes.Length > 0 ? System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 200)) : "(vacío)";
                throw new Exception($"Descarga muy pequeña ({bytes.Length} B). Contenido: {preview}");
            }

            if (bytes[0] != 0x50 || bytes[1] != 0x4B)
            {
                string preview = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 300));
                Android.Util.Log.Error("OnlineSearch", $"No es un zip. Inicio: {preview}");
                throw new Exception($"No es un ZIP (inicio: {preview.Substring(0, Math.Min(100, preview.Length))}…)");
            }

            await System.IO.File.WriteAllBytesAsync(localPath, bytes, ct);

            string finalPath = localPath;
            if (isZip)
            {
                try
                {
                    using var archive = ZipFile.OpenRead(localPath);
                    var entry = archive.Entries.FirstOrDefault(e =>
                        e.FullName.EndsWith(".tap", StringComparison.OrdinalIgnoreCase)
                        || e.FullName.EndsWith(".tzx", StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                    {
                        finalPath = System.IO.Path.Combine(CacheDirPath, $"{safeName}{System.IO.Path.GetExtension(entry.FullName)}");
                        entry.ExtractToFile(finalPath, overwrite: true);
                        System.IO.File.Delete(localPath);
                        Android.Util.Log.Info("OnlineSearch", $"Extraído: {entry.FullName} → {finalPath}");
                    }
                    else
                    {
                        throw new Exception("El zip no contiene archivos .tap ni .tzx");
                    }
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Warn("OnlineSearch", $"Error al descomprimir: {ex.GetType().Name}: {ex.Message}");
                    throw new Exception($"Zip inválido ({bytes.Length} B): {ex.Message}");
                }
            }

            var recents = new RecentsManager(this);
            recents.Add(finalPath, hit.Source.Title);

            var result = new Intent();
            result.PutExtra(ResultTapePath, finalPath);
            result.PutExtra(ResultTapeName, hit.Source.Title);
            SetResult(Result.Ok, result);
            Finish();
        }
        catch (Exception ex)
        {
            ShowDownloadProgress(hit, false);
            string msg = ex.Message;
            if (ex.InnerException != null) msg += $" [{ex.InnerException.Message}]";
            Toast.MakeText(this, $"✗ {msg}", ToastLength.Long)?.Show();
            Android.Util.Log.Error("OnlineSearch", $"Download failed: {ex}");
        }
    }

    private void ShowDownloadProgress(ZxInfoHit hit, bool downloading)
    {
        RunOnUiThread(() =>
        {
            for (int i = 0; i < (_listResults?.Count ?? 0); i++)
            {
                if (_listResults == null) break;
                if (_listResults.Adapter == null) break;
                if (!ReferenceEquals(_listResults.Adapter.GetItem(i), hit)) continue;
                var v = _listResults.GetChildAt(i - _listResults.FirstVisiblePosition);
                if (v == null) continue;
                var btn = v.FindViewById<ImageView>(Resource.Id.btnDownload);
                var prog = v.FindViewById<ProgressBar>(Resource.Id.downloadProgress);
                if (downloading)
                {
                    if (btn != null) btn.Visibility = ViewStates.Gone;
                    if (prog != null) prog.Visibility = ViewStates.Visible;
                }
                else
                {
                    if (btn != null) btn.Visibility = ViewStates.Visible;
                    if (prog != null) prog.Visibility = ViewStates.Gone;
                }
                break;
            }
        });
    }

    private void ShowProgress(bool show)
    {
        if (_progressBar != null) _progressBar.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        if (_listResults != null) _listResults.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
    }

    private void ShowEmpty(string message, bool showMessage = true)
    {
        if (_emptyView != null)
        {
            _emptyView.Visibility = showMessage ? ViewStates.Visible : ViewStates.Gone;
            _emptyView.Text = message;
        }
    }

    private void HideKeyboard()
    {
        try
        {
            var imm = GetSystemService(Context.InputMethodService) as InputMethodManager;
            imm?.HideSoftInputFromWindow(_editSearch?.WindowToken, 0);
        }
        catch { }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "game";
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrEmpty(clean) ? "game" : clean;
    }

    private static string GetInnerExt(string zipPath)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(zipPath);
        var dot = name.LastIndexOf('.');
        if (dot > 0)
        {
            var ext = name.Substring(dot).ToLowerInvariant();
            if (ext == ".tap" || ext == ".tzx") return ext;
        }
        return ".tap";
    }

    public override void OnBackPressed()
    {
        SetResult(Result.Canceled);
        base.OnBackPressed();
    }

    protected override void OnDestroy()
    {
        _searchCts?.Cancel();
        _downloadCts?.Cancel();
        base.OnDestroy();
    }
}
