using System.Drawing.Drawing2D;

namespace StadiaBridge;

internal sealed class NeonButton : Button
{
    public bool Primary { get; set; }
    public bool ClassicStyle { get; set; }
    private bool hovered, held;

    public NeonButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
    }
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = held = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { held = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { held = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnPaintBackground(PaintEventArgs e)
        => e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? UiTheme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new RectangleF(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
        using var path = new GraphicsPath(); float d = Math.Min(16 * Font.Size / (40f / 3f), Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90); path.CloseFigure();
        Color top = !Enabled ? Color.FromArgb(21, 27, 41) : Primary ? Color.FromArgb(35, 192, 213) : Color.FromArgb(34, 43, 64);
        Color bottom = !Enabled ? Color.FromArgb(15, 20, 32) : Primary ? Color.FromArgb(75, 116, 238) : Color.FromArgb(18, 25, 42);
        if (ClassicStyle)
        {
            top = !Enabled ? Color.FromArgb(33,31,29) : Primary ? Color.FromArgb(62,130,225) : Color.FromArgb(54,51,47);
            bottom = !Enabled ? Color.FromArgb(25,24,22) : Primary ? Color.FromArgb(46,108,190) : Color.FromArgb(40,38,35);
        }
        if (UiTheme.IsLight)
        {
            top = !Enabled ? Color.FromArgb(236,240,246) : Primary ? Color.FromArgb(38,117,211) : Color.White;
            bottom = !Enabled ? Color.FromArgb(236,240,246) : Primary ? Color.FromArgb(30,96,184) : Color.FromArgb(232,239,249);
        }
        if (held) top = bottom;
        using (var fill = new LinearGradientBrush(bounds, top, bottom, 15)) g.FillPath(fill, path);
        if (!ClassicStyle && Enabled && (hovered || Focused))
        {
            using var glow = new Pen(Color.FromArgb(40, ControllerDashboard.Teal), 4); g.DrawPath(glow, path);
        }
        using (var edge = new Pen(UiTheme.IsLight ? hovered || Focused ? UiTheme.Accent : UiTheme.Edge : !Enabled ? Color.FromArgb(42, 52, 72) : hovered || Focused ? ControllerDashboard.Teal : Primary ? Color.FromArgb(100, 216, 244) : Color.FromArgb(69, 87, 117), 1))
            g.DrawPath(edge, path);
        TextRenderer.DrawText(g, Text, Font, Rectangle.Round(bounds), !Enabled ? ControllerDashboard.Muted : Primary ? UiTheme.IsLight ? Color.White : Color.FromArgb(6, 14, 27) : ControllerDashboard.Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
