using System;
using System.Net;
using System.Net.Sockets;
using Common.Structures;
using Common.Network;

namespace Client.Network
{
    public static class ServerManager
    {
        private static bool started = false;

        internal static Connection _server;

        /// <summary>
        /// Starts the connection with server
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool Start(string ip, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Parse string IP Address
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ip, out ipAddress))
                {
                    //String.Format("Failed to parse Server IP ({0})", ip);
                    return false;
                }
                // Connect
                //socket.BeginConnect(new IPEndPoint(ipAddress, port), new AsyncCallback(ConnectCallback), socket);
                socket.Connect(new IPEndPoint(ipAddress, port));

                // Initializes a new instance of Server
                _server = new Connection(socket, ConnectionType.Server);

                // Starts to receive data
                socket.BeginReceive(_server.Buffer, 0, PacketStream.MaxBuffer, SocketFlags.None, new AsyncCallback(readCallback), null);

                started = true;
            }
            catch (Exception e)
            {
                Console.Write("Failed to connect to the Launcher Server!\nThe server may be down for a moment. Try again in a bit.");

                socket.Close();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when an entire packet is received
        /// </summary>
        /// <param name="packetStream"></param>
        private static void PacketReceived(PacketStream packetStream)
        {
            // Dumps packet data and process
            Packets.Instance.PacketReceived(packetStream);
        }

        /// <summary>
        /// Sends a packet to a server
        /// </summary>
        /// <param name="packet"></param>
        public static void Send(PacketStream packet)
        {
            // Completes the packet and retrieve it
            byte[] data = packet.Finalize();

            _server.Socket.BeginSend(
                data,
                0,
                data.Length,
                SocketFlags.None,
                new AsyncCallback(sendCallback),
                null
               );
        }

        #region Internal

        /// <summary>
        /// Triggered when it connects to Server
        /// </summary>
        /// <param name="ar"></param>
        private static void connectCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            if (!socket.Connected)
            {
                //"Could not stabilish a connection to Launcher Server.";
                return;
            }
            socket.EndConnect(ar);


        }

        /// <summary>
        /// Receives data and split them into packets
        /// </summary>
        /// <param name="ar"></param>
        private static void readCallback(IAsyncResult ar)
        {
            try
            {
                //// Retrieves the ammount of bytes received
                //int bytesRead = _server.ClSocket.EndReceive(ar);
                //if (bytesRead > 0)
                //{
                //    // The offset of the current buffer
                //    int curOffset = 0;
                //    // Bytes to use in the next write
                //    int bytesToRead = 0;

                //    byte[] decode = Encryption.DecryptBytes(ref _server.Buffer, Packets.Instance.keyIV.Key, Packets.Instance.keyIV.IV);
                //    do
                //    { // While there's data to read

                //        if (_server.PacketSize == 0)
                //        { // If we don't have the packet size yet

                //            if (_server.Offset + bytesRead > 3)
                //            { // If we can retrieve the packet size with the received data
                //              // If yes, we read remaining bytes until we get the packet size
                //                bytesToRead = (4 - _server.Offset);
                //                _server.Data.Write(decode, curOffset, bytesToRead);
                //                curOffset += bytesToRead;
                //                _server.Offset = bytesToRead;
                //                _server.PacketSize = BitConverter.ToInt32(_server.Data.ReadBytes(4, 0, true), 0);
                //            }
                //            else
                //            {
                //                // If not, we read everything.
                //                _server.Data.Write(decode, 0, bytesRead);
                //                _server.Offset += bytesRead;
                //                curOffset += bytesRead;
                //            }
                //        }
                //        else
                //        { // If we have packet size
                //          // How many bytes we need to complete this packet
                //            int needBytes = _server.PacketSize - _server.Offset;

                //            // If there's enough bytes to complete this packet
                //            if (needBytes <= (bytesRead - curOffset))
                //            {
                //                _server.Data.Write(decode, curOffset, needBytes);
                //                curOffset += needBytes;
                //                // Packet is done, send to server to be parsed
                //                // and continue.
                //                PacketReceived(_server.Data);
                //                // Do needed clean up to start a new packet
                //                _server.Data = new PacketStream();
                //                _server.PacketSize = 0;
                //                _server.Offset = 0;
                //            }
                //            else
                //            {
                //                bytesToRead = (bytesRead - curOffset);
                //                _server.Data.Write(decode, curOffset, bytesToRead);
                //                _server.Offset += bytesToRead;
                //                curOffset += bytesToRead;
                //            }
                //        }
                //    } while (bytesRead - 1 > curOffset);

                //    // Starts to receive more data
                //    _server.ClSocket.BeginReceive(
                //        _server.Buffer,
                //        0,
                //        PacketStream.MaxBuffer,
                //        SocketFlags.None,
                //        new AsyncCallback(readCallback),
                //        null
                //    );
                //}
                //else
                //{
                //    //this.ErrorMessage = "Connection to server lost.";
                //    _server.ClSocket.Close();
                //    return;
                //}
            }
            catch (SocketException e)
            {
                // 10054 : Socket closed, not an error
                if (!(e.ErrorCode == 10054))
                    //this.ErrorMessage = e.Message;

                //this.ErrorMessage += "Connection to server lost.";
                _server.Socket.Close();
            }
            catch (Exception e)
            {
                //this.ErrorMessage = e.Message;
                //this.ErrorMessage += "Connection to server lost.";
                _server.Socket.Close();
            }
        }

        internal static void Close()
        {
            if (started)
                _server.Socket.Close();
        }

        /// <summary>
        /// Sends a packet to a game-server
        /// </summary>
        /// <param name="ar"></param>
        private static void sendCallback(IAsyncResult ar)
        {
            try
            {
                // Complete sending the data to the remote device.
                int bytesSent = _server.Socket.EndSend(ar);
            }
            catch (Exception)
            {
                //ErrorMessage = "Failed to send data to server.";
            }
        }
        #endregion
    }

}