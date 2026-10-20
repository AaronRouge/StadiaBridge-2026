using System.Drawing.Drawing2D;

namespace StadiaBridge;

internal sealed class VibrationPanel : UserControl
{
    private readonly Label message = new() { AutoEllipsis = true, Font = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel) };
    private readonly NeonButton stop = new() { Text = "Detener", Font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Pixel), AccessibleName = "Detener prueba de vibración" };
    internal IReadOnlyList<RumbleOptionButton> Options { get; }
    internal event Action<RumblePattern>? PatternRequested;
    internal event Action? StopRequested;
    private int pair;
    private bool classic;
    private RumblePattern? active;
    internal string StatusMessage { get => message.Text; set => message.Text = value; }

    internal VibrationPanel()
    {
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);
        SetStyle(ControlStyles.ResizeRedraw, true);
        AccessibleName = "Pruebas de vibración USB, dos motores";
        Options = new[]
        {
            new RumbleOptionButton(RumblePattern.Heavy, "FUERTE", "Motor grande", Color.FromArgb(255,132,47)),
            new RumbleOptionButton(RumblePattern.Light, "SUAVE", "Motor pequeño", Color.FromArgb(66,139,255)),
            new RumbleOptionButton(RumblePattern.Burst, "RÁFAGA", "Tres golpes cortos", Color.FromArgb(175,89,246)),
            new RumbleOptionButton(RumblePattern.Pulse, "PULSO", "Dos ondas suaves", Color.FromArgb(246,91,132))
        };
        foreach (var option in Options)
        {
            Controls.Add(option);
            option.Click += (_, _) => PatternRequested?.Invoke(option.Pattern);
        }
        Controls.Add(message); Controls.Add(stop);
        stop.Enabled = false;
        stop.Click += (_, _) => StopRequested?.Invoke();
        AccessibleDescription = "Los indicadores representan la salida solicitada; no miden la vibración física.";
    }

    internal void SetTheme(bool value)
    {
        classic = value;
        BackColor = UiTheme.IsLight ? UiTheme.Surface : value ? Color.FromArgb(29,27,25) : UiTheme.Surface;
        message.BackColor = BackColor;
        stop.ClassicStyle = value;
        message.ForeColor = UiTheme.Muted;
        foreach (var option in Options) { option.ClassicStyle = value; option.Invalidate(); }
        Invalidate();
    }

    internal void UpdateMotors(int value, RumblePattern? pattern)
    {
        if (value == pair && active == pattern) return;
        pair = value; active = pattern;
        stop.Enabled = pattern is not null;
        foreach (var option in Options) option.Selected = option.Pattern == pattern;
        Invalidate();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (Options is null) return;
        float s = Font.Size / 14f;
        int Px(float n) => (int)Math.Round(n * s);
        int left = Px(166), gap = Px(9), right = Px(16);
        int buttonWidth = Math.Max(Px(90), (Width - left - right - gap) / 2);
        for (int i = 0; i < Options.Count; i++)
            Options[i].SetBounds(left + i % 2 * (buttonWidth + gap), Px(52 + i / 2 * 52), buttonWidth, Px(44));
        stop.SetBounds(Width - Px(86), Px(13), Px(70), Px(25));
        message.SetBounds(Px(18), Height - Px(28), Math.Max(1, Width - Px(36)), Px(22));
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        float s = Font.Size / 14f;
        using var frame = InputFrame.Round(new(1,1,Width-3,Height-3),24*s);
        Color surface = UiTheme.IsLight ? UiTheme.Surface : classic ? Color.FromArgb(29,27,25) : UiTheme.Surface;
        using (var fill = new SolidBrush(surface)) g.FillPath(fill,frame);
        using (var edge = new Pen(UiTheme.IsLight ? UiTheme.Edge : Color.FromArgb(57,54,50), Math.Max(1,s))) g.DrawPath(edge,frame);
        var saved = g.Save(); g.ScaleTransform(s,s);
        using (var badge = InputFrame.Round(new(18,15,30,30),9))
        using (var fill = new SolidBrush(UiTheme.IsLight ? Color.FromArgb(255,239,217) : Color.FromArgb(52,34,19))) g.FillPath(fill,badge);
        using (var bolt = new Pen(Color.FromArgb(255,170,0),2) { LineJoin = LineJoin.Round })
            g.DrawLines(bolt,new PointF[]{new(35,19),new(26,31),new(33,31),new(30,40),new(41,27),new(34,27),new(35,19)});
        DrawText(g,"Prueba de vibración",56,13,15,UiTheme.Amber,true);
        DrawText(g,"Dos motores · USB",56,32,10,UiTheme.Muted);
        using (var fill = new SolidBrush(UiTheme.IsLight ? Color.FromArgb(241,244,248) : Color.FromArgb(39,36,34))) g.FillEllipse(fill,51,64,76,76);
        Color waveColor = active is null ? UiTheme.Muted : UiTheme.Amber;
        using (var wave = new Pen(waveColor,2.5f) { StartCap=LineCap.Round, EndCap=LineCap.Round })
        {
            g.DrawArc(wave,68,82,42,39,-50,100); g.DrawArc(wave,68,82,42,39,130,100);
            g.DrawArc(wave,77,89,24,25,-50,100); g.DrawArc(wave,77,89,24,25,130,100);
            g.DrawEllipse(wave,87,99,4,4);
        }
        Meter(g,30,73,pair&255,Color.FromArgb(255,132,47));
        Meter(g,141,73,(pair>>8)&255,Color.FromArgb(66,139,255));
        g.Restore(saved);
    }

    private static void Meter(Graphics g,float x,float y,int level,Color accent)
    {
        using var track = new Pen(UiTheme.IsLight ? Color.FromArgb(216,223,232) : Color.FromArgb(50,46,43),5) { StartCap=LineCap.Round,EndCap=LineCap.Round };
        g.DrawLine(track,x,y,x,y+58);
        if(level==0) return;
        using var fill = new Pen(accent,5) { StartCap=LineCap.Round,EndCap=LineCap.Round };
        g.DrawLine(fill,x,y+58,x,y+58-58*level/255f);
    }
    private static void DrawText(Graphics g,string text,float x,float y,float size,Color color,bool bold=false)
    {
        using var font = new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color); g.DrawString(text,font,brush,x,y);
    }
}

internal sealed class RumbleOptionButton : Button
{
    internal RumblePattern Pattern { get; }
    internal bool ClassicStyle { get; set; }
    private readonly Color accent;
    private bool selected, hovered;
    internal bool Selected { get => selected; set { if(selected!=value) {selected=value;Invalidate();} } }

    internal RumbleOptionButton(RumblePattern pattern,string title,string description,Color accent)
    {
        Pattern=pattern; Text=title; this.accent=accent;
        Font=new Font("Segoe UI",12,FontStyle.Bold,GraphicsUnit.Pixel);
        AccessibleName=$"{RumbleState.Name(pattern)}: {description}";
        AccessibleDescription="Prueba USB breve; respeta el porcentaje de intensidad global.";
        FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        Cursor=Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) {hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e) {hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnGotFocus(EventArgs e) {base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e) {base.OnLostFocus(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??UiTheme.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
        float s=Font.Size/12f;
        using var path=InputFrame.Round(new(1,1,Width-3,Height-3),12*s);
        Color fill=UiTheme.IsLight?Color.FromArgb(237,242,249):ClassicStyle?Color.FromArgb(41,38,36):Color.FromArgb(26,33,49);
        using(var brush=new SolidBrush(fill))g.FillPath(brush,path);
        if(selected||hovered||Focused) {using var outline=new Pen(accent,Math.Max(1,1.4f*s));g.DrawPath(outline,path);}
        TextRenderer.DrawText(g,Text,Font,new Rectangle(0,(int)(3*s),Width,(int)(21*s)),UiTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        float center=Width/2f,y=Height-10*s;
        using var track=new Pen(Color.FromArgb(70,accent),4*s){StartCap=LineCap.Round,EndCap=LineCap.Round};
        using var ink=new Pen(accent,4*s){StartCap=LineCap.Round,EndCap=LineCap.Round};
        g.DrawLine(track,center-21*s,y,center+21*s,y);
        if(Pattern==RumblePattern.Pulse)
        {
            for(int i=0;i<4;i++)g.DrawLine(ink,center+(-21+i*12)*s,y,center+(-16+i*12)*s,y);
        }
        else g.DrawLine(ink,center-21*s,y,center+(Pattern==RumblePattern.Light?9:14)*s,y);
    }
}
