using System.Diagnostics;

namespace StadiaBridge;

internal enum RumblePattern { Both, Heavy, Light, Burst, Pulse }

internal sealed class RumbleState
{
    private int motors, gain = 100;
    private sealed record TestRequest(RumblePattern Pattern, long Started, long Until);
    private TestRequest? test;
    public void SetMotors(byte large, byte small) => Volatile.Write(ref motors, large | (small << 8));
    public void SetGain(int percentage) => Volatile.Write(ref gain, Math.Clamp(percentage, 0, 100));
    public void Test() => Test(RumblePattern.Both);
    public void Test(RumblePattern pattern) => Test(pattern, Stopwatch.GetTimestamp());
    internal void Test(RumblePattern pattern, long timestamp)
    {
        int duration = DurationMilliseconds(pattern);
        Volatile.Write(ref test, new TestRequest(pattern, timestamp, timestamp + Stopwatch.Frequency * duration / 1000));
    }
    public void CancelTest() => Volatile.Write(ref test, null);
    public RumblePattern? ActivePattern(long timestamp)
    {
        var request = Volatile.Read(ref test);
        return request is not null && timestamp >= request.Started && timestamp < request.Until ? request.Pattern : null;
    }
    public static int DurationMilliseconds(RumblePattern pattern) => pattern switch
    {
        RumblePattern.Both => 500,
        RumblePattern.Heavy or RumblePattern.Light => 650,
        RumblePattern.Burst => 660,
        RumblePattern.Pulse => 1200,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern))
    };
    public static string Name(RumblePattern pattern) => pattern switch
    {
        RumblePattern.Both => "Ambos motores", RumblePattern.Heavy => "Fuerte",
        RumblePattern.Light => "Suave", RumblePattern.Burst => "Ráfaga", RumblePattern.Pulse => "Pulso",
        _ => throw new ArgumentOutOfRangeException(nameof(pattern))
    };
    public int Current(long timestamp)
    {
        var request = Volatile.Read(ref test);
        int pair = Volatile.Read(ref motors);
        if (request is not null && timestamp >= request.Started && timestamp < request.Until)
        {
            double elapsed = (timestamp - request.Started) * 1000d / Stopwatch.Frequency;
            pair = request.Pattern switch
            {
                RumblePattern.Both => 0x8080,
                RumblePattern.Heavy => 0x00FF,
                RumblePattern.Light => 0xFF00,
                RumblePattern.Burst => elapsed % 220 < 120 ? 0xDCDC : 0,
                RumblePattern.Pulse => Pulse(elapsed),
                _ => 0
            };
        }
        int percent = Volatile.Read(ref gain);
        return ((pair & 255) * percent / 100) | (((pair >> 8) * percent / 100) << 8);
    }
    private static int Pulse(double elapsed)
    {
        int value = (int)Math.Round(100 * (1 - Math.Cos(2 * Math.PI * elapsed / 600)));
        return value | (value << 8);
    }
    public static byte[] Packet(int pair, int length)
    {
        if (length < 5 || length > 65535) throw new ArgumentOutOfRangeException(nameof(length));
        var packet = new byte[length];
        packet[0] = 5;
        packet[1] = packet[2] = (byte)pair;
        packet[3] = packet[4] = (byte)(pair >> 8);
        return packet;
    }
}

internal static class RumblePump
{
    // One writer owns the USB output stream. Native callbacks only update state.
    public static async Task RunAsync(Stream output, int length, RumbleState state, CancellationToken token)
    {
        int previous = -1;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int pair = state.Current(Stopwatch.GetTimestamp());
                if (pair != previous)
                {
                    await output.WriteAsync(RumbleState.Packet(pair, length), token);
                    await output.FlushAsync(token);
                    previous = pair;
                }
                await Task.Delay(16, token);
            }
        }
        finally
        {
            // Use a fresh, bounded token: the session token is already canceled.
            // A disconnected device cannot acknowledge the final stop command.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            try
            {
                await output.WriteAsync(RumbleState.Packet(0, length), cleanup.Token);
                await output.FlushAsync(cleanup.Token);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException) { }
        }
    }
}
