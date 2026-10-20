namespace StadiaBridge;

internal static class UiTheme
{
    public static bool IsLight { get; set; }
    public static Color Background => IsLight ? Color.FromArgb(243,246,251) : Color.FromArgb(8,10,18);
    public static Color Surface => IsLight ? Color.White : Color.FromArgb(17,21,34);
    public static Color Text => IsLight ? Color.FromArgb(28,39,56) : Color.FromArgb(237,242,247);
    public static Color Muted => IsLight ? Color.FromArgb(87,103,123) : Color.FromArgb(137,151,177);
    public static Color Edge => IsLight ? Color.FromArgb(196,207,223) : Color.FromArgb(64,83,112);
    public static Color Accent => IsLight ? Color.FromArgb(0,113,128) : Color.FromArgb(39,233,237);
    public static Color Purple => IsLight ? Color.FromArgb(111,55,199) : Color.FromArgb(174,110,255);
    public static Color Amber => IsLight ? Color.FromArgb(155,83,0) : Color.FromArgb(255,171,86);
}
