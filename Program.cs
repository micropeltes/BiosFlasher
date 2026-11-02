using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BiosFlasher
{
    internal static class Program
    {
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// Setelah AttachConsole, stream Console lama masih menunjuk ke null.
        /// Buka ulang stdout/stderr supaya output CLI benar-benar terlihat.
        /// </summary>
        private static void RedirectConsole()
        {
            bool redirected = false;
            try { redirected = Console.IsOutputRedirected; }
            catch { }

            // kalau output di-pipe/redirect (mis. '> file'), jangan ambil console:
            // biar tulisan masuk ke file; kalau tidak, sambung ke console cmd.
            if (!redirected)
            {
                AttachConsole(AttachParentProcess);
            }

            try
            {
                StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
                output.AutoFlush = true;
                Console.SetOut(output);

                StreamWriter error = new StreamWriter(Console.OpenStandardError());
                error.AutoFlush = true;
                Console.SetError(error);
            }
            catch
            {
                // kalau gagal, biarkan Console default
            }
        }

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                string first = args[0].ToLowerInvariant();

                if (first == "--cli" || first == "-c" || first == "/cli")
                {
                    RedirectConsole();   // supaya Console.Write terlihat di cmd
                    string[] rest = new string[args.Length - 1];
                    Array.Copy(args, 1, rest, 0, rest.Length);
                    return Cli.Run(rest);
                }

                if (first == "--help" || first == "-h" || first == "/?")
                {
                    RedirectConsole();
                    Console.WriteLine("BiosFlasher.exe              -> mode GUI");
                    Console.WriteLine("BiosFlasher.exe --cli <cmd>  -> mode command line");
                    Console.WriteLine();
                    Cli.Run(new string[0]);
                    return 0;
                }
            }

            // Sengaja TANPA Application.EnableVisualStyles():
            // kontrol digambar gaya klasik (Win9x/2000) supaya tampilannya sederhana.
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }
}

