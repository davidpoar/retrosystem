using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class SearchResultAdapter : BaseAdapter<ZxInfoHit>
{
    private readonly Context _context;
    private readonly LayoutInflater _inflater;
    private readonly List<ZxInfoHit> _items;

    public event EventHandler<ZxInfoHit>? DownloadRequested;

    public SearchResultAdapter(Context context, List<ZxInfoHit> items)
    {
        _context = context;
        _items = items;
        _inflater = LayoutInflater.From(context);
    }

    public override int Count => _items.Count;

    public override ZxInfoHit this[int position] => _items[position];

    public override long GetItemId(int position) => position;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var view = convertView ?? _inflater.Inflate(Resource.Layout.item_search_result, parent, false);
        if (view == null) return null!;

        var hit = _items[position];
        var src = hit.Source;

        var title = view.FindViewById<TextView>(Resource.Id.textTitle);
        var meta = view.FindViewById<TextView>(Resource.Id.textMeta);
        var genre = view.FindViewById<TextView>(Resource.Id.textGenre);
        var status = view.FindViewById<TextView>(Resource.Id.textStatus);
        var imgScreen = view.FindViewById<ImageView>(Resource.Id.imageScreen);
        var imgPlaceholder = view.FindViewById<ImageView>(Resource.Id.imagePlaceholder);
        var imgProgress = view.FindViewById<ProgressBar>(Resource.Id.imageProgress);
        var btnDownload = view.FindViewById<ImageView>(Resource.Id.btnDownload);
        var downloadProgress = view.FindViewById<ProgressBar>(Resource.Id.downloadProgress);

        if (title != null) title.Text = src?.Title ?? "(sin título)";
        if (meta != null)
        {
            var parts = new List<string>();
            if (src?.Year.HasValue == true) parts.Add(src.Year.Value.ToString());
            if (src?.Publishers?.Count > 0) parts.Add(src.Publishers[0].Name);
            if (src?.MachineType != null) parts.Add(src.MachineType.Replace("ZX-Spectrum ", ""));
            meta.Text = string.Join(" · ", parts);
        }
        if (genre != null) genre.Text = src?.Genre ?? "";

        if (status != null)
        {
            if (src != null)
            {
                var avail = src.Availability?.ToLowerInvariant() ?? "";
                if (avail.Contains("available"))
                {
                    status.Visibility = ViewStates.Gone;
                }
                else
                {
                    status.Visibility = ViewStates.Visible;
                    status.Text = $"⚠ {src.Availability}";
                    status.SetTextColor(Color.Argb(0xFF, 0xE0, 0xA0, 0x40));
                }
            }
        }

        if (imgScreen != null) imgScreen.SetImageDrawable(null);
        if (imgPlaceholder != null) imgPlaceholder.Visibility = ViewStates.Visible;
        if (imgProgress != null) imgProgress.Visibility = ViewStates.Gone;

        if (imgScreen != null && src?.Screens?.Count > 0)
        {
            imgPlaceholder.Visibility = ViewStates.Gone;
            if (imgProgress != null) imgProgress.Visibility = ViewStates.Visible;
            view.Tag = src.Screens[0].Url;
            if (imgProgress != null) imgProgress.Tag = position;
        }

        if (btnDownload != null)
        {
            btnDownload.Tag = position.ToString();
            btnDownload.Click -= OnDownloadClick;
            btnDownload.Click += OnDownloadClick;
        }

        if (downloadProgress != null) downloadProgress.Visibility = ViewStates.Gone;
        if (btnDownload != null) btnDownload.Visibility = ViewStates.Visible;

        return view;
    }

    private void OnDownloadClick(object? sender, EventArgs e)
    {
        if (sender is View v && v.Tag != null && int.TryParse(v.Tag.ToString(), out int idx))
        {
            if (idx >= 0 && idx < _items.Count)
            {
                DownloadRequested?.Invoke(this, _items[idx]);
            }
        }
    }
}
