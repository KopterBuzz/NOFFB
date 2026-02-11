using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NOFFBController
{
    public class FastUdpListener
    {
        private readonly Socket _socket;
        private readonly int _port;
        private const int SIO_UDP_CONNRESET = -1744830452;

        public FastUdpListener(int port, int receiveBufferSize = 2 * 1024 * 1024)
        {
            _port = port;

            // Create socket with optimal settings
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Disable ICMP port unreachable messages from crashing the socket
            _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

            // Critical performance settings
            _socket.ReceiveBufferSize = receiveBufferSize;  // Large OS buffer to prevent drops
            _socket.Blocking = false;  // Non-blocking for async
            _socket.DontFragment = true;

            // Remove this line - NoDelay is TCP-only
            // _socket.NoDelay = true;

            // Set socket options for minimal latency
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind to endpoint
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        // Zero-allocation receive using Memory<byte>
        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            return await _socket.ReceiveAsync(buffer, SocketFlags.None, ct);
        }

        // For scenarios where you need the remote endpoint
        public async ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            return await _socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEP, ct);
        }

        // Absolute fastest: synchronous non-blocking with span
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TryReceive(Span<byte> buffer)
        {
            try
            {
                return _socket.Receive(buffer, SocketFlags.None);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                return 0; // No data available
            }
        }

        public void Close()
        {
            _socket.Close();
            _socket.Dispose();
        }
    }


}
