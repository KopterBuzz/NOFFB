using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace NOFFBController
{
    public sealed class UdpChannelListener : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly Channel<string> _channel;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listenerTask;

        public UdpChannelListener(int port, int capacity = 10)
        {
            _udpClient = new UdpClient(port);

            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait, // block producer if full
                SingleWriter = true,
                SingleReader = false
            });
        }

        public ChannelReader<string> Reader => _channel.Reader;

        public void Start()
        {
            _listenerTask = Task.Run(ListenAsync);
        }

        private async Task ListenAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync(_cts.Token);
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    // Blocks (async) when buffer is full
                    await _channel.Writer.WriteAsync(message, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP error: {ex}");
            }
            finally
            {
                _channel.Writer.TryComplete();
            }
        }

        public async Task<string> ReadAsync(CancellationToken token = default)
        {
            // Blocks until a message is available
            return await _channel.Reader.ReadAsync(token);
        }

        public void Stop()
        {
            _cts.Cancel();
            _udpClient.Close();
        }

        public void Dispose()
        {
            Stop();
            _udpClient.Dispose();
            _cts.Dispose();
        }
    }
}
