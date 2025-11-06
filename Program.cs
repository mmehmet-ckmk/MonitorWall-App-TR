using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace MonitorWall.App
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var tr = new CultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = tr;
            Thread.CurrentThread.CurrentUICulture = tr;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UI.MainForm());
        }
    }
}
