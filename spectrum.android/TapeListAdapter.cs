using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace retro.spectrum.android;

public sealed class TapeListAdapter : BaseAdapter<TapeInfo>
{
    private readonly Context _context;
    private readonly List<TapeInfo> _items;
    private readonly LayoutInflater _inflater;

    public event EventHandler<TapeInfo>? TapeClicked;

    public TapeListAdapter(Context context, List<TapeInfo> items)
    {
        _context = context;
        _items = items;
        _inflater = LayoutInflater.From(context);
    }

    public override int Count => _items.Count;

    public override TapeInfo this[int position] => _items[position];

    public override long GetItemId(int position) => position;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var view = convertView ?? _inflater.Inflate(Resource.Layout.item_tape, parent, false);
        if (view == null) return null!;

        var info = _items[position];

        var title = view.FindViewById<TextView>(Resource.Id.textTitle);
        var meta = view.FindViewById<TextView>(Resource.Id.textMeta);
        var badge = view.FindViewById<TextView>(Resource.Id.badgeFormat);
        var recentBadge = view.FindViewById<TextView>(Resource.Id.badgeRecent);

        if (title != null) title.Text = info.DisplayName;
        if (meta != null) meta.Text = info.Meta;
        if (badge != null)
        {
            badge.Text = info.FormatTag;
            badge.SetBackgroundResource(info.Format == TapeFormat.TAP
                ? Resource.Drawable.bg_badge_tap
                : Resource.Drawable.bg_badge_tzx);
        }
        if (recentBadge != null)
        {
            recentBadge.Visibility = info.IsRecent ? ViewStates.Visible : ViewStates.Gone;
        }

        view.Click -= OnRowClick;
        view.Tag = position;
        view.Click += OnRowClick;

        return view;
    }

    private void OnRowClick(object? sender, EventArgs e)
    {
        if (sender is View v && v.Tag != null && int.TryParse(v.Tag.ToString(), out int idx))
        {
            if (idx >= 0 && idx < _items.Count)
            {
                TapeClicked?.Invoke(this, _items[idx]);
            }
        }
    }
}
