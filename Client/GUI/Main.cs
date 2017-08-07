using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Network;
using Client.Network;

namespace Client.GUI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        internal void UpdateStatus(string text) { statusLbl.Text = text; }

        private void Main_Shown(object sender, EventArgs e)
        {
            UpdateStatus((ServerManager.Start("127.0.0.1", 1447)) ? "Connected" : "Connection Failed");
            Packets.Instance.CS_Test();
        }
    }
}
