namespace StadiaBridge;

// Keep one immutable 96-DPI layout. Resizing always starts from that layout,
// rather than repeatedly scaling rounded sizes and gradually losing precision.
internal sealed class ResponsiveLayout : IDisposable
{
    private sealed record Metrics(Control Control, Rectangle Bounds, Padding Margin,
        Padding Padding, Size Minimum, Size Maximum, Font Font,
        (SizeType Type, float Value)[] Rows, (SizeType Type, float Value)[] Columns);

    private readonly Control root;
    private readonly List<Metrics> metrics = [];
    private readonly Dictionary<Control, Font> ownedFonts = [];
    private bool applying;
    internal float Scale { get; private set; } = 1;

    internal ResponsiveLayout(Control root)
    {
        this.root = root;
        Capture(root);
    }

    private void Capture(Control control)
    {
        var table = control as TableLayoutPanel;
        metrics.Add(new(control, control.Bounds, control.Margin, control.Padding,
            control.MinimumSize, control.MaximumSize, (Font)control.Font.Clone(),
            table?.RowStyles.Cast<RowStyle>().Select(s => (s.SizeType, s.Height)).ToArray() ?? [],
            table?.ColumnStyles.Cast<ColumnStyle>().Select(s => (s.SizeType, s.Width)).ToArray() ?? []));
        foreach (Control child in control.Controls) Capture(child);
    }

    internal void Apply(Size clientSize)
    {
        float next = MathF.Round(Math.Min(clientSize.Width / 1280f, clientSize.Height / 860f), 3);
        if (applying || next <= 0 || Math.Abs(next - Scale) < .001f) return;
        applying = true;
        Scale = next;
        int Px(int value) => (int)MathF.Round(value * Scale);
        Size ScaledSize(Size size) => new(Px(size.Width), Px(size.Height));
        Padding ScaledPadding(Padding p) => new(Px(p.Left), Px(p.Top), Px(p.Right), Px(p.Bottom));
        foreach (var m in metrics) m.Control.SuspendLayout();
        try
        {
            foreach (var m in metrics)
            {
                var control = m.Control;
                control.MinimumSize = Size.Empty;
                control.MaximumSize = Size.Empty;
                control.Margin = ScaledPadding(m.Margin);
                control.Padding = ScaledPadding(m.Padding);
                var font = new Font(m.Font.FontFamily, m.Font.Size * Scale, m.Font.Style, m.Font.Unit);
                control.Font = font;
                if (ownedFonts.Remove(control, out var previous)) previous.Dispose();
                ownedFonts[control] = font;
                if (control != root)
                    control.SetBounds(Px(m.Bounds.X), Px(m.Bounds.Y), Px(m.Bounds.Width), Px(m.Bounds.Height));
                control.MinimumSize = ScaledSize(m.Minimum);
                control.MaximumSize = ScaledSize(m.Maximum);
                if (control is not TableLayoutPanel table) continue;
                for (int i = 0; i < m.Rows.Length; i++)
                    if (m.Rows[i].Type == SizeType.Absolute) table.RowStyles[i].Height = m.Rows[i].Value * Scale;
                for (int i = 0; i < m.Columns.Length; i++)
                    if (m.Columns[i].Type == SizeType.Absolute) table.ColumnStyles[i].Width = m.Columns[i].Value * Scale;
            }
        }
        finally
        {
            // Parent layout must settle before child layout computes its final bounds.
            foreach (var m in metrics) m.Control.ResumeLayout(true);
            root.PerformLayout();
            root.Invalidate(true);
            applying = false;
        }
    }

    public void Dispose()
    {
        foreach (var font in ownedFonts.Values) font.Dispose();
        foreach (var m in metrics) m.Font.Dispose();
        ownedFonts.Clear();
    }
}
