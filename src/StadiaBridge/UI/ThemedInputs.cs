using System.Drawing.Drawing2D;
using System.Globalization;

namespace StadiaBridge;

internal static class InputFrame
{
    public static GraphicsPath Round(RectangleF r, float radius = 9)
    {
        var p = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p;
    }
    public static Color Surface(bool classic) => UiTheme.IsLight ? Color.FromArgb(249,251,255) : classic ? Color.FromArgb(36,34,31) : Color.FromArgb(21,29,46);
    public static Color Edge(bool classic) => UiTheme.IsLight ? UiTheme.Edge : classic ? Color.FromArgb(85,79,70) : Color.FromArgb(64,83,112);
}

internal sealed class ThemedNumber : UserControl
{
    private readonly TextBox editor = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, MaxLength = 3 };
    private readonly NeonButton minus = new() { Text = "−", AccessibleName = "Disminuir valor", TabStop = false };
    private readonly NeonButton plus = new() { Text = "+", AccessibleName = "Aumentar valor", TabStop = false };
    private decimal current, minimum, maximum = 100;
    private bool classic;
    public event EventHandler? ValueChanged;
    public decimal Minimum { get => minimum; set { minimum=value; Value=current; } }
    public decimal Maximum { get => maximum; set { maximum=value; Value=current; } }
    public decimal Value
    {
        get => current;
        set
        {
            decimal next=Math.Clamp(decimal.Truncate(value),minimum,maximum);
            bool changed=next!=current; current=next; ResetTextValue();
            if(changed) ValueChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public bool ClassicStyle
    {
        get=>classic;
        set
        {
            classic=value; BackColor=editor.BackColor=InputFrame.Surface(classic); editor.ForeColor=UiTheme.Text;
            minus.ClassicStyle=plus.ClassicStyle=classic; Invalidate(); minus.Invalidate(); plus.Invalidate();
        }
    }
    public ThemedNumber()
    {
        AutoScaleMode = AutoScaleMode.None;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        Size=new Size(102,34); MinimumSize=new Size(94,32); AccessibleRole=AccessibleRole.SpinButton;
        Font=new Font("Segoe UI",14,FontStyle.Regular,GraphicsUnit.Pixel);
        editor.Font=Font; editor.ForeColor=ControllerDashboard.Ink; editor.BackColor=InputFrame.Surface(false);
        Controls.AddRange([minus,editor,plus]); ResetTextValue();
        minus.Click+=(_,_)=>{ Commit(); Value-=1; }; plus.Click+=(_,_)=>{ Commit(); Value+=1; };
        editor.Enter+=(_,_)=>{editor.SelectAll();Invalidate();}; editor.Leave+=(_,_)=>{Commit();Invalidate();};
        editor.KeyDown+=(_,e)=>
        {
            if(e.KeyCode==Keys.Up || e.KeyCode==Keys.Down) {Commit();Value+=e.KeyCode==Keys.Up?1:-1;e.SuppressKeyPress=true;}
            if(e.KeyCode==Keys.Enter) {Commit();editor.SelectAll();e.SuppressKeyPress=true;}
            if(e.KeyCode==Keys.Escape) {ResetTextValue();editor.SelectAll();e.SuppressKeyPress=true;}
        };
        editor.MouseWheel+=(_,e)=>{Commit();Value+=Math.Sign(e.Delta);};
        FontChanged+=(_,_)=>{editor.Font=Font;PerformLayout();};
    }
    private void ResetTextValue()=>editor.Text=current.ToString("0",CultureInfo.CurrentCulture);
    private void Commit()
    {
        if(decimal.TryParse(editor.Text,NumberStyles.Integer,CultureInfo.CurrentCulture,out var number)) Value=number;
        else ResetTextValue();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if(editor is null || minus is null || plus is null) return;
        float scale = Font.Size / 14f;
        int Px(int value) => Math.Max(1, (int)Math.Round(value * scale));
        minus.SetBounds(Px(4),Px(4),Px(25),Math.Max(Px(24),Height-Px(8)));
        plus.SetBounds(Width-Px(29),Px(4),Px(25),Math.Max(Px(24),Height-Px(8)));
        editor.SetBounds(Px(31),Math.Max(Px(4),(Height-editor.PreferredHeight)/2),Math.Max(Px(28),Width-Px(62)),editor.PreferredHeight);
    }
    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor??ControllerDashboard.Background);
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=InputFrame.Round(new(1,1,Width-3,Height-3),9 * Font.Size / 14f);
        using(var fill=new SolidBrush(InputFrame.Surface(classic))) g.FillPath(fill,path);
        using(var edge=new Pen(ContainsFocus?UiTheme.Accent:InputFrame.Edge(classic),1)) g.DrawPath(edge,path);
    }
}

internal sealed class ThemedSelect : Control
{
    public List<string> Items { get; } = new();
    public event EventHandler? SelectedIndexChanged;
    private int selected=-1;
    private bool classic,hovered;
    private readonly ContextMenuStrip menu=new() { ShowImageMargin=false, ShowCheckMargin=false, Padding=new Padding(5) };
    public bool ClassicStyle { get=>classic; set {classic=value;Invalidate();} }
    public int SelectedIndex
    {
        get=>selected;
        set
        {
            if(value<0 || value>=Items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if(selected==value) return; selected=value; Invalidate(); SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public ThemedSelect()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        Size=new Size(184,34);Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.ComboBox;
        menu.Renderer=new MenuRenderer(); menu.Closed+=(_,_)=>Invalidate();
    }
    protected override void OnMouseEnter(EventArgs e) {hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e) {hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();OpenMenu();}}
    protected override void OnGotFocus(EventArgs e) {base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e) {base.OnLostFocus(e);Invalidate();}
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Up or Keys.Down || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(e.KeyCode is Keys.Space or Keys.Enter || (e.Alt&&e.KeyCode==Keys.Down)){OpenMenu();e.SuppressKeyPress=true;}
        else if(e.KeyCode is Keys.Up or Keys.Down) {SelectedIndex=Math.Clamp(selected+(e.KeyCode==Keys.Down?1:-1),0,Items.Count-1);e.SuppressKeyPress=true;}
    }
    private void OpenMenu()
    {
        if(menu.Visible){menu.Close();return;}
        var previous=menu.Items.Cast<ToolStripItem>().ToArray();menu.Items.Clear();
        foreach(var old in previous) old.Dispose();
        menu.BackColor=InputFrame.Surface(classic);menu.ForeColor=ControllerDashboard.Ink;menu.Font=Font;
        for(int i=0;i<Items.Count;i++)
        {
            int index=i;
            var item=new ToolStripMenuItem((i==selected?"✓  ":"    ")+Items[i]) {AutoSize=false,Width=Width-10,Height=Math.Max(32,Font.Height+16),ForeColor=ControllerDashboard.Ink};
            item.Click+=(_,_)=>SelectedIndex=index;menu.Items.Add(item);
        }
        menu.Show(this,new Point(0,Height+4));Invalidate();
    }
    protected override void OnPaintBackground(PaintEventArgs e)=>e.Graphics.Clear(Parent?.BackColor??ControllerDashboard.Background);
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        float scale = Height / 34f;
        using var path=InputFrame.Round(new(1,1,Width-3,Height-3),9 * scale);
        using(var fill=new SolidBrush(InputFrame.Surface(classic)))g.FillPath(fill,path);
        using(var edge=new Pen(hovered||Focused||menu.Visible?UiTheme.Accent:InputFrame.Edge(classic),1))g.DrawPath(edge,path);
        TextRenderer.DrawText(g,selected>=0?Items[selected]:"Elegir estilo",Font,new Rectangle((int)(13*scale),0,Math.Max(1,Width-(int)(48*scale)),Height),ControllerDashboard.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        using var arrow=new Pen(ControllerDashboard.Muted,1.7f*scale){StartCap=LineCap.Round,EndCap=LineCap.Round};
        float x=Width-22*scale,y=Height/2f;g.DrawLines(arrow,new PointF[]{new(x-4*scale,y-2*scale),new(x,y+2*scale),new(x+4*scale,y-2*scale)});
    }
    protected override void Dispose(bool disposing) {if(disposing)menu.Dispose();base.Dispose(disposing);}
    private sealed class MenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {using var brush=new SolidBrush(e.ToolStrip.BackColor);e.Graphics.FillRectangle(brush,e.AffectedBounds);}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {using var pen=new Pen(UiTheme.Edge);e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if(!e.Item.Selected)return;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using var path=InputFrame.Round(new(1,1,e.Item.Width-2,e.Item.Height-2),6);
            using var brush=new SolidBrush(UiTheme.IsLight?Color.FromArgb(220,233,250):Color.FromArgb(46,69,94));e.Graphics.FillPath(brush,path);
        }
    }
}
