using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

//test:
using System.Runtime.InteropServices; // for the callback Marshaling
using AIOUSBNet;  // the namespace exposes the AIOUSB Class interface 

namespace Sample1
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
