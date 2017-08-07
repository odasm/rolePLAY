using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Security;
using System.IO;

namespace Client.Functions
{
    public class OptionManager
    {
        private bool optExists { get { return File.Exists(string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "config.opt")); } }
    }
}
