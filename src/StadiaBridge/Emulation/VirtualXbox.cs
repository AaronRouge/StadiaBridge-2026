using System.Runtime.InteropServices;

namespace StadiaBridge;

internal sealed class VirtualXbox : IDisposable
{
    private IntPtr client, target;
    private bool connected, added, registered;
    private readonly Notification? notification;
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void Notification(IntPtr client, IntPtr target, byte large, byte small, byte led, IntPtr user);
    // Restrict dependency loading to the application directory.
    static VirtualXbox() => NativeLibrary.SetDllImportResolver(typeof(VirtualXbox).Assembly,
        (name, assembly, search) => name == "ViGEmClient.dll"
            ? NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, name)) : IntPtr.Zero);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr vigem_alloc();
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint vigem_connect(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr vigem_target_x360_alloc();
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint vigem_target_add(IntPtr client, IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint vigem_target_x360_update(IntPtr client, IntPtr target, XboxReport report);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint vigem_target_remove(IntPtr client, IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void vigem_target_free(IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void vigem_disconnect(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void vigem_free(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint vigem_target_x360_register_notification(IntPtr client, IntPtr target, Notification callback, IntPtr user);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void vigem_target_x360_unregister_notification(IntPtr target);

    public VirtualXbox(RumbleState? rumble = null)
    {
        if (rumble is not null) notification = (_, _, large, small, _, _) => rumble.SetMotors(large, small);
        try
        {
            client = vigem_alloc();
            if (client == IntPtr.Zero) throw new InvalidOperationException("No se pudo reservar el cliente ViGEm.");
            Check(vigem_connect(client)); connected = true;
            target = vigem_target_x360_alloc();
            if (target == IntPtr.Zero) throw new InvalidOperationException("No se pudo reservar el mando virtual.");
            Check(vigem_target_add(client, target)); added = true;
            if (notification is not null)
            {
                Check(vigem_target_x360_register_notification(client, target, notification, IntPtr.Zero));
                registered = true;
            }
        }
        catch { Dispose(); throw; }
    }
    public void Send(XboxReport report) => Check(vigem_target_x360_update(client, target, report));
    public static string ProbeBus()
    {
        var probe = vigem_alloc();
        if (probe == IntPtr.Zero) return "No se pudo reservar el cliente.";
        try
        {
            uint result = vigem_connect(probe);
            if (result == 0x20000000) { vigem_disconnect(probe); return "ViGEmBus disponible."; }
            return $"ViGEmBus no disponible: 0x{result:X8}";
        }
        finally { vigem_free(probe); }
    }
    private static void Check(uint result)
    {
        if (result == 0x20000000) return;
        throw new InvalidOperationException(result == 0xE0000001
            ? "Falta ViGEmBus. Instala la versión oficial 1.22.0 y vuelve a iniciar."
            : $"ViGEm devolvió 0x{result:X8}.");
    }
    public void Dispose()
    {
        if (registered) { vigem_target_x360_unregister_notification(target); registered = false; }
        if (added) { vigem_target_x360_update(client, target, default); vigem_target_remove(client, target); added = false; }
        if (target != IntPtr.Zero) { vigem_target_free(target); target = IntPtr.Zero; }
        if (connected) { vigem_disconnect(client); connected = false; }
        if (client != IntPtr.Zero) { vigem_free(client); client = IntPtr.Zero; }
        GC.KeepAlive(notification);
    }
}
