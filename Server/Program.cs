using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Network;
using Common.Network;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Creating Connection Pool...");
            Pool.Instance.Start();
            Console.Write("Starting Connection Manager...");
            ClientManager.Instance.Start();
            Console.ReadLine();
        }
    }
}
