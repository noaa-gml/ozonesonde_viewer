using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Ozonesonde_Viewer_2019
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
            Application.Run(new MainForm());
        }
    }

    namespace ExtensionMethods
    {
        public static class ControlExtensions
        {
            [DllImport("user32.dll")]
            public static extern bool LockWindowUpdate(IntPtr hWndLock);

            public static void Suspend(this Control control)
            {
                LockWindowUpdate(control.Handle);
            }

            public static void Resume(this Control control)
            {
                LockWindowUpdate(IntPtr.Zero);
            }
        }
    }
}
