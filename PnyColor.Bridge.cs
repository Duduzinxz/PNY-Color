using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// Local-only transport. Receiving a frame never calls the GPU or changes a color.
// Wire format: eight bytes [P, N, Y, 1, red, green, blue, brightnessPercent].
sealed class SignalBridge : IDisposable
{
    public const int Port = 39841;
    readonly object gate = new object();
    readonly Socket socket;
    readonly Thread receiver;
    readonly int boundPort;
    ColorFrame latest;
    long latestTimestamp, latestSequence;
    bool hasFrame;
    int disposed;

    public SignalBridge() : this(Port) { }

    // Port zero is reserved for the hardware-free self-test, not normal use.
    internal SignalBridge(int port)
    {
        if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException("port");
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            socket.ExclusiveAddressUse = true;
            socket.ReceiveBufferSize = 16384;
            socket.ReceiveTimeout = 500;
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            boundPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            receiver = new Thread(ReceiveLoop);
            receiver.IsBackground = true;
            receiver.Name = "PNY Color local SignalRGB receiver";
            receiver.Start();
        }
        catch
        {
            socket.Close();
            throw;
        }
    }

    bool IsDisposed { get { return Interlocked.CompareExchange(ref disposed, 0, 0) != 0; } }

    void ReceiveLoop()
    {
        // A ninth byte makes an oversized datagram distinguishable from a valid
        // eight-byte frame. Larger datagrams are discarded by the socket API.
        byte[] packet = new byte[9];
        EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        while (!IsDisposed)
        {
            try
            {
                int count = socket.ReceiveFrom(packet, 0, packet.Length, SocketFlags.None, ref sender);
                IPEndPoint endpoint = sender as IPEndPoint;
                ColorFrame frame;
                if (endpoint == null || !IPAddress.IsLoopback(endpoint.Address) ||
                    !TryDecode(packet, count, out frame)) continue;
                lock (gate)
                {
                    if (IsDisposed) return;
                    latest = frame;
                    latestTimestamp = Stopwatch.GetTimestamp();
                    latestSequence = unchecked(latestSequence + 1);
                    hasFrame = true;
                }
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException error)
            {
                if (IsDisposed) return;
                if (error.SocketErrorCode == SocketError.TimedOut ||
                    error.SocketErrorCode == SocketError.WouldBlock ||
                    error.SocketErrorCode == SocketError.MessageSize ||
                    error.SocketErrorCode == SocketError.ConnectionReset) continue;
                // An unexpected permanent socket error must not spin forever.
                return;
            }
        }
    }

    static bool TryDecode(byte[] packet, int count, out ColorFrame frame)
    {
        frame = new ColorFrame();
        if (packet == null || count != 8 || packet.Length < count ||
            packet[0] != 80 || packet[1] != 78 || packet[2] != 89 || packet[3] != 49 ||
            packet[7] > 100) return false;
        frame.Color = Color.FromArgb(packet[4], packet[5], packet[6]);
        frame.White = 0;
        frame.Brightness = packet[7];
        return true;
    }

    public bool TryLatest(out ColorFrame frame, out long sequence, out double ageSeconds)
    {
        lock (gate)
        {
            if (!hasFrame || IsDisposed)
            {
                frame = new ColorFrame();
                sequence = 0;
                ageSeconds = double.PositiveInfinity;
                return false;
            }
            frame = latest;
            sequence = latestSequence;
            ageSeconds = Math.Max(0.0, (Stopwatch.GetTimestamp() - latestTimestamp) / (double)Stopwatch.Frequency);
            return true;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        socket.Close();
        // Never hold gate while waiting, nor join the current thread.
        if (receiver != null && receiver != Thread.CurrentThread) receiver.Join(1000);
    }

    static void Assert(bool passed, string name)
    {
        if (!passed) throw new Exception("SignalBridge self-test failed: " + name);
    }

    public static void SelfTest()
    {
        ColorFrame frame;
        byte[] valid = new byte[] { 80, 78, 89, 49, 255, 128, 0, 100 };
        Assert(TryDecode(valid, 8, out frame), "valid frame");
        Assert(frame.Color.R == 255 && frame.Color.G == 128 && frame.Color.B == 0 &&
            frame.White == 0 && frame.Brightness == 100, "decoded channels");
        Assert(!TryDecode(null, 8, out frame), "null packet");
        Assert(!TryDecode(new byte[7], 8, out frame), "short backing buffer");
        Assert(!TryDecode(valid, 7, out frame), "short datagram");
        Assert(!TryDecode(new byte[9], 9, out frame), "long datagram");
        for (int index = 0; index < 4; index++)
        {
            byte[] bad = (byte[])valid.Clone();
            bad[index] ^= 1;
            Assert(!TryDecode(bad, 8, out frame), "bad magic " + index);
        }
        foreach (byte badBrightness in new byte[] { 101, 255 })
        {
            byte[] bad = (byte[])valid.Clone(); bad[7] = badBrightness;
            Assert(!TryDecode(bad, 8, out frame), "invalid brightness");
        }
        byte[] black = new byte[] { 80, 78, 89, 49, 0, 0, 0, 0 };
        Assert(TryDecode(black, 8, out frame) && frame.Brightness == 0, "zero brightness");

        // Ephemeral loopback port: no dependency on port 39841 or GPU hardware.
        SignalBridge bridge = new SignalBridge(0);
        try
        {
            long sequence; double age;
            Assert(!bridge.TryLatest(out frame, out sequence, out age), "initially empty");
            bool rejectedDuplicateBind = false;
            try { using (SignalBridge duplicate = new SignalBridge(bridge.boundPort)) { } }
            catch (SocketException) { rejectedDuplicateBind = true; }
            Assert(rejectedDuplicateBind, "exclusive binding");
            using (Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint destination = new IPEndPoint(IPAddress.Loopback, bridge.boundPort);
                sender.SendTo(valid, destination);
                Stopwatch deadline = Stopwatch.StartNew();
                while (!bridge.TryLatest(out frame, out sequence, out age) && deadline.ElapsedMilliseconds < 2000) Thread.Sleep(5);
                Assert(bridge.TryLatest(out frame, out sequence, out age) && sequence == 1 &&
                    frame.Color.R == 255 && frame.Color.G == 128 && frame.Brightness == 100 && age >= 0, "loopback receive");
                double firstAge = age;
                Thread.Sleep(15);
                Assert(bridge.TryLatest(out frame, out sequence, out age) && age >= firstAge, "monotonic age");
                byte[] malformed = (byte[])valid.Clone(); malformed[7] = 101;
                sender.SendTo(malformed, destination);
                sender.SendTo(new byte[9], destination);
                sender.SendTo(new byte[4096], destination);
                // Same sender/socket preserves order in this local self-test.
                sender.SendTo(black, destination);
                deadline.Restart();
                while ((!bridge.TryLatest(out frame, out sequence, out age) || sequence < 2) && deadline.ElapsedMilliseconds < 2000) Thread.Sleep(5);
                Assert(bridge.TryLatest(out frame, out sequence, out age) && sequence == 2 &&
                    frame.Color.R == 0 && frame.Color.G == 0 && frame.Color.B == 0 && frame.Brightness == 0, "invalid packets ignored; latest valid frame retained");
            }
        }
        finally { bridge.Dispose(); }
        bridge.Dispose();
        long finalSequence; double finalAge;
        Assert(!bridge.TryLatest(out frame, out finalSequence, out finalAge), "disposed receiver unavailable");
        Console.WriteLine("PASS: SignalBridge parsing, exclusive loopback receive, malformed packets, latest-frame state, monotonic age and disposal; no GPU writes.");
    }
}
