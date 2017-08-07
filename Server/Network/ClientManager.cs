using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Common.Structures;
using Common.Network;
using Common.Security;

namespace Server.Network
{
    public class ClientManager
    {
        private XRC4Cipher rc4Cipher;

        public static readonly ClientManager Instance = new ClientManager();

        internal string ip = "127.0.0.1";
        internal int port = 1447;

        /// <summary>
        /// Initializes the client listener
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(this.ip, out ip))
                {
                    Console.WriteLine("Failed to parse Server IP ({0})", ip);
                    return false;
                }
                listener.Bind(new IPEndPoint(ip, port));
                listener.Listen(100);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("At ClientManager.Start()");

                listener.Close();
                return false;
            }

            Console.WriteLine("[OK]\nNow listening for connections @ {0} : {1}", this.ip, this.port);
            return true;
        }

        /// <summary>
		/// Called when an entire packet is received
		/// </summary>
		/// <param name="client">the client</param>
		/// <param name="packetStream">the packet</param>
		private void PacketReceived(Connection client, PacketStream packetStream) { Packets.Instance.Received(client, packetStream); }

        /// <summary>
        /// Sends a packet to a client
        /// </summary>
        /// <param name="target">the client which will receive the packet</param>
        /// <param name="packet">packet data</param>
        public void Send(Connection target, PacketStream packet)
        {
            byte[] data = packet.Finalize();
            target.Socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), target);
        }

        #region Internal
        /// <summary>
        /// Triggered when a client tries to connect
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket socket = listener.EndAccept(ar);

            // Starts to accept another connection
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

            Connection client = new Connection(socket, ConnectionType.Client);

            Console.WriteLine("Client [{0}] connected from: {1} [{2}]", client.ID, client.IP, client.Port);

            Pool.Instance.Register(client);

            socket.BeginReceive(
                client.Buffer, 0, PacketStream.MaxBuffer, SocketFlags.None,
                new AsyncCallback(ReadCallback), client
            );
        }

        /// <summary>
        /// Receives data and split them into packets
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            KeyIV keyIV = new KeyIV();

            Connection client = (Connection)ar.AsyncState;

            if (client.Socket.Connected)
            {
                try
                {
                    int totalBytes = client.Socket.EndReceive(ar);

                    // If the stream actually has bytes in it
                    if (totalBytes > 0)
                    {
                        int curOffset = 0;
                        int bytesToRead = 0;
                        int headerLen = 0;

                        // If the packet size hasn't been defined in the client yet
                        if (client.PacketSize == 0)
                        {
                            // Read the encrypted header length
                            headerLen = BitConverter.ToInt32(client.Buffer.Take(4).ToArray(), 0);

                            // Read the encrypted header
                            byte[] encHeader = client.Buffer.Skip(4).Take(headerLen).ToArray();

                            // Generate a new dateTime key for encrypting the packet header
                            string dtKey = DateTime.UtcNow.ToString("yyyyMMddHH");
                            rc4Cipher = new XRC4Cipher(dtKey);

                            // Decode the header using the dtKey + rc4Cipher
                            byte[] decodedHeader = rc4Cipher.DoCipher(ref encHeader, encHeader.Length);
                            headerLen = decodedHeader.Length;

                            // Write decoded header info to client.Data
                            client.Data.Write(decodedHeader, curOffset, decodedHeader.Length);

                            // If there is more than just the header info in this stream
                            if (client.Offset + totalBytes > 4 + encHeader.Length)
                            {
                                // Define the packetSize by reading the size bytes from the client.Data buffer
                                client.PacketSize = BitConverter.ToInt32(client.Data.ReadBytes(4, 0, true), 0);

                                // TODO: Read my from client.Data?
                                // Define the keyIV used for decoding the encrypted data
                                int keyLen = client.Data.ReadInt16(6, true);
                                keyIV.Key = client.Data.ReadBytes(keyLen, 10, true);

                                int ivLen = client.Data.ReadInt16(12 + keyIV.Key.Length, true);
                                keyIV.IV = client.Data.ReadBytes(ivLen, 14 + keyIV.Key.Length, true);
                            }
                        }

                        // Set the amount of bytes to read by subtracting the current client offset from the decodedHeader.Length
                        bytesToRead = (totalBytes - (4 + headerLen));

                        // Decode encoded information still in the buffer
                        byte[] decodedArray = Encryption.DecryptBytes(client.Buffer.Skip(4 + headerLen).Take(bytesToRead).ToArray(), keyIV.Key, keyIV.IV);

                        // Write decoded information to client.Data
                        client.Data.Write(decodedArray, 0, decodedArray.Length);

                        // Trigger the action associated to this packet
                        PacketReceived(client, client.Data);

                        client.Data = new PacketStream();
                        client.PacketSize = 0;
                        client.Offset = 0;

                        client.Socket.BeginReceive(
                            client.Buffer, 0, PacketStream.MaxBuffer, SocketFlags.None,
                            new AsyncCallback(ReadCallback), client
                        );
                    }
                    else
                    {
                        client.Socket.Close();
                        return;
                    }
                }
                catch (Exception e)
                {
                    client.Socket.Close();
                    Console.WriteLine("Client [{0}] disconected! (with errors)\n\t-Errors!\n\t-{1}.", client.ID, e.Message);
                }
            }
        }

        /// <summary>
        /// Sends a packet to a client
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Connection client = (Connection)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.Socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send packet to client.\nError: {0}", ex.Message);
            }
        }
        #endregion

    }
}