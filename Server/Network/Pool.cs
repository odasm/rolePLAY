using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Structures;

namespace Server.Network
{
    public class Pool
    {
        public static Pool Instance = new Pool();
        List<Connection> Clients;

        public void Start() { Clients = new List<Connection>(); Console.WriteLine("[OK]"); }

        public void Register(Connection client)
        {
            if (Clients.Find(c=>c.ID == client.ID) == null)
            {
                Clients.Add(client);
                //Packets.Instance.SC_EncryptionCredentials(client);
            }
            else { Console.WriteLine("[Pool.Register()] Client ({0}) already exists in the pool!"); }
        }
    }
}
