using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Common.Structures;
using Client.Network;

namespace Common.Network
{
    public class Packets
    {
        public static readonly Packets Instance = new Packets();

        private delegate void PacketAction(PacketStream stream);

        private Dictionary<ushort, PacketAction> PacketsDb;

        public Packets()
        {
            // Loads PacketsDb
            PacketsDb = new Dictionary<ushort, PacketAction>();

            #region Packets

            PacketsDb.Add(0x0001, SC_ReceiveKeyIV);

            #endregion
        }

        /// <summary>
        /// Called whenever a packet is received from the Server
        /// </summary>
        /// <param name="stream"></param>
        public void PacketReceived(PacketStream stream)
        {
            // Is it a known packet ID?
            if (!PacketsDb.ContainsKey(stream.GetId()))
            {
                Console.WriteLine("Unknown packet Id: {0}", stream.GetId());
                return;
            }

            // Calls this packet parsing function
            Task.Run(() => { PacketsDb[stream.GetId()].Invoke(stream); });
        }

        #region Client to Server (CS)

        internal void CS_Test()
        {
            PacketStream stream = new PacketStream(0x0001);
            stream.WriteString("This is a super secret message");
            ServerManager.Send(stream);
 }

        internal void SC_AuthenticateUser(string username, string password, string fingerprint) { }

        #endregion

        #region Server to Client (CS)

        private void SC_ReceiveKeyIV(PacketStream stream)
        {
            int keyLen = stream.ReadInt32();
            byte[] key = stream.ReadBytes(keyLen);
            int ivLen = stream.ReadInt32();
            byte[] iv = stream.ReadBytes(ivLen);

            System.Windows.Forms.MessageBox.Show(string.Format("Key Length: {0}\nIV Length: {1}", keyLen, ivLen));
        }

        #endregion
    }
}
