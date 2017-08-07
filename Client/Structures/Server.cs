using Client.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common.Network;

namespace Client.Structures
{
    public class Server
    {
        public Socket ClSocket { get; set; }
        public byte[] Buffer;
        public PacketStream Data { get; set; }
        public int PacketSize { get; set; }
        public int Offset { get; set; }

        public Server(Socket socket)
        {
            this.ClSocket = socket;
            this.Buffer = new byte[PacketStream.MaxBuffer];
            this.Data = new PacketStream();
        }
    }
}
