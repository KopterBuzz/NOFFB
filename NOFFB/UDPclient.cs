using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NOFFB
{
    internal class UDPClient
    {
        UdpClient udp;


        public UDPClient()
        {
            udp = new UdpClient();
        }

        public async void SendData(string data_)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data_);
            await udp?.SendAsync(bytes, bytes.Length, "127.0.0.1", 5001);
            //Plugin.Logger?.LogDebug(data_);
        }
    }
}
