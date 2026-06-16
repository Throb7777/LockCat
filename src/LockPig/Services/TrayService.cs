using LockPig.Localization;
using LockPig.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LockPig.Services;

public sealed class TrayService : IDisposable
{
    private readonly Icon _icon;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _lockItem;
    private readonly ToolStripMenuItem _unlockItem;
    private readonly ToolStripMenuItem _petVisibilityItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Action _showPetRequested;
    private readonly Action _hidePetRequested;
    private LocalizedStrings _strings;
    private LockState _state = LockState.Normal;
    private bool _petVisible;

    public TrayService(
        Action lockRequested,
        Action unlockRequested,
        Action settingsRequested,
        Action showPetRequested,
        Action hidePetRequested,
        Action exitRequested,
        string language,
        bool petVisible)
    {
        _strings = Strings.For(language);
        _petVisible = petVisible;
        _showPetRequested = showPetRequested;
        _hidePetRequested = hidePetRequested;

        _lockItem = new ToolStripMenuItem(_strings.TrayLock, null, (_, _) => lockRequested());
        _unlockItem = new ToolStripMenuItem(_strings.TrayUnlock, null, (_, _) => unlockRequested());
        _petVisibilityItem = new ToolStripMenuItem(string.Empty, null, (_, _) =>
        {
            if (_petVisible)
            {
                _hidePetRequested();
            }
            else
            {
                _showPetRequested();
            }
        });
        _settingsItem = new ToolStripMenuItem(_strings.TraySettings, null, (_, _) => settingsRequested());
        _exitItem = new ToolStripMenuItem(_strings.TrayExit, null, (_, _) => exitRequested());

        ContextMenuStrip menu = new()
        {
            BackColor = Color.FromArgb(255, 246, 222),
            ForeColor = Color.FromArgb(59, 33, 20),
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point),
            Padding = new Padding(4, 5, 4, 5),
            ShowCheckMargin = false,
            ShowImageMargin = false,
            Renderer = new PixelTrayMenuRenderer()
        };
        menu.Items.Add(_lockItem);
        menu.Items.Add(_unlockItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_petVisibilityItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _icon = LoadIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => settingsRequested();
        StyleItems(menu);
        ApplyLanguage(language);
        UpdatePetVisibility(petVisible);
    }

    public void ApplyLanguage(string language)
    {
        _strings = Strings.For(language);
        _lockItem.Text = _strings.TrayLock;
        _unlockItem.Text = _strings.TrayUnlock;
        _settingsItem.Text = _strings.TraySettings;
        _exitItem.Text = _strings.TrayExit;
        UpdateState(_state);
        UpdatePetVisibility(_petVisible);
    }

    public void UpdatePetVisibility(bool visible)
    {
        _petVisible = visible;
        _petVisibilityItem.Text = _petVisible ? _strings.TrayHidePet : _strings.TrayShowPet;
    }

    public void UpdateState(LockState state)
    {
        _state = state;
        _lockItem.Enabled = state == LockState.Normal;
        _unlockItem.Enabled = state == LockState.Locked;
        _notifyIcon.Text = state == LockState.Locked ? $"{Strings.AppName} - {_strings.TrayLockedSuffix}" : Strings.AppName;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private static Icon LoadIcon()
    {
        try
        {
            Uri uri = new("pack://application:,,,/LockCat;component/Assets/Icons/LockCat.ico", UriKind.Absolute);
            System.Windows.Resources.StreamResourceInfo? resource = System.Windows.Application.GetResourceStream(uri);
            if (resource is null)
            {
                return (Icon)SystemIcons.Application.Clone();
            }

            using Stream stream = resource.Stream;
            return new Icon(stream);
        }
        catch
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }

    private static void StyleItems(ContextMenuStrip menu)
    {
        foreach (ToolStripItem item in menu.Items)
        {
            item.AutoSize = true;
            item.ForeColor = Color.FromArgb(59, 33, 20);
            item.BackColor = Color.Transparent;
            item.Padding = item is ToolStripSeparator ? Padding.Empty : new Padding(14, 6, 18, 6);
            item.Margin = item is ToolStripSeparator ? new Padding(6, 4, 6, 4) : new Padding(2, 1, 2, 1);
        }
    }

    private sealed class PixelTrayMenuRenderer : ToolStripRenderer
    {
        private static readonly Color Border = Color.FromArgb(122, 76, 36);
        private static readonly Color InnerBorder = Color.FromArgb(255, 209, 90);
        private static readonly Color Background = Color.FromArgb(255, 246, 222);
        private static readonly Color Hover = Color.FromArgb(255, 209, 90);
        private static readonly Color Pressed = Color.FromArgb(242, 179, 38);
        private static readonly Color Text = Color.FromArgb(59, 33, 20);
        private static readonly Color DisabledText = Color.FromArgb(156, 119, 82);
        private static readonly Color Separator = Color.FromArgb(232, 201, 141);

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using SolidBrush background = new(Background);
            e.Graphics.FillRectangle(background, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle outer = new(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            Rectangle inner = new(2, 2, e.ToolStrip.Width - 5, e.ToolStrip.Height - 5);
            using Pen outerPen = new(Border, 2);
            using Pen innerPen = new(InnerBorder, 1);
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.DrawRectangle(outerPen, outer);
            if (inner.Width > 0 && inner.Height > 0)
            {
                e.Graphics.DrawRectangle(innerPen, inner);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not ToolStripMenuItem || !e.Item.Selected)
            {
                return;
            }

            Rectangle bounds = new(4, 1, e.Item.Width - 8, e.Item.Height - 2);
            using SolidBrush fill = new(e.Item.Pressed ? Pressed : Hover);
            using Pen border = new(Border, 1);
            e.Graphics.FillRectangle(fill, bounds);
            e.Graphics.DrawRectangle(border, bounds);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Text : DisabledText;
            e.TextRectangle = new Rectangle(
                e.TextRectangle.X,
                e.TextRectangle.Y,
                e.TextRectangle.Width,
                e.TextRectangle.Height);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using Pen shadow = new(Color.FromArgb(255, 247, 216, 148), 1);
            using Pen line = new(Separator, 1);
            e.Graphics.DrawLine(line, 8, y, e.Item.Width - 8, y);
            e.Graphics.DrawLine(shadow, 8, y + 1, e.Item.Width - 8, y + 1);
        }
    }
}
