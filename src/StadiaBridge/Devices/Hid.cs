using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace StadiaBridge;

internal static class Hid
{
    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceData { public int Size; public Guid Class; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Attributes { public int Size; public ushort Vendor, Product, Version; }
    [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")] private static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attributes);
    [DllImport("hid.dll")] private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
    [DllImport("hid.dll")] private static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, IntPtr caps);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(ref Guid guid, IntPtr enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint required, IntPtr device);
    [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    internal sealed record Device(string Path, int InputLength, int OutputLength);

    public static List<Device> Find()
    {
        var found = new List<Device>();
        HidD_GetHidGuid(out var guid);
        var set = SetupDiGetClassDevsW(ref guid, IntPtr.Zero, IntPtr.Zero, 0x12);
        if (set == new IntPtr(-1)) throw new Win32Exception();
        try
        {
            for (uint i = 0; ; i++)
            {
                var info = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref info))
                {
                    if (Marshal.GetLastWin32Error() != 259) throw new Win32Exception();
                    break;
                }
                SetupDiGetDeviceInterfaceDetailW(set, ref info, IntPtr.Zero, 0, out uint length, IntPtr.Zero);
                if (length < 8) continue;
                var detail = Marshal.AllocHGlobal((int)length);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetailW(set, ref info, detail, length, out _, IntPtr.Zero)) throw new Win32Exception();
                    var path = Marshal.PtrToStringUni(detail + 4)!;
                    using var handle = CreateFileW(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                    if (handle.IsInvalid) continue;
                    var attr = new Attributes { Size = Marshal.SizeOf<Attributes>() };
                    if (!HidD_GetAttributes(handle, ref attr) || attr.Vendor != 0x18D1 || attr.Product != 0x9400) continue;
                    if (!HidD_GetPreparsedData(handle, out var preparsed)) continue;
                    var caps = Marshal.AllocHGlobal(64);
                    try
                    {
                        if (HidP_GetCaps(preparsed, caps) != 0x110000) continue;
                        int usage = (ushort)Marshal.ReadInt16(caps), page = (ushort)Marshal.ReadInt16(caps, 2);
                        int inputLength = (ushort)Marshal.ReadInt16(caps, 4);
                        if (page == 1 && (usage == 4 || usage == 5) && inputLength >= 10)
                            found.Add(new(path, inputLength, (ushort)Marshal.ReadInt16(caps, 6)));
                    }
                    finally { Marshal.FreeHGlobal(caps); HidD_FreePreparsedData(preparsed); }
                }
                finally { Marshal.FreeHGlobal(detail); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return found;
    }

    public static FileStream Open(Device device)
    {
        var handle = CreateFileW(device.Path, 0x80000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new Win32Exception(); }
        return new FileStream(handle, FileAccess.Read, device.InputLength, true);
    }

    public static FileStream OpenOutput(Device device)
    {
        if (device.OutputLength < 5) throw new IOException("El mando no anuncia un reporte USB de vibración compatible.");
        var handle = CreateFileW(device.Path, 0x40000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new Win32Exception(); }
        return new FileStream(handle, FileAccess.Write, device.OutputLength, true);
    }
}
