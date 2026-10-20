namespace StadiaBridge;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--ui-test"))
        {
            ApplicationConfiguration.Initialize();
            using var test = new MainForm();
            test.EnableInteractivePreview();
            Application.Run(test);
            return;
        }
        if (args.Length == 2 && args[0] == "--preview-scaling")
        {
            ApplicationConfiguration.Initialize();
            Directory.CreateDirectory(args[1]);
            using var preview = new MainForm();
            foreach (var size in new[] { new Size(1280, 860), new Size(1080, 790), new Size(1920, 1290), new Size(2560, 1440), new Size(3840, 2060), new Size(1280, 860) })
            {
                string suffix = size == new Size(1280, 860) && File.Exists(Path.Combine(args[1], "dark-1280x860.png")) ? "-return" : "";
                string name = $"dark-{size.Width}x{size.Height}{suffix}";
                preview.RenderPreview(Path.Combine(args[1], name + ".png"), true, size, true);
                preview.ValidatePreviewLayout(Path.Combine(args[1], name + ".layout.txt"));
            }
            preview.RenderPreview(Path.Combine(args[1], "light-3840x2060.png"), true, new Size(3840, 2060), false, true);
            preview.ValidatePreviewLayout(Path.Combine(args[1], "light-3840x2060.layout.txt"));
            return;
        }
        if (args.Length == 2 && args[0] == "--preview-ui")
        {
            ApplicationConfiguration.Initialize();
            Directory.CreateDirectory(args[1]);
            using var preview = new MainForm();
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-neutral.png"), false, new Size(1280, 860));
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-active.png"), true, new Size(1280, 860));
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-compact.png"), true, new Size(1080, 790));
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-classic-neutral.png"), false, new Size(1280, 860), true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-classic-active.png"), true, new Size(1280, 860), true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-classic-compact.png"), true, new Size(1080, 790), true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-light-neon.png"), true, new Size(1280, 860), false, true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-light-neon-compact.png"), true, new Size(1080, 790), false, true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-light-classic.png"), true, new Size(1280, 860), true, true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-light-classic-compact.png"), true, new Size(1080, 790), true, true);
            preview.RenderPreview(Path.Combine(args[1], "stadia-bridge-dark-return.png"), true, new Size(1280, 860), true, false);
            return;
        }
        if (args.Contains("--check-vigem"))
        {
            try { Environment.ExitCode = VirtualXbox.ProbeBus() == "ViGEmBus disponible." ? 0 : 1; }
            catch { Environment.ExitCode = 2; }
            return;
        }
        if (args.Contains("--test-rumble"))
        {
            TestRumble();
            return;
        }
        if (args.Contains("--diagnose"))
        {
            var lines = new List<string> { $"Stadia Bridge | {DateTimeOffset.Now:O}", $"Windows: {Environment.OSVersion}", $"x64: {Environment.Is64BitProcess}" };
            try { lines.Add($"Interfaces Stadia USB: {Hid.Find().Count}"); }
            catch (Exception ex) { lines.Add($"Error HID: {ex.Message}"); }
            try { lines.Add(VirtualXbox.ProbeBus()); }
            catch (Exception ex) { lines.Add($"Error ViGEm: {ex.Message}"); }
            lines.Add("El diagnóstico no crea un mando virtual ni instala controladores.");
            File.WriteAllLines(LogPath("diagnostico.txt"), lines);
            return;
        }
        ApplicationConfiguration.Initialize();
        using var single = new Mutex(true, "Local\\StadiaBridgeUsb", out bool first);
        if (!first) { MessageBox.Show("Stadia Bridge ya está abierto."); return; }
        using var form = new MainForm();
        if (args.Length == 2 && args[0] == "--capture-usb-ui")
        {
            Directory.CreateDirectory(args[1]);
            form.Opacity = 0; form.ShowInTaskbar = false;
            form.Shown += async (_, _) =>
            {
                await Task.Delay(15000);
                form.SaveView(Path.Combine(args[1], "stadia-bridge-usb.png"));
                form.Close();
            };
        }
        Application.Run(form);
    }

    private static void TestRumble()
    {
        var lines = new List<string> { $"Prueba USB de vibración | {DateTimeOffset.Now:O}" };
        using var single = new Mutex(false, "Local\\StadiaBridgeUsb");
        bool acquired = false;
        try
        {
            try { acquired = single.WaitOne(0); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) { lines.Add("Cierra la ventana de Stadia Bridge antes de ejecutar la prueba por comando."); return; }
            // Keep named mutex ownership on this thread across the USB operation.
            TestRumbleCoreAsync(lines).GetAwaiter().GetResult();
        }
        catch (Exception ex) { lines.Add($"ERROR: {ex.Message}"); }
        finally
        {
            if (acquired) single.ReleaseMutex();
            File.WriteAllLines(LogPath("prueba-vibracion.txt"), lines);
        }
    }

    private static async Task TestRumbleCoreAsync(List<string> lines)
    {
        var devices = Hid.Find();
        lines.Add($"Interfaces Stadia USB: {devices.Count}");
        if (devices.Count == 0) { lines.Add("No se envió vibración: conecta el mando mediante un cable USB de datos."); return; }
        var device = devices[0];
        lines.Add($"Reporte de salida: {device.OutputLength} bytes");
        using var output = Hid.OpenOutput(device);
        var state = new RumbleState(); state.SetGain(100); state.Test();
        using var duration = new CancellationTokenSource(650);
        try { await RumblePump.RunAsync(output, device.OutputLength, state, duration.Token); }
        catch (OperationCanceledException) when (duration.IsCancellationRequested) { }
        lines.Add("Pulso de medio segundo solicitado a ambos motores al 50 %; prueba finalizada.");
        lines.Add("La respuesta física debe confirmarla el usuario. No se necesita ViGEmBus para esta prueba.");
    }

    private static string LogPath(string name)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StadiaBridge", "Logs");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, name);
    }
}
