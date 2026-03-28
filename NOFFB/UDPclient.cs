using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NOFFB
{
    internal class UDPClient
    {
        int port;
        UdpClient udp;


        public UDPClient(int port )
        {
            udp = new UdpClient();
            this.port = port;
        }

        public async void SendData(string data_)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data_);
            await udp?.SendAsync(bytes, bytes.Length, "127.0.0.1", port);
            Plugin.Logger?.LogDebug($"SENDER SIDE: {data_}");
        }
    }
}
