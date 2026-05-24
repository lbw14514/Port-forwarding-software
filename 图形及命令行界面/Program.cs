using System;
using System.Windows.Forms;

namespace PortForwarder
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && Array.Exists(args, a => a.StartsWith("--")))
            {
                CommandLineMode.Run(args);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}