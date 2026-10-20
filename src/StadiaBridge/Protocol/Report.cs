using System.Runtime.InteropServices;

namespace StadiaBridge;

[StructLayout(LayoutKind.Sequential)]
public struct XboxReport
{
    public ushort Buttons;
    public byte LeftTrigger, RightTrigger;
    public short LX, LY, RX, RY;
}

public readonly record struct StadiaReport(XboxReport Xbox, bool Extra1, bool Extra2)
{
    // Protocol reference: 71/stadiacontroller (ISC). See THIRD-PARTY-NOTICES.txt.
    public static bool TryParse(ReadOnlySpan<byte> bytes, int deadzone, out StadiaReport report)
    {
        report = default;
        if (bytes.Length > 0 && bytes[0] == 0) bytes = bytes[1..];
        if (bytes.Length < 10 || bytes[0] != 3 || bytes[1] > 8) return false;
        var x = new XboxReport();
        int[] hats = [1, 9, 8, 10, 2, 6, 4, 5, 0];
        x.Buttons = (ushort)hats[bytes[1]];
        int[] masks = [0x1000, 0x2000, 0x4000, 0x8000, 0x100, 0x200, 0x40];
        for (int i = 0; i < masks.Length; i++)
            if ((bytes[3] & (0x40 >> i)) != 0) x.Buttons |= (ushort)masks[i];
        if ((bytes[2] & 0x80) != 0) x.Buttons |= 0x80;
        if ((bytes[2] & 0x40) != 0) x.Buttons |= 0x20;
        if ((bytes[2] & 0x20) != 0) x.Buttons |= 0x10;
        if ((bytes[2] & 0x10) != 0) x.Buttons |= 0x400;
        x.LX = Axis(bytes[4], false, deadzone); x.LY = Axis(bytes[5], true, deadzone);
        x.RX = Axis(bytes[6], false, deadzone); x.RY = Axis(bytes[7], true, deadzone);
        x.LeftTrigger = bytes[8]; x.RightTrigger = bytes[9];
        // The upstream Go and Rust versions disagree on Assistant/Capture names.
        // Keep raw bits until confirmed with the physical controller.
        report = new(x, (bytes[2] & 1) != 0, (bytes[2] & 2) != 0);
        return true;
    }

    private static short Axis(byte value, bool invert, int deadzone)
    {
        // Stadia has an 8-bit axis with a center at 128.
        int delta = value - 128;
        int result = delta < 0 ? delta * 256 : (int)Math.Round(delta * 32767.0 / 127);
        if (invert) result = Math.Clamp(-result, short.MinValue, short.MaxValue);
        int threshold = Math.Clamp(deadzone, 0, 30) * 32767 / 100;
        return (short)(Math.Abs(result) <= threshold ? 0 : result);
    }
}
