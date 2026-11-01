using System;
using System.Windows.Forms;

namespace BiosFlasher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Sengaja TANPA Application.EnableVisualStyles():
            // kontrol digambar gaya klasik (Win9x/2000) supaya tampilannya sederhana.
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
