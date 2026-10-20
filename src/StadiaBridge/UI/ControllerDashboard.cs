using System.Drawing.Drawing2D;

namespace StadiaBridge;

internal sealed class ControllerDashboard : Control
{
    internal static Color Background => UiTheme.Background;
    internal static Color Surface => UiTheme.Surface;
    internal static Color Muted => UiTheme.Muted;
    internal static Color Ink => UiTheme.Text;
    private static readonly Color ShellInk = Color.FromArgb(237,242,247);
    internal static readonly Color Teal = Color.FromArgb(39, 233, 237);
    internal static readonly Color Orange = Color.FromArgb(255, 171, 86);
    internal static readonly Color Violet = Color.FromArgb(174, 110, 255);
    private bool classicStyle;
    public bool ClassicStyle
    {
        get => classicStyle;
        set { classicStyle = value; Invalidate(); }
    }
    private StadiaReport physical;
    private XboxReport mapped;
    private bool connected;
    private long reports;
    private int deadzone;

    public ControllerDashboard()
    {
        DoubleBuffered = true; BackColor = Background;
        SetStyle(ControlStyles.ResizeRedraw, true);
        AccessibleName = "Visor del mando Stadia: botones, sticks y gatillos en tiempo real";
    }

    public void UpdateState(StadiaReport raw, XboxReport output, bool online, long count, int zone)
    {
        physical = raw; mapped = output; connected = online; reports = count; deadzone = zone;
        AccessibleDescription = online
            ? $"Botones {raw.Xbox.Buttons:X4}. Stick izquierdo {raw.Xbox.LX}, {raw.Xbox.LY}. Stick derecho {raw.Xbox.RX}, {raw.Xbox.RY}. Gatillos {raw.Xbox.LeftTrigger}, {raw.Xbox.RightTrigger}."
            : "Mando desconectado";
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        // A fixed logical canvas preserves the geometry when the window or DPI changes.
        float scale = Math.Min(Width / 1200f, Height / 540f);
        if (scale <= 0) return;
        g.TranslateTransform((Width - 1200 * scale) / 2, (Height - 540 * scale) / 2);
        g.ScaleTransform(scale, scale);
        Card(g, new(0, 0, 600, 540));
        Card(g, new(618, 0, 340, 286));
        Card(g, new(976, 0, 224, 286));
        Card(g, new(618, 304, 582, 236));
        DrawText(g, "TU MANDO", 24, 21, 10, Muted, true);
        DrawText(g, ClassicStyle ? "VISOR DEL CONTROL" : "STADIA / NEON", 24, 42, 23, Ink, true);
        Pill(g, new(452, 27, 124, 28), connected ? "USB CONECTADO" : "SIN CONEXIÓN", connected ? Teal : Muted);
        DrawText(g, ClassicStyle ? "Google Stadia · distribución visual Xbox" : "Cada pulsación enciende su propia luz", 24, 79, 10, Muted);
        if (ClassicStyle) DrawClassicController(g); else DrawController(g);
        Dot(g, 30, 510, Teal, 4);
        DrawText(g, "Pulsado", 42, 502, 10, Ink);
        Dot(g, 138, 510, Color.FromArgb(75, 86, 100), 4);
        DrawText(g, "En reposo", 150, 502, 10, Muted);
        DrawText(g, "USB · 1 MANDO", 452, 503, 9, Muted, true);
        DrawSticks(g); DrawTriggers(g); DrawButtons(g);
    }

    private void DrawClassicController(Graphics g)
    {
        var save = g.Save(); g.TranslateTransform(300, 300);
        Color line = Color.FromArgb(81, 78, 73);
        Color shell = Color.FromArgb(40, 38, 37);
        using var body = new GraphicsPath();
        body.StartFigure();
        body.AddBezier(-151,-121,-204,-119,-220,-75,-234,-14);
        body.AddBezier(-234,-14,-251,55,-268,140,-228,170);
        body.AddBezier(-228,170,-191,196,-160,137,-132,111);
        body.AddBezier(-132,111,-115,96,-97,95,-72,95);
        body.AddLine(-72,95,72,95);
        body.AddBezier(72,95,97,95,115,96,132,111);
        body.AddBezier(132,111,160,137,191,196,228,170);
        body.AddBezier(228,170,268,140,251,55,234,-14);
        body.AddBezier(234,-14,220,-75,204,-119,151,-121);
        body.AddLine(151,-121,-151,-121); body.CloseFigure();
        using(var fill=new SolidBrush(shell)) g.FillPath(fill,body);
        using(var edge=new Pen(line,3)) g.DrawPath(edge,body);
        // Matte panel seams, with asymmetric Xbox-style stick placement.
        using(var seam=new Pen(line,2))
        {
            g.DrawLine(seam,-236,8,-118,108);
            g.DrawLine(seam,236,8,118,108);
        }
        ClassicTrigger(g,new(-159,-202,35,58),"LT",physical.Xbox.LeftTrigger);
        ClassicTrigger(g,new(124,-202,35,58),"RT",physical.Xbox.RightTrigger);
        FlatKey(g,new(-169,-137,56,23),"LB",Down(0x100),true);
        FlatKey(g,new(113,-137,56,23),"RB",Down(0x200),true);
        ClassicStick(g,-132,-34,physical.Xbox.LX,physical.Xbox.LY,Down(0x40));
        ClassicStick(g,65,60,physical.Xbox.RX,physical.Xbox.RY,Down(0x80));
        using(var well=new SolidBrush(Color.FromArgb(26,25,24))) g.FillEllipse(well,76,-91,112,112);
        using(var outline=new Pen(line,2)) g.DrawEllipse(outline,76,-91,112,112);
        ClassicButton(g,132,-65,17,"Y",Down(0x8000));
        ClassicButton(g,163,-34,17,"B",Down(0x2000));
        ClassicButton(g,101,-34,17,"X",Down(0x4000));
        ClassicButton(g,132,-3,17,"A",Down(0x1000));
        ClassicButton(g,0,-82,17,"⌂",Down(0x400));
        ClassicButton(g,-40,-34,11,"▭",Down(0x20));
        ClassicButton(g,0,-34,11,"≡",Down(0x10));
        ClassicButton(g,40,-34,11,"☰",physical.Extra1);
        ClassicButton(g,0,0,8,"2",physical.Extra2);
        float cx=-65,cy=60;
        using(var well=new SolidBrush(Color.FromArgb(25,24,23))) g.FillEllipse(well,cx-47,cy-47,94,94);
        using(var outline=new Pen(line,2)) g.DrawEllipse(outline,cx-47,cy-47,94,94);
        using var cross=new GraphicsPath();
        PointF[] corners=[new(cx-10,cy-33),new(cx+10,cy-33),new(cx+10,cy-10),new(cx+33,cy-10),new(cx+33,cy+10),new(cx+10,cy+10),new(cx+10,cy+33),new(cx-10,cy+33),new(cx-10,cy+10),new(cx-33,cy+10),new(cx-33,cy-10),new(cx-10,cy-10)];
        cross.AddPolygon(corners);
        using(var fill=new SolidBrush(Color.FromArgb(79,76,70))) g.FillPath(fill,cross);
        using(var pressed=new SolidBrush(Color.FromArgb(64,151,255)))
        {
            if(Down(1)) g.FillRectangle(pressed,cx-10,cy-33,20,23);
            if(Down(2)) g.FillRectangle(pressed,cx-10,cy+10,20,23);
            if(Down(4)) g.FillRectangle(pressed,cx-33,cy-10,23,20);
            if(Down(8)) g.FillRectangle(pressed,cx+10,cy-10,23,20);
        }
        g.Restore(save);
    }

    private static void ClassicStick(Graphics g,float cx,float cy,short x,short y,bool pressed)
    {
        using(var well=new SolidBrush(Color.FromArgb(24,23,22))) g.FillEllipse(well,cx-47,cy-47,94,94);
        using(var rim=new Pen(Color.FromArgb(78,75,69),2)) g.DrawEllipse(rim,cx-47,cy-47,94,94);
        float px=cx+Unit(x)*15,py=cy-Unit(y)*15;
        using(var cap=new SolidBrush(pressed?Color.FromArgb(64,151,255):Color.FromArgb(89,86,80))) g.FillEllipse(cap,px-34,py-34,68,68);
        using(var edge=new Pen(pressed?Color.FromArgb(122,190,255):Color.FromArgb(112,108,99),3)) g.DrawEllipse(edge,px-32,py-32,64,64);
        using(var inside=new Pen(Color.FromArgb(55,184,179,167),2)) g.DrawEllipse(inside,px-27,py-27,54,54);
    }
    private static void ClassicButton(Graphics g,float cx,float cy,float radius,string text,bool pressed)
    {
        using(var fill=new SolidBrush(pressed?Color.FromArgb(64,151,255):Color.FromArgb(75,72,66))) g.FillEllipse(fill,cx-radius,cy-radius,radius*2,radius*2);
        Center(g,text,new(cx-radius,cy-radius,radius*2,radius*2),radius>15?13:9,ShellInk,true);
    }
    private static void ClassicTrigger(Graphics g,RectangleF rect,string label,byte value)
    {
        using var path=Round(rect,16);
        using(var fill=new SolidBrush(Color.FromArgb(50,48,45))) g.FillPath(fill,path);
        if(value>0)
        {
            var saved=g.Save();g.SetClip(path);
            using var active=new SolidBrush(Color.FromArgb(216,214,208));
            float height=rect.Height*value/255f;
            g.FillRectangle(active,rect.X,rect.Bottom-height,rect.Width,height);g.Restore(saved);
        }
        using(var edge=new Pen(Color.FromArgb(103,100,92),2)) g.DrawPath(edge,path);
        Center(g,label,new(rect.X,rect.Y+18,rect.Width,20),8,value>160?Color.FromArgb(45,42,39):Color.FromArgb(190,196,207),true);
    }
    private static void FlatKey(Graphics g,RectangleF rect,string label,bool pressed,bool shell=false)
    {
        using var path=Round(rect,7);
        using(var fill=new SolidBrush(pressed?Color.FromArgb(64,151,255):UiTheme.IsLight&&!shell?Color.FromArgb(238,241,246):Color.FromArgb(49,46,43))) g.FillPath(fill,path);
        using(var border=new Pen(UiTheme.IsLight&&!shell?UiTheme.Edge:Color.FromArgb(76,72,66),1.5f)) g.DrawPath(border,path);
        Center(g,label,rect,9,pressed?Color.White:UiTheme.IsLight&&!shell?Ink:Color.FromArgb(191,186,175),true);
    }

    private void DrawController(Graphics g)
    {
        var save = g.Save(); g.TranslateTransform(300, 303);
        Halo(g, new(-265, -175, 320, 340), Color.FromArgb(28, Teal));
        Halo(g, new(-30, -175, 300, 340), Color.FromArgb(28, Violet));
        // A faint instrument grid gives the controller its own stage.
        using (var grid = new Pen(Color.FromArgb(14, 122, 151, 190)))
            for (int x = -252; x <= 252; x += 28) for (int y = -167; y < 166; y += 28)
                g.DrawLine(grid, x, y, x + 1, y);
        using var body = new GraphicsPath();
        body.StartFigure();
        body.AddBezier(-152, -132, -195, -130, -208, -95, -222, -40);
        body.AddBezier(-222, -40, -239, 21, -259, 116, -221, 150);
        body.AddBezier(-221, 150, -190, 177, -164, 148, -139, 119);
        body.AddBezier(-139, 119, -117, 93, -103, 96, -67, 98);
        body.AddBezier(-67, 98, -30, 92, 30, 92, 67, 98);
        body.AddBezier(67, 98, 103, 96, 117, 93, 139, 119);
        body.AddBezier(139, 119, 164, 148, 190, 177, 221, 150);
        body.AddBezier(221, 150, 259, 116, 239, 21, 222, -40);
        body.AddBezier(222, -40, 208, -95, 195, -130, 152, -132);
        body.AddBezier(152, -132, 90, -148, -90, -148, -152, -132); body.CloseFigure();
        using (var shadow = (GraphicsPath)body.Clone())
        {
            using var offset = new Matrix(); offset.Translate(0, 9); shadow.Transform(offset);
            using var shade = new SolidBrush(Color.FromArgb(90, 0, 0, 0)); g.FillPath(shade, shadow);
        }
        using (var fill = new LinearGradientBrush(new Rectangle(-245, -145, 490, 310), Color.FromArgb(42, 50, 68), Color.FromArgb(12, 16, 27), 90))
            g.FillPath(fill, body);
        GlowOutline(g, body, Teal, Violet);
        using (var inner = (GraphicsPath)body.Clone())
        {
            using var inset = new Matrix(); inset.Scale(.972f, .965f); inner.Transform(inset);
            using var edge = new Pen(Color.FromArgb(45, 181, 201, 228), 1); g.DrawPath(edge, inner);
        }
        Grip(g, body, false); Grip(g, body, true);
        // Small engraved seam across the upper shell.
        using (var seam = new Pen(Color.FromArgb(55, 80, 100, 128), 1)) g.DrawBezier(seam, -175, -104, -65, -117, 65, -117, 175, -104);
        TriggerTop(g, -158, physical.Xbox.LeftTrigger, "LT");
        TriggerTop(g, 108, physical.Xbox.RightTrigger, "RT");
        Key(g, new(-185, -131, 104, 27), "LB", Down(0x100), true);
        Key(g, new(81, -131, 104, 27), "RB", Down(0x200), true, Violet);
        DPad(g, -128, -37);
        using (var faceRing = new Pen(Color.FromArgb(52, 68, 88), 1.3f)) g.DrawEllipse(faceRing, 69, -100, 126, 126);
        Button(g, 132, -74, 19, "Y", Down(0x8000), Color.FromArgb(255, 213, 90));
        Button(g, 169, -37, 19, "B", Down(0x2000), Color.FromArgb(255, 88, 133));
        Button(g, 95, -37, 19, "X", Down(0x4000), Color.FromArgb(91, 162, 255));
        Button(g, 132, 0, 19, "A", Down(0x1000), Color.FromArgb(71, 255, 183));
        Button(g, 0, -87, 18, "S", Down(0x400), Violet);
        Button(g, -37, -33, 12, "↶", Down(0x20));
        Button(g, 37, -33, 12, "≡", Down(0x10), Violet);
        Button(g, -21, 5, 10, "1", physical.Extra1);
        Button(g, 21, 5, 10, "2", physical.Extra2, Violet);
        Stick(g, -65, 64, physical.Xbox.LX, physical.Xbox.LY, Down(0x40), "L3");
        Stick(g, 65, 64, physical.Xbox.RX, physical.Xbox.RY, Down(0x80), "R3");
        using (var light = new LinearGradientBrush(new Rectangle(-46, 121, 92, 2), Teal, Violet, 0f))
            g.FillRectangle(light, -46, 121, 92, 2);
        Center(g, "S T A D I A", new(-60, 132, 120, 20), 8, Muted, true);
        g.Restore(save);
    }

    private static void Grip(Graphics g, GraphicsPath body, bool right)
    {
        float sign = right ? 1 : -1;
        var save = g.Save(); g.SetClip(body);
        using var panel = new GraphicsPath();
        panel.AddBezier(211 * sign, -48, 185 * sign, 15, 139 * sign, 48, 121 * sign, 90);
        panel.AddBezier(121 * sign, 90, 157 * sign, 126, 197 * sign, 175, 244 * sign, 165);
        panel.AddLine(244 * sign, 165, 258 * sign, -12); panel.CloseFigure();
        using var fill = new SolidBrush(Color.FromArgb(15, 20, 31)); g.FillPath(fill, panel);
        using var edge = new Pen(Color.FromArgb(64, right ? Violet : Teal), 1); g.DrawPath(edge, panel);
        g.SetClip(panel, CombineMode.Intersect);
        using var texture = new SolidBrush(Color.FromArgb(42, 110, 128, 153));
        for (int x = 129; x < 250; x += 7) for (int y = 1; y < 155; y += 7)
            g.FillEllipse(texture, x * sign, y + (x % 2) * 3, 1.3f, 1.3f);
        g.Restore(save);
    }

    private void DPad(Graphics g, float cx, float cy)
    {
        using var well = new LinearGradientBrush(new RectangleF(cx - 53, cy - 53, 106, 106), Color.FromArgb(11, 15, 25), Color.FromArgb(37, 46, 63), 90);
        g.FillEllipse(well, cx - 53, cy - 53, 106, 106);
        using (var ring = new Pen(Color.FromArgb(58, 75, 96), 1)) g.DrawEllipse(ring, cx - 53, cy - 53, 106, 106);
        using var cross = new GraphicsPath();
        PointF[] points = [new(cx-12,cy-37),new(cx+12,cy-37),new(cx+12,cy-12),new(cx+37,cy-12),new(cx+37,cy+12),new(cx+12,cy+12),new(cx+12,cy+37),new(cx-12,cy+37),new(cx-12,cy+12),new(cx-37,cy+12),new(cx-37,cy-12),new(cx-12,cy-12)];
        cross.AddPolygon(points);
        using var fill = new LinearGradientBrush(new RectangleF(cx-37,cy-37,74,74), Color.FromArgb(67, 78, 98), Color.FromArgb(26, 33, 48), 90);
        g.FillPath(fill,cross);
        using var edge = new Pen(Color.FromArgb(98, 119, 145), 1); g.DrawPath(edge,cross);
        (RectangleF Rect,int Mask)[] directions = [(new(cx-10,cy-35,20,25),1),(new(cx-10,cy+10,20,25),2),(new(cx-35,cy-10,25,20),4),(new(cx+10,cy-10,25,20),8)];
        foreach (var direction in directions)
            if (Down(direction.Mask))
            {
                using var key=Round(direction.Rect,4); GlowOutline(g,key,Teal,Teal);
                using var glow=new SolidBrush(Color.FromArgb(80,Teal)); g.FillPath(glow,key);
            }
        Dot(g,cx,cy,Color.FromArgb(18,25,38),4);
    }

    private void DrawSticks(Graphics g)
    {
        DrawText(g, "STICKS ANALÓGICOS", 642, 21, 11, Teal, true);
        DrawText(g, "Lectura física · antes de la zona muerta", 642, 46, 9, Muted);
        AxisPlot(g, 710, 139, physical.Xbox.LX, physical.Xbox.LY, "IZQUIERDO", UiTheme.IsLight ? UiTheme.Accent : ClassicStyle ? Color.FromArgb(23, 193, 138) : Teal);
        AxisPlot(g, 865, 139, physical.Xbox.RX, physical.Xbox.RY, "DERECHO", UiTheme.IsLight ? UiTheme.Purple : ClassicStyle ? Color.FromArgb(23, 193, 138) : Violet);
        DrawText(g, $"Zona muerta de salida: {deadzone}%", 642, 253, 9, Muted);
    }

    private void AxisPlot(Graphics g, float cx, float cy, short x, short y, string label, Color accent)
    {
        using var surface = new SolidBrush(Background); g.FillEllipse(surface, cx - 54, cy - 54, 108, 108);
        using var ring = new Pen(UiTheme.IsLight ? UiTheme.Edge : Color.FromArgb(53, 70, 78), 1);
        g.DrawEllipse(ring, cx - 54, cy - 54, 108, 108);
        g.DrawLine(ring, cx - 54, cy, cx + 54, cy); g.DrawLine(ring, cx, cy - 54, cx, cy + 54);
        float zone = Math.Max(2, 54 * deadzone / 100f);
        using var dz = new SolidBrush(Color.FromArgb(45, Teal)); g.FillEllipse(dz, cx - zone, cy - zone, zone * 2, zone * 2);
        var px = cx + Unit(x) * 51; var py = cy - Unit(y) * 51;
        using var line = new Pen(accent, 2); g.DrawLine(line, cx, cy, px, py);
        Dot(g, px, py, connected ? accent : Muted, 5);
        Center(g, label, new(cx - 70, cy + 65, 140, 19), 9, Ink, true);
        Center(g, $"{Unit(x):+0.000;-0.000;0.000}   {Unit(y):+0.000;-0.000;0.000}", new(cx - 76, cy + 85, 152, 20), 9, Muted);
    }

    private void DrawTriggers(Graphics g)
    {
        DrawText(g, "GATILLOS", 1000, 21, 11, Violet, true);
        DrawText(g, "Presión independiente", 1000, 46, 9, Muted);
        TriggerBar(g, 1005, physical.Xbox.LeftTrigger, "LT");
        TriggerBar(g, 1101, physical.Xbox.RightTrigger, "RT");
    }

    private void TriggerBar(Graphics g, float x, byte value, string label)
    {
        var bounds = new RectangleF(x, 84, 64, 133);
        using var path = Round(bounds, 12);
        using var bg = new SolidBrush(Background); g.FillPath(bg, path);
        if (value > 0)
        {
            var saved = g.Save(); g.SetClip(path);
            using Brush fill = ClassicStyle ? new SolidBrush(Color.FromArgb(139, 82, 245)) : new LinearGradientBrush(bounds, Violet, Color.FromArgb(255, 92, 169), 90);
            float height = bounds.Height * value / 255f;
            g.FillRectangle(fill, x, bounds.Bottom - height, bounds.Width, height); g.Restore(saved);
        }
        using var pen = new Pen(Color.FromArgb(58, 69, 81)); g.DrawPath(pen, path);
        Center(g, $"{value / 255f:P0}", new(x - 5, 225, 74, 20), 12, Ink, true);
        Center(g, label, new(x - 5, 252, 74, 18), 10, Muted, true);
    }

    private void DrawButtons(Graphics g)
    {
        DrawText(g, "BOTONES EN TIEMPO REAL", 642, 325, 11, Ink, true);
        DrawText(g, connected ? $"{reports:N0} reportes recibidos" : "Conecta tu mando para comenzar", 972, 327, 9, Muted);
        (string Label, bool Pressed)[] keys = [
            ("A", Down(0x1000)), ("B", Down(0x2000)), ("X", Down(0x4000)), ("Y", Down(0x8000)),
            ("LB", Down(0x100)), ("RB", Down(0x200)), ("LT", physical.Xbox.LeftTrigger > 0), ("RT", physical.Xbox.RightTrigger > 0),
            ("L3", Down(0x40)), ("R3", Down(0x80)), ("↑", Down(1)), ("↓", Down(2)), ("←", Down(4)), ("→", Down(8)),
            ("Vista", Down(0x20)), ("Menú", Down(0x10)), ("Stadia", Down(0x400)), ("Extra 1", physical.Extra1), ("Extra 2", physical.Extra2)
        ];
        for (int i = 0; i < keys.Length; i++)
        {
            float x = 642 + i % 7 * 78; float y = 365 + i / 7 * 43;
            if (ClassicStyle) FlatKey(g, new(x, y, 68, 33), keys[i].Label, keys[i].Pressed);
            else Key(g, new(x, y, 68, 33), keys[i].Label, keys[i].Pressed);
        }
        DrawText(g, $"Mapeo Xbox: L {Unit(mapped.LX):0.00}, {Unit(mapped.LY):0.00}   /   R {Unit(mapped.RX):0.00}, {Unit(mapped.RY):0.00}", 642, 511, 9, Muted);
    }

    private bool Down(int mask) => (physical.Xbox.Buttons & mask) != 0;
    private static float Unit(short value) => value < 0 ? value / 32768f : value / 32767f;
    private static void TriggerTop(Graphics g, float x, byte value, string label)
    {
        Key(g, new(x, -174, 50, 34), label, value > 0, false, label == "RT" ? Violet : Teal);
    }
    private static void Stick(Graphics g, float cx, float cy, short x, short y, bool pressed, string label)
    {
        Color accent = cx < 0 ? Teal : Violet;
        using var rim = new GraphicsPath(); rim.AddEllipse(cx - 44, cy - 44, 88, 88);
        using (var well = new SolidBrush(Color.FromArgb(6, 10, 18))) g.FillPath(well, rim);
        GlowOutline(g, rim, Color.FromArgb(pressed ? 255 : 105, accent), Color.FromArgb(pressed ? 255 : 105, accent));
        using (var metal = new Pen(Color.FromArgb(70, 89, 116), 3)) g.DrawEllipse(metal,cx-39,cy-39,78,78);
        float px = cx + Unit(x) * 12, py = cy - Unit(y) * 12;
        using (var cap = new LinearGradientBrush(new RectangleF(px-32,py-32,64,64),Color.FromArgb(65,77,100),Color.FromArgb(22,29,45),90))
            g.FillEllipse(cap,px-32,py-32,64,64);
        using (var lip = new Pen(Color.FromArgb(113,133,160),1.3f)) g.DrawEllipse(lip,px-32,py-32,64,64);
        using (var rings = new Pen(Color.FromArgb(62,89,113,146),1))
            for (int radius=20;radius<=27;radius+=3) g.DrawEllipse(rings,px-radius,py-radius,radius*2,radius*2);
        using (var grooves = new Pen(Color.FromArgb(140,10,17,28),1.3f))
            for (int i=0;i<24;i++)
            {
                double angle=i*Math.PI/12;
                g.DrawLine(grooves,px+(float)Math.Cos(angle)*29,py+(float)Math.Sin(angle)*29,px+(float)Math.Cos(angle)*32,py+(float)Math.Sin(angle)*32);
            }
        if (pressed) { using var active = new Pen(accent,2); g.DrawEllipse(active,px-33,py-33,66,66); }
        Center(g,label,new(px-22,py-11,44,22),9,pressed?accent:ShellInk,true);
    }
    private static void Button(Graphics g, float x, float y, float radius, string text, bool active, Color? accent = null)
    {
        Color color=accent??Teal;
        using var path = new GraphicsPath(); path.AddEllipse(x-radius,y-radius,radius*2,radius*2);
        if(active) GlowOutline(g,path,color,color);
        using(var fill=new LinearGradientBrush(new RectangleF(x-radius,y-radius,radius*2,radius*2),active?Color.FromArgb(70,color):Color.FromArgb(43,53,73),Color.FromArgb(12,18,30),90)) g.FillPath(fill,path);
        using(var ring=new Pen(active?color:Color.FromArgb(93,110,138),active?1.8f:1)) g.DrawPath(ring,path);
        using(var highlight=new Pen(Color.FromArgb(active?130:45,Color.White),1)) g.DrawArc(highlight,x-radius+3,y-radius+3,radius*2-6,radius*2-6,210,110);
        Center(g,text,new(x-radius,y-radius,radius*2,radius*2),radius>15?13:9,active?Color.White:color,true);
    }
    private static void Key(Graphics g, RectangleF rect, string label, bool active, bool dark = false, Color? neon = null)
    {
        Color accent=neon??Teal;
        using var path=Round(rect,8);
        if(active) GlowOutline(g,path,accent,accent);
        using(var fill=new LinearGradientBrush(rect,active?Color.FromArgb(44,accent):UiTheme.IsLight&&!dark?Color.FromArgb(249,251,255):Color.FromArgb(31,40,59),dark?Color.FromArgb(13,18,29):UiTheme.IsLight?Color.FromArgb(238,242,249):Color.FromArgb(17,23,37),90)) g.FillPath(fill,path);
        using(var edge=new Pen(active?accent:UiTheme.IsLight&&!dark?UiTheme.Edge:Color.FromArgb(54,69,94),active?1.3f:1)) g.DrawPath(edge,path);
        Center(g,label,rect,10,active?UiTheme.IsLight&&!dark?UiTheme.Accent:accent:dark?ShellInk:Ink,true);
    }
    private void Card(Graphics g, RectangleF rect)
    {
        using var path=Round(new(rect.X+1,rect.Y+1,rect.Width-2,rect.Height-2),20);
        using(var bg=new LinearGradientBrush(rect,UiTheme.IsLight?Color.White:ClassicStyle?Color.FromArgb(31,29,27):Surface,UiTheme.IsLight?Color.FromArgb(249,251,255):ClassicStyle?Color.FromArgb(27,25,23):Color.FromArgb(12,16,27),100)) g.FillPath(bg,path);
        using(var edge=new Pen(UiTheme.IsLight?UiTheme.Edge:ClassicStyle?Color.FromArgb(53,50,46):Color.FromArgb(45,58,81))) g.DrawPath(edge,path);
        if(ClassicStyle) return;
        using var accent=new LinearGradientBrush(new RectangleF(rect.X+22,rect.Y+1,rect.Width-44,2),Color.FromArgb(100,Teal),Color.FromArgb(90,Violet),0f);
        g.FillRectangle(accent,rect.X+24,rect.Y+1,rect.Width-48,1);
    }
    private static void Halo(Graphics g, RectangleF bounds, Color color)
    {
        using var p=new GraphicsPath();p.AddEllipse(bounds);
        using var glow=new PathGradientBrush(p){CenterColor=color,SurroundColors=[Color.Transparent]};g.FillPath(glow,p);
    }
    private static void GlowOutline(Graphics g, GraphicsPath path, Color from, Color to)
    {
        foreach(var layer in new (float Width,int Alpha)[]{(14,6),(9,10),(5,18),(2.8f,42),(1.2f,190)})
        {
            using var brush=new LinearGradientBrush(path.GetBounds(),Color.FromArgb(layer.Alpha*from.A/255,from),Color.FromArgb(layer.Alpha*to.A/255,to),0f);
            using var pen=new Pen(brush,layer.Width){LineJoin=LineJoin.Round};g.DrawPath(pen,path);
        }
    }
    private static void Pill(Graphics g, RectangleF rect, string label, Color color)
    {
        using var path = Round(rect, 14); using var bg = new SolidBrush(Color.FromArgb(24, color)); g.FillPath(bg, path);
        Center(g, label, rect, 8, TextColor(color), true);
    }
    private static void Dot(Graphics g, float x, float y, Color color, float r)
    { using var brush = new SolidBrush(color); g.FillEllipse(brush, x - r, y - r, r * 2, r * 2); }
    private static void DrawText(Graphics g, string text, float x, float y, float size, Color color, bool bold = false)
    { using var font = new Font("Segoe UI", size * 4f / 3f, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel); using var brush = new SolidBrush(TextColor(color)); g.DrawString(text, font, brush, x, y); }
    private static Color TextColor(Color color) => UiTheme.IsLight ? color == Teal ? UiTheme.Accent : color == Violet ? UiTheme.Purple : color == Orange ? UiTheme.Amber : color : color;
    private static void Center(Graphics g, string text, RectangleF bounds, float size, Color color, bool bold = false)
    {
        using var font = new Font("Segoe UI", size * 4f / 3f, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color); using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, bounds, format);
    }
    private static GraphicsPath Round(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
}
