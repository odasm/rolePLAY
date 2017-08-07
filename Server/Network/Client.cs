using System.Net;
using System.Net.Sockets;
using Common.Security;
using Common.Network;

namespace Server.Network
{
    public class Client
    {
        public string ID { get; set; }
        public int CharacterID { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public Socket Socket { get; set; }
        public byte[] Buffer;
        public PacketStream Data { get; set; }
        public int PacketSize { get; set; }
        public int Offset { get; set; }

        public Client(Socket socket)
        {
            ID = RandomID.Generate();
            IP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            Port = ((IPEndPoint)socket.RemoteEndPoint).Port;
            this.Socket = socket;
            this.Buffer = new byte[PacketStream.MaxBuffer];
            this.Data = new PacketStream();
        }
    }
}
