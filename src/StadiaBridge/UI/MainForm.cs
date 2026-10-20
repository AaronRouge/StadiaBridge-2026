using System.Diagnostics;

namespace StadiaBridge;

internal sealed class MainForm : Form
{
    private readonly NeonButton start = new() { Text = "Conectar", AutoSize = true };
    private readonly NeonButton stop = new() { Text = "Detener", AutoSize = true, Enabled = false };
    private readonly Label status = new() { Text = "Listo. Conecta tu mando Stadia por USB.", AutoSize = true };
    private readonly ControllerDashboard dashboard = new();
    private readonly ResponsiveLayout responsiveLayout;
    private readonly Label bridgeStatus = new();
    private string bridgeMessage = "Conecta el Stadia con un cable USB de datos.";
    private StadiaReport physicalLatest;
    private bool deviceConnected;
    private bool previewMode;
    private bool interactivePreview;
    private readonly ThemedSelect themeSelector = new();
    private readonly ThemedSelect modeSelector = new() { Width = 135, AccessibleName = "Modo de color" };
    private readonly ThemedNumber deadzone = new() { Minimum = 0, Maximum = 30, Value = 5, AccessibleName = "Zona muerta en porcentaje" };
    private readonly ThemedNumber strength = new() { Minimum = 0, Maximum = 100, Value = 100, AccessibleName = "Intensidad de vibración en porcentaje" };
    private readonly VibrationPanel vibrationPanel = new();
    private RumbleState? activeRumble;
    private RumbleState? standaloneRumble;
    private string rumbleMessage = "Vibración USB lista para probar.";
    private CancellationTokenSource? testCancellation;
    private Task? testTask;
    private CancellationTokenSource? cancellation;
    private Task? worker;
    private int threshold = 5;
    private int vibrationGain = 100;
    private readonly object stateLock = new();
    private string message = "Listo. Conecta tu mando Stadia por USB.";
    private StadiaReport latest;
    private long count;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 33 };

    public MainForm()
    {
        Text = "Stadia Bridge · Panel de control";
        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("StadiaBridge.AppIcon"))
        {
            if (iconStream is not null)
            {
                using var originalIcon = new Icon(iconStream);
                Icon = (Icon)originalIcon.Clone();
                Disposed += (_, _) => Icon?.Dispose();
            }
        }
        // The responsive layout owns both fonts and geometry; do not mix it
        // with WinForms' inherited font scaling.
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(1280, 860); MinimumSize = new Size(1080, 790);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ControllerDashboard.Background; ForeColor = ControllerDashboard.Ink;
        Font = new Font("Segoe UI", 10 * 4f / 3f, FontStyle.Regular, GraphicsUnit.Pixel);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(24), BackColor = BackColor };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(new Label { Text = "S", Location = new Point(0, 4), Size = new Size(46, 46), TextAlign = ContentAlignment.MiddleCenter, BackColor = ControllerDashboard.Orange, ForeColor = BackColor, Font = new Font("Segoe UI", 23 * 4f / 3f, FontStyle.Bold, GraphicsUnit.Pixel) });
        header.Controls.Add(new Label { Text = "STADIA BRIDGE", AutoSize = true, Location = new Point(60, 0), Font = new Font("Segoe UI", 24 * 4f / 3f, FontStyle.Bold, GraphicsUnit.Pixel) });
        header.Controls.Add(new Label { Text = "Tu mando. Cada movimiento a la vista.", AutoSize = true, Location = new Point(63, 45), ForeColor = ControllerDashboard.Muted });
        var appearance = new Panel { Dock = DockStyle.Right, Width = 450 };
        var version = new Label { Text = $"STADIA BRIDGE  /  v{Application.ProductVersion.Split('+')[0]}", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleRight, ForeColor = ControllerDashboard.Muted };
        appearance.Controls.Add(version);
        appearance.Controls.Add(new Label { Text = "ESTILO", AutoSize = true, Location = new Point(0, 40), ForeColor = ControllerDashboard.Muted });
        themeSelector.Location = new Point(56, 32); themeSelector.BackColor = ControllerDashboard.Surface; themeSelector.ForeColor = ControllerDashboard.Ink;
        themeSelector.Items.AddRange(["Neón", "Clásico · Xbox"]);
        themeSelector.SelectedIndex = LoadClassicStyle() ? 1 : 0;
        themeSelector.SelectedIndexChanged += (_, _) =>
        {
            ApplyVisualStyle(themeSelector.SelectedIndex == 1);
            if (!previewMode) SaveVisualStyle(themeSelector.SelectedIndex == 1);
        };
        appearance.Controls.Add(themeSelector);
        appearance.Controls.Add(new Label { Text = "MODO", AutoSize = true, Location = new Point(264, 40), ForeColor = ControllerDashboard.Muted });
        modeSelector.Location = new Point(315, 32); modeSelector.Items.AddRange(["Oscuro", "Claro"]);
        modeSelector.SelectedIndex = LoadLightMode() ? 1 : 0;
        modeSelector.SelectedIndexChanged += (_, _) =>
        {
            ApplyVisualStyle(themeSelector.SelectedIndex == 1);
            if (!previewMode) SaveLightMode(modeSelector.SelectedIndex == 1);
        };
        appearance.Controls.Add(modeSelector); header.Controls.Add(appearance); root.Controls.Add(header, 0, 0);
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, i == 2 ? 150 : 112));
        var connection = new Panel { Dock = DockStyle.Fill };
        status.AutoSize = false; status.Dock = DockStyle.Top; status.Height = 27;
        status.ForeColor = ControllerDashboard.Teal; status.Font = new Font("Segoe UI", 11 * 4f / 3f, FontStyle.Bold, GraphicsUnit.Pixel);
        connection.Controls.Add(status);
        bridgeStatus.Dock = DockStyle.Bottom; bridgeStatus.Height = 29;
        bridgeStatus.ForeColor = ControllerDashboard.Muted; bridgeStatus.AutoEllipsis = true;
        bridgeStatus.Font = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel);
        connection.Controls.Add(bridgeStatus);
        toolbar.Controls.Add(connection, 0, 0);
        StyleButton(start, true); StyleButton(stop);
        start.Text = "Conectar";
        start.Dock = stop.Dock = DockStyle.Top; start.Height = stop.Height = 40;
        toolbar.Controls.Add(start, 1, 0); toolbar.Controls.Add(stop, 2, 0);
        var test = new NeonButton { Text = "Panel de Windows", Dock = DockStyle.Top, Height = 40 };
        StyleButton(test);
        test.Click += (_, _) => Process.Start(new ProcessStartInfo("control.exe", "joy.cpl") { UseShellExecute = true });
        toolbar.Controls.Add(test, 3, 0); root.Controls.Add(toolbar, 0, 1);
        dashboard.Dock = DockStyle.Fill; dashboard.Margin = Padding.Empty;
        root.Controls.Add(dashboard, 0, 2);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 14, 0, 0) };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        var settings = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(16, 9, 16, 7), BackColor = ControllerDashboard.Surface, Margin = new Padding(0, 0, 14, 0) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 21)); settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 37)); settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        settings.Controls.Add(new Label { Text = "ZONA MUERTA", AutoSize = true, ForeColor = ControllerDashboard.Muted, Font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Pixel) }, 0, 0);
        settings.Controls.Add(new Label { Text = "VIBRACIÓN", AutoSize = true, ForeColor = ControllerDashboard.Muted, Font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Pixel) }, 1, 0);
        var zoneRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        StyleNumber(deadzone); deadzone.Margin = Padding.Empty; zoneRow.Controls.Add(deadzone);
        zoneRow.Controls.Add(new Label { Text = "%", Width = 24, Height = 34, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty });
        var strengthRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        StyleNumber(strength); strength.Margin = Padding.Empty; strengthRow.Controls.Add(strength);
        strengthRow.Controls.Add(new Label { Text = "%", Width = 24, Height = 34, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty });
        settings.Controls.Add(zoneRow, 0, 1); settings.Controls.Add(strengthRow, 1, 1);
        var hint = new Label { Text = "Los círculos muestran la lectura física; la zona muerta se aplica a Xbox.\nExtra 1 / Extra 2: identificación pendiente. Bluetooth no disponible.", Dock = DockStyle.Fill, ForeColor = ControllerDashboard.Muted, Font = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel), Margin = new Padding(0, 4, 0, 0) };
        settings.Controls.Add(hint, 0, 2); settings.SetColumnSpan(hint, 2);
        footer.Controls.Add(settings, 0, 0);
        vibrationPanel.Dock = DockStyle.Fill; vibrationPanel.Margin = Padding.Empty;
        footer.Controls.Add(vibrationPanel, 1, 0);
        root.Controls.Add(footer, 0, 3); Controls.Add(root);
        PerformLayout(); root.PerformLayout();
        responsiveLayout = new ResponsiveLayout(root);
        ClientSizeChanged += (_, _) => responsiveLayout.Apply(ClientSize);
        DpiChanged += (_, _) =>
        {
            if (!IsDisposed) BeginInvoke((Action)(() => { FitMinimumToScreen(); responsiveLayout.Apply(ClientSize); }));
        };
        Disposed += (_, _) => responsiveLayout.Dispose();
        ApplyVisualStyle(themeSelector.SelectedIndex == 1);
        deadzone.ValueChanged += (_, _) => Volatile.Write(ref threshold, (int)deadzone.Value);
        strength.ValueChanged += (_, _) =>
        {
            Volatile.Write(ref vibrationGain, (int)strength.Value);
            lock (stateLock) activeRumble?.SetGain((int)strength.Value);
            lock (stateLock) standaloneRumble?.SetGain((int)strength.Value);
        };
        vibrationPanel.PatternRequested += async pattern =>
        {
            if (previewMode)
            {
                lock (stateLock)
                {
                    standaloneRumble ??= new RumbleState();
                    standaloneRumble.SetGain(vibrationGain); standaloneRumble.Test(pattern);
                }
                SetRumbleStatus($"Vista de diseño: {RumbleState.Name(pattern)} · sin salida USB."); return;
            }
            if (testTask is { IsCompleted: false }) return;
            testCancellation = new();
            testTask = TestRumbleAsync(pattern, testCancellation.Token);
            try { await testTask; }
            finally { testCancellation.Dispose(); testCancellation = null; }
        };
        vibrationPanel.StopRequested += () =>
        {
            lock (stateLock) { activeRumble?.CancelTest(); standaloneRumble?.CancelTest(); }
            testCancellation?.Cancel();
            SetRumbleStatus("Prueba detenida · se recupera la salida del juego si está activa.");
        };
        start.Click += (_, _) => Start(); stop.Click += async (_, _) => await StopAsync();
        FormClosing += async (_, e) =>
        {
            if ((worker is null || worker.IsCompleted) && (testTask is null || testTask.IsCompleted)) return;
            e.Cancel = true; await StopAsync(); Close();
        };
        timer.Tick += (_, _) => RefreshDashboard();
        timer.Start();
        Shown += (_, _) => { if (!previewMode) Start(); };
    }

    protected override void OnLoad(EventArgs e)
    {
        if (!previewMode || interactivePreview)
        {
            var area = Screen.FromControl(this).WorkingArea;
            var frame = SizeFromClientSize(Size.Empty);
            float scale = Math.Min(DeviceDpi / 96f,
                Math.Min((area.Width - frame.Width - 32) / 1280f, (area.Height - frame.Height - 32) / 860f));
            MinimumSize = Size.Empty;
            ClientSize = new Size((int)(1280 * scale), (int)(860 * scale));
            FitMinimumToScreen();
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }
        base.OnLoad(e);
        responsiveLayout.Apply(ClientSize);
    }

    private void FitMinimumToScreen()
    {
        var area = Screen.FromControl(this).WorkingArea;
        float scale = Math.Min(DeviceDpi / 96f, Math.Min(area.Width / 1280f, area.Height / 900f));
        var desired = SizeFromClientSize(new Size((int)(1080 * scale), (int)(790 * scale)));
        MinimumSize = new Size(Math.Min(desired.Width, area.Width), Math.Min(desired.Height, area.Height));
    }

    private static void StyleButton(Button button, bool primary = false)
    {
        if (button is NeonButton neon) neon.Primary = primary;
        button.AutoSize = false; button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(62, 77, 93);
        button.BackColor = primary ? ControllerDashboard.Teal : ControllerDashboard.Surface;
        button.ForeColor = primary ? ControllerDashboard.Background : ControllerDashboard.Ink;
        button.Font = new Font("Segoe UI", 10 * 4f / 3f, FontStyle.Bold, GraphicsUnit.Pixel);
        button.Cursor = Cursors.Hand; button.Margin = new Padding(6, 4, 0, 0);
    }
    private static void StyleNumber(ThemedNumber number)
    {
        number.BackColor = ControllerDashboard.Background; number.ForeColor = ControllerDashboard.Ink;
        number.Width = 102; number.Height = 34;
    }
    private void RefreshDashboard()
    {
        lock (stateLock)
        {
            status.Text = message; bridgeStatus.Text = bridgeMessage; vibrationPanel.StatusMessage = rumbleMessage;
            var rumble = activeRumble ?? standaloneRumble;
            long timestamp = Stopwatch.GetTimestamp();
            vibrationPanel.UpdateMotors(rumble?.Current(timestamp) ?? 0, rumble?.ActivePattern(timestamp));
            dashboard.UpdateState(physicalLatest, latest.Xbox, deviceConnected, count, Volatile.Read(ref threshold));
        }
        if (worker?.IsCompleted == true && (testTask is null || testTask.IsCompleted)) { start.Enabled = true; stop.Enabled = false; }
    }

    internal void RenderPreview(string path, bool pressed, Size size, bool classicStyle = false, bool lightMode = false)
    {
        previewMode = true; timer.Stop();
        modeSelector.SelectedIndex = lightMode ? 1 : 0;
        themeSelector.SelectedIndex = classicStyle ? 1 : 0;
        ApplyVisualStyle(classicStyle);
        ShowInTaskbar = false; Opacity = 0;
        StartPosition = FormStartPosition.Manual; Location = new Point(-20000, -20000);
        ClientSize = size;
        Show(); Application.DoEvents();
        deviceConnected = true;
        message = "VISTA DE DISEÑO · DATOS SIMULADOS";
        bridgeMessage = "La captura ilustra el diseño; no representa una lectura del mando físico.";
        rumbleMessage = "Pulso USB de medio segundo · intensidad ajustable";
        physicalLatest = pressed
            ? new StadiaReport(new XboxReport { Buttons = 0x1101, LX = 20000, LY = 12000, RX = -8000, RY = -16000, LeftTrigger = 175, RightTrigger = 64 }, true, false)
            : default;
        latest = physicalLatest; count = pressed ? 1842 : 0;
        RefreshDashboard(); PerformLayout(); Application.DoEvents();
        SaveView(path);
    }

    internal void EnableInteractivePreview()
    {
        previewMode = interactivePreview = true;
        Text = "Stadia Bridge · PRUEBA DE ESCALADO · DATOS SIMULADOS";
        start.Enabled = stop.Enabled = false;
        message = "PRUEBA DE ESCALADO · DATOS SIMULADOS";
        bridgeMessage = "Maximiza, restaura y cambia el tamaño. Esta ventana no utiliza el mando USB.";
        rumbleMessage = "Modo de diseño: no se envían órdenes a los motores.";
        physicalLatest = new StadiaReport(new XboxReport { Buttons = 0x1101, LX = 20000, LY = 12000, RX = -8000, RY = -16000, LeftTrigger = 175, RightTrigger = 64 }, true, false);
        latest = physicalLatest;
        deviceConnected = true;
        RefreshDashboard();
    }

    internal void ValidatePreviewLayout(string path)
    {
        if (!previewMode) throw new InvalidOperationException("La validación de diseño requiere datos simulados.");
        float scale = responsiveLayout.Scale;
        var lines = new List<string>
        {
            "VISTA SIMULADA; no valida USB ni un cambio físico de monitor.",
            $"ClientSize: {ClientSize}; DPI real del proceso: {DeviceDpi}; escala de diseño: {scale:F3}",
            $"Conexión: fuente {status.Font.Size:F3}px; botón {start.Size}; fuente botón {start.Font.Size:F3}px",
            $"Ajustes: campo {deadzone.Size}; fuente {deadzone.Font.Size:F3}px; panel de vibración {vibrationPanel.Size}",
        };
        void Require(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Escalado: " + name);
            lines.Add("PASS: " + name);
        }
        Require(Math.Abs(status.Font.Size - 44f / 3f * scale) < .05f, "la cabecera usa la escala común");
        Require(Math.Abs(start.Font.Size - 40f / 3f * scale) < .05f, "los botones usan la escala común");
        Require(Math.Abs(deadzone.Font.Size - 14 * scale) < .05f, "los ajustes usan la escala común");
        foreach (var control in new Control[] { status, start, stop, deadzone, strength, vibrationPanel, dashboard }.Concat(vibrationPanel.Options))
        {
            var parent = control.Parent!;
            Require(control.Left >= -1 && control.Top >= -1 && control.Right <= parent.ClientSize.Width + 1 && control.Bottom <= parent.ClientSize.Height + 1,
                $"{control.GetType().Name} {control.Text}: dentro de su panel");
        }
        foreach (var number in new[] { deadzone, strength })
        {
            var editor = number.Controls.OfType<TextBox>().Single();
            Require(editor.Top >= 0 && editor.Bottom <= number.Height && editor.Right <= number.Width,
                $"editor {number.AccessibleName}: sin recorte vertical");
            int textWidth = TextRenderer.MeasureText("100", editor.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            Require(textWidth <= editor.Width, $"editor {number.AccessibleName}: tres dígitos legibles");
        }
        File.WriteAllLines(path, lines);
    }

    private void ApplyVisualStyle(bool classic)
    {
        Color oldMuted=ControllerDashboard.Muted;
        void CaptureRoles(Control parent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child.Tag is null && child is Label)
                    child.Tag=child.ForeColor==oldMuted?"muted":child.ForeColor==ControllerDashboard.Teal?"accent":child.ForeColor==ControllerDashboard.Orange?"amber":"text";
                if(child.Tag is null && child is (Panel or TableLayoutPanel or ControllerDashboard))
                    child.Tag=child is ControllerDashboard?"canvas":child.BackColor==ControllerDashboard.Surface || child.BackColor==Color.FromArgb(29,27,25)?"surface":"canvas";
                CaptureRoles(child);
            }
        }
        CaptureRoles(this);
        UiTheme.IsLight=modeSelector.SelectedIndex==1;
        dashboard.ClassicStyle=classic;
        Color background=UiTheme.IsLight?UiTheme.Background:classic?Color.FromArgb(12,11,10):UiTheme.Background;
        Color surface=UiTheme.IsLight?UiTheme.Surface:classic?Color.FromArgb(29,27,25):UiTheme.Surface;
        BackColor=background;ForeColor=UiTheme.Text;
        void Recolor(Control parent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child is Panel or TableLayoutPanel or ControllerDashboard)
                    child.BackColor=child.Tag as string=="surface"?surface:background;
                if(child is Label)
                    child.ForeColor=(child.Tag as string) switch {"muted"=>UiTheme.Muted,"accent"=>UiTheme.Accent,"amber"=>UiTheme.Amber,_=>UiTheme.Text};
                if(child is NeonButton button){button.ClassicStyle=classic;button.Invalidate();}
                if(child is ThemedNumber number)number.ClassicStyle=classic;
                if(child is ThemedSelect select){select.ClassicStyle=classic;select.Invalidate();}
                Recolor(child);
            }
        }
        Recolor(this);vibrationPanel.SetTheme(classic);dashboard.Invalidate();ApplyTitleBar();
    }
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    private void ApplyTitleBar()
    {
        if(!IsHandleCreated)return;
        int dark=UiTheme.IsLight?0:1;
        DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);ApplyTitleBar();}
    private static string ModePreferencePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"StadiaBridge","color-mode.txt");
    private static bool LoadLightMode()
    {
        try{return File.Exists(ModePreferencePath)&&File.ReadAllText(ModePreferencePath).Trim()=="light";}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){return false;}
    }
    private static void SaveLightMode(bool light)
    {
        try{Directory.CreateDirectory(Path.GetDirectoryName(ModePreferencePath)!);File.WriteAllText(ModePreferencePath,light?"light":"dark");}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
    }
    private static string StylePreferencePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StadiaBridge", "visual-style.txt");
    private static bool LoadClassicStyle()
    {
        try { return !File.Exists(StylePreferencePath) || File.ReadAllText(StylePreferencePath).Trim() != "neon"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return true; }
    }
    private static void SaveVisualStyle(bool classic)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(StylePreferencePath)!); File.WriteAllText(StylePreferencePath, classic ? "classic" : "neon"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Appearance still applies for this session. */ }
    }

    internal void SaveView(string path)
    {
        RefreshDashboard(); PerformLayout(); Application.DoEvents();
        using var image = new Bitmap(ClientSize.Width, ClientSize.Height);
        Controls[0].DrawToBitmap(image, new Rectangle(Point.Empty, ClientSize)); image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        lock (stateLock)
            File.WriteAllText(Path.ChangeExtension(path, ".txt"), $"Simulado: {previewMode}\nUSB conectado: {deviceConnected}\nReportes: {count}\nEstado: {message}\nEmulación: {bridgeMessage}\nBotones: 0x{physicalLatest.Xbox.Buttons:X4}\nSticks físicos: {physicalLatest.Xbox.LX}, {physicalLatest.Xbox.LY}, {physicalLatest.Xbox.RX}, {physicalLatest.Xbox.RY}\nGatillos: {physicalLatest.Xbox.LeftTrigger}, {physicalLatest.Xbox.RightTrigger}");
    }
    private void SetStatus(string text) { lock (stateLock) message = text; }
    private void Start()
    {
        if (worker is { IsCompleted: false }) return;
        cancellation?.Dispose(); cancellation = new();
        start.Enabled = false; stop.Enabled = true;
        worker = Task.Run(() => RunAsync(cancellation.Token));
    }
    private async Task StopAsync()
    {
        testCancellation?.Cancel();
        cancellation?.Cancel();
        if (testTask is not null) await testTask;
        if (worker is not null) await worker;
        start.Enabled = true; stop.Enabled = false;
    }
    private void SetRumbleStatus(string text) { lock (stateLock) rumbleMessage = text; }
    private async Task TestRumbleAsync(RumblePattern pattern, CancellationToken token)
    {
        lock (stateLock)
        {
            if (activeRumble is not null) { activeRumble.Test(pattern); rumbleMessage = $"Prueba USB: {RumbleState.Name(pattern)} · intensidad {vibrationGain} %."; return; }
        }
        if (worker is { IsCompleted: false }) { SetRumbleStatus("Espera a que se conecte el mando o pulsa Detener para probar por separado."); return; }
        // Standalone test does not need ViGEmBus and cannot race the session.
        start.Enabled = false; stop.Enabled = true;
        try
        {
            var devices = Hid.Find();
            if (devices.Count == 0) { SetRumbleStatus("No se encontró un mando Stadia USB. Conecta un cable de datos."); return; }
            using var output = Hid.OpenOutput(devices[0]);
            var state = new RumbleState(); state.SetGain((int)strength.Value); state.Test(pattern);
            lock (stateLock) standaloneRumble = state;
            using var duration = CancellationTokenSource.CreateLinkedTokenSource(token);
            duration.CancelAfter(RumbleState.DurationMilliseconds(pattern) + 150);
            SetRumbleStatus($"Prueba USB: {RumbleState.Name(pattern)} · intensidad {vibrationGain} %.");
            try { await RumblePump.RunAsync(output, devices[0].OutputLength, state, duration.Token); }
            catch (OperationCanceledException) when (duration.IsCancellationRequested) { }
            SetRumbleStatus(token.IsCancellationRequested ? "Prueba detenida." : "Prueba finalizada. Comprueba la respuesta del mando.");
        }
        catch (Exception ex) { SetRumbleStatus($"No se pudo probar la vibración: {ex.Message}"); }
        finally { lock (stateLock) standaloneRumble = null; start.Enabled = true; stop.Enabled = false; }
    }
    private async Task ForwardRumbleAsync(Hid.Device device, RumbleState state, CancellationToken token)
    {
        try
        {
            using var output = Hid.OpenOutput(device);
            SetRumbleStatus("Salida de vibración USB abierta.");
            await RumblePump.RunAsync(output, device.OutputLength, state, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { SetRumbleStatus("Vibración detenida; apagado de motores solicitado."); }
        catch (Exception ex) { SetRumbleStatus($"Vibración USB no disponible: {ex.Message}"); }
        finally { lock (stateLock) { if (ReferenceEquals(activeRumble, state)) activeRumble = null; } }
    }
    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                SetStatus("Buscando mando Stadia USB…");
                var devices = Hid.Find();
                if (devices.Count == 0) { await Task.Delay(1000, token); continue; }
                try
                {
                    using var input = Hid.Open(devices[0]);
                    SetStatus("●  Stadia detectado · pulsa un botón o mueve un stick");
                    lock (stateLock) { deviceConnected = true; bridgeMessage = "Esperando el primer reporte del mando…"; }
                    var buffer = new byte[devices[0].InputLength];
                    VirtualXbox? output = null;
                    bool initialized = false;
                    var rumble = new RumbleState();
                    // Read UI settings through their thread-safe numeric copy.
                    rumble.SetGain(Volatile.Read(ref vibrationGain));
                    using var vibrationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    Task? vibrationTask = null;
                    try
                    {
                        while (true)
                        {
                            int read = await input.ReadAsync(buffer, token);
                            if (read == 0) throw new IOException("El mando dejó de enviar datos.");
                            if (!StadiaReport.TryParse(buffer.AsSpan(0, read), Volatile.Read(ref threshold), out var report))
                            {
                                output?.Send(default); rumble.SetMotors(0, 0);
                                lock (stateLock) { physicalLatest = default; latest = default; deviceConnected = false; }
                                SetStatus($"Formato USB no reconocido ({read} bytes). Se requiere validar este firmware."); continue;
                            }
                            StadiaReport.TryParse(buffer.AsSpan(0, read), 0, out var raw);
                            if (!initialized)
                            {
                                initialized = true;
                                lock (stateLock) { rumble.SetGain(Volatile.Read(ref vibrationGain)); activeRumble = rumble; }
                                vibrationTask = ForwardRumbleAsync(devices[0], rumble, vibrationCancellation.Token);
                                try
                                {
                                    output = new VirtualXbox(rumble);
                                    lock (stateLock) bridgeMessage = "Xbox 360 virtual activo · listo para juegos XInput";
                                }
                                catch (Exception ex)
                                {
                                    lock (stateLock) bridgeMessage = $"Solo visor · {ex.Message}";
                                }
                            }
                            if (output is not null)
                            {
                                try { output.Send(report.Xbox); }
                                catch (Exception ex)
                                {
                                    output.Dispose(); output = null;
                                    rumble.SetMotors(0, 0);
                                    lock (stateLock) bridgeMessage = $"Solo visor · emulación interrumpida: {ex.Message}";
                                }
                            }
                            lock (stateLock) { latest = report; physicalLatest = raw; deviceConnected = true; count++; }
                            SetStatus(output is null ? "●  Stadia conectado · visor USB activo" : "●  Stadia conectado · visor + Xbox activos");
                        }
                    }
                    finally
                    {
                        try { output?.Dispose(); }
                        finally
                        {
                            vibrationCancellation.Cancel();
                            if (vibrationTask is not null) await vibrationTask;
                            lock (stateLock) { activeRumble = null; latest = default; physicalLatest = default; deviceConnected = false; bridgeMessage = "Conecta el Stadia con un cable USB de datos."; }
                        }
                    }
                }
                catch (IOException ex) { SetStatus($"Conexión interrumpida: {ex.Message}"); await Task.Delay(1000, token); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { SetStatus("Detenido. Mando virtual retirado."); }
        catch (Exception ex) { SetStatus($"Error: {ex.Message}"); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); cancellation?.Dispose(); }
        base.Dispose(disposing);
    }
}
