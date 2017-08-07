using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Security;
using Common.Structures;
using Common.Network;
using Server.Network;

namespace Server.Network
{
    public class Packets
    {
        public static readonly Packets Instance = new Packets();
        delegate void PacketAction(Connection Connection, PacketStream stream);
        Dictionary<ushort, PacketAction> PacketsDb;

        public Packets()
        {
            PacketsDb = new Dictionary<ushort, PacketAction>();

            PacketsDb.Add(0x0001, CS_Test);

        }

        private void CS_Test(Connection Connection, PacketStream stream)
        {
            string test = stream.ReadString();
        }

        public void Received(Connection Connection, PacketStream stream)
        {
            if (!PacketsDb.ContainsKey(stream.GetId()))
            {
                Console.WriteLine("Unknown packet Id: {0}", stream.GetId());
                return;
            }

            Task.Run(() => { PacketsDb[stream.GetId()].Invoke(Connection, stream); });
        }

        #region Server to Connection (SC)

        #endregion

        #region Connection to Server (CS)

        #endregion
    }
}
