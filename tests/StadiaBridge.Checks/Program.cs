using StadiaBridge;
using System.Runtime.InteropServices;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
byte[] Neutral() => [3, 8, 0, 0, 128, 128, 128, 128, 0, 0];
StadiaReport Parse(byte[] bytes, int deadzone = 0) { Check(StadiaReport.TryParse(bytes, deadzone, out var r), "valid report"); return r; }
Check(Marshal.SizeOf<XboxReport>() == 12, "native XUSB size");
Check(!StadiaReport.TryParse([], 0, out _), "empty");
for (int i = 0; i < 10; i++) Check(!StadiaReport.TryParse(new byte[i], 0, out _), "truncated");
var neutral = Parse(Neutral()).Xbox;
Check(neutral.Buttons == 0 && neutral.LX == 0 && neutral.LY == 0 && neutral.RX == 0 && neutral.RY == 0, "neutral");
int[] hats = [1, 9, 8, 10, 2, 6, 4, 5, 0];
for (int i = 0; i < 9; i++) { var b = Neutral(); b[1] = (byte)i; Check(Parse(b).Xbox.Buttons == hats[i], "hat"); }
int[] masks = [0x1000, 0x2000, 0x4000, 0x8000, 0x100, 0x200, 0x40];
for (int i = 0; i < 7; i++) { var b = Neutral(); b[3] = (byte)(0x40 >> i); Check(Parse(b).Xbox.Buttons == masks[i], "face button"); }
int[] other = [0x80, 0x20, 0x10, 0x400];
for (int i = 0; i < 4; i++) { var b = Neutral(); b[2] = (byte)(0x80 >> i); Check(Parse(b).Xbox.Buttons == other[i], "system button"); }
var extreme = Neutral(); extreme[4] = 0; extreme[5] = 0; extreme[6] = 255; extreme[7] = 255; extreme[8] = 128; extreme[9] = 255;
var x = Parse(extreme).Xbox;
Check(x.LX == -32768 && x.LY == 32767 && x.RX == 32767 && x.RY == -32767, "axis bounds and inversion");
Check(x.LeftTrigger == 128 && x.RightTrigger == 255, "analog triggers");
var drift = Neutral(); drift[4] = 130; Check(Parse(drift, 5).Xbox.LX == 0, "deadzone");
var prefixed = new byte[11]; Neutral().CopyTo(prefixed, 1); Check(Parse(prefixed).Xbox.Buttons == 0, "zero prefix");
for (int i = 0; i < 256; i++) { var b = Neutral(); b[4] = (byte)i; var copy = (byte[])b.Clone(); var r = Parse(b); Check(b.SequenceEqual(copy), "immutable input"); if (i > 0) { b[4]--; Check(Parse(b).Xbox.LX <= r.Xbox.LX, "monotonic axis"); } }
var extras = Neutral(); extras[2] = 3; var extraReport = Parse(extras); Check(extraReport.Extra1 && extraReport.Extra2 && extraReport.Xbox.Buttons == 0, "extra bits isolated");
var invalid = Neutral(); invalid[1] = 9; Check(!StadiaReport.TryParse(invalid, 0, out _), "invalid hat");
var vibration = new RumbleState();
vibration.SetMotors(255, 64);
Check(vibration.Current(long.MaxValue) == 0x40FF, "independent motor channels");
vibration.SetGain(50);
Check(vibration.Current(long.MaxValue) == 0x207F, "gain scaling");
vibration.SetGain(-1); Check(vibration.Current(long.MaxValue) == 0, "mute and lower bound");
vibration.SetGain(150); Check(vibration.Current(long.MaxValue) == 0x40FF, "upper gain bound");
vibration.Test();
Check(vibration.Current(System.Diagnostics.Stopwatch.GetTimestamp()) == 0x8080, "test overrides game");
Check(vibration.Current(long.MaxValue) == 0x40FF, "game resumes after bounded test");
long patternStart = System.Diagnostics.Stopwatch.Frequency * 10;
long At(int ms) => patternStart + System.Diagnostics.Stopwatch.Frequency * ms / 1000;
var patterns = new RumbleState(); patterns.SetMotors(20, 40);
patterns.Test(RumblePattern.Heavy, patternStart);
Check(patterns.Current(At(0)) == 255 && patterns.Current(At(649)) == 255, "heavy only drives the large motor");
Check(patterns.Current(At(650)) == 0x2814, "heavy ends and restores game");
patterns.Test(RumblePattern.Light, patternStart);
Check(patterns.Current(At(100)) == 0xFF00, "light only drives the small motor");
patterns.Test(RumblePattern.Burst, patternStart);
Check(patterns.Current(At(20)) == 0xDCDC, "burst first hit");
Check(patterns.Current(At(150)) == 0, "burst first quiet interval");
Check(patterns.Current(At(230)) == 0xDCDC, "burst second hit");
Check(patterns.Current(At(370)) == 0, "burst second quiet interval");
Check(patterns.Current(At(450)) == 0xDCDC, "burst third hit");
Check(patterns.Current(At(600)) == 0, "burst final quiet interval");
Check(patterns.Current(At(660)) == 0x2814, "burst restores game at end");
patterns.Test(RumblePattern.Pulse, patternStart);
Check(patterns.Current(At(0)) == 0, "pulse starts quietly");
Check(patterns.Current(At(150)) == 0x6464, "pulse rises smoothly");
Check(patterns.Current(At(300)) == 0xC8C8, "pulse first peak");
Check(patterns.Current(At(450)) == 0x6464, "pulse falls smoothly");
Check(patterns.Current(At(600)) == 0, "pulse quiet between waves");
Check(patterns.Current(At(900)) == 0xC8C8, "pulse second peak");
patterns.SetMotors(10, 30);
Check(patterns.Current(At(1200)) == 0x1E0A, "pulse restores the latest game request");
patterns.SetGain(50);
Check(patterns.Current(At(300)) == 0x6464, "gain applies during pulse");
patterns.CancelTest();
Check(patterns.Current(At(300)) == 0x0F05, "cancel immediately restores game with gain");
Check(patterns.ActivePattern(At(300)) is null, "cancel clears active selection");
foreach (var pattern in Enum.GetValues<RumblePattern>())
{
    patterns.SetGain(0); patterns.Test(pattern, patternStart);
    Check(patterns.Current(At(50)) == 0, "every pattern respects mute");
    Check(patterns.ActivePattern(At(50)) == pattern, "active pattern is reported");
    Check(patterns.ActivePattern(At(RumbleState.DurationMilliseconds(pattern))) is null, "pattern selection expires");
}
patterns.SetGain(100); patterns.Test(RumblePattern.Heavy,patternStart); patterns.Test(RumblePattern.Light,patternStart);
Check(patterns.Current(At(100)) == 0xFF00, "new pattern replaces previous test atomically");
Check(RumbleState.Packet(0x40FF, 8).SequenceEqual(new byte[] { 5, 255, 255, 64, 64, 0, 0, 0 }), "HID motor encoding and padding");
Check(RumbleState.Packet(0, 5).SequenceEqual(new byte[] { 5, 0, 0, 0, 0 }), "motor stop encoding");
using (var recording = new RecordingOutput())
using (var cancel = new CancellationTokenSource())
{
    var state = new RumbleState(); state.SetMotors(200, 100);
    var pumping = RumblePump.RunAsync(recording, 5, state, cancel.Token);
    await recording.Nonzero.Task.WaitAsync(TimeSpan.FromSeconds(2));
    state.SetGain(0);
    await recording.Zero.Task.WaitAsync(TimeSpan.FromSeconds(2));
    cancel.Cancel();
    try { await pumping; } catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
    Check(recording.Packets[0].SequenceEqual(new byte[] { 5, 200, 200, 100, 100 }), "USB pump forwards game motors");
    Check(recording.Packets[^1].SequenceEqual(new byte[] { 5, 0, 0, 0, 0 }), "USB pump stops on cancellation");
    Check(recording.Packets.Count >= 3, "mute applies while connected plus final stop");
}
using (var failing = new RecordingOutput { FailFirst = true })
{
    bool failed = false;
    try { await RumblePump.RunAsync(failing, 5, new RumbleState(), CancellationToken.None); }
    catch (IOException) { failed = true; }
    Check(failed && failing.Packets.Count == 1 && failing.Packets[0][1] == 0, "write failure attempts final stop and propagates");
}
Console.WriteLine($"PASS: {checks} checks");

sealed class RecordingOutput : MemoryStream
{
    public List<byte[]> Packets { get; } = new();
    public TaskCompletionSource Nonzero { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Zero { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool FailFirst { get; init; }
    private bool attempted;
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (FailFirst && !attempted) { attempted = true; throw new IOException("simulated USB failure"); }
        var bytes = buffer.ToArray(); Packets.Add(bytes);
        if (bytes[1] != 0 || bytes[3] != 0) Nonzero.TrySetResult(); else Zero.TrySetResult();
        return ValueTask.CompletedTask;
    }
}
