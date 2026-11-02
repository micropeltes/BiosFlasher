using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace BiosFlasher
{
    /// <summary>Mode command line: BiosFlasher.exe --cli &lt;perintah&gt; [opsi]</summary>
    public static class Cli
    {
        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
            {
                Usage();
                return args.Length == 0 ? 2 : 0;
            }

            string command = args[0].ToLowerInvariant();
            string port = null;
            string file = null;
            string addr = "0";
            bool yes = false;
            bool noVerify = false;
            int khz = 0;

            for (int i = 1; i < args.Length; i++)
            {
                string a = args[i];
                switch (a)
                {
                    case "-p":
                    case "--port":
                        if (i + 1 < args.Length) port = args[++i];
                        break;
                    case "-f":
                    case "--file":
                        if (i + 1 < args.Length) file = args[++i];
                        break;
                    case "-a":
                    case "--addr":
                        if (i + 1 < args.Length) addr = args[++i];
                        break;
                    case "-y":
                    case "--yes":
                        yes = true;
                        break;
                    case "--no-verify":
                        noVerify = true;
                        break;
                    case "--freq":
                        if (i + 1 < args.Length) int.TryParse(args[++i], out khz);
                        break;
                    default:
                        if (a.StartsWith("-")) { Error("opsi tidak dikenal: " + a); return 2; }
                        if (file == null) file = a;
                        break;
                }
            }

            uint startAddr;
            if (!TryParseUInt(addr, out startAddr)) { Error("alamat tidak valid: " + addr); return 2; }

            port = port ?? PortScanner.FindEsp32() ?? SinglePort();
            if (string.IsNullOrEmpty(port)) { Error("port serial tidak ditemukan, pakai -p COMx"); return 2; }

            using (FlasherDevice dev = new FlasherDevice())
            {
                try
                {
                    Info("port " + port);
                    dev.Open(port);
                }
                catch (Exception ex)
                {
                    Error("gagal membuka " + port + ": " + ex.Message);
                    return 1;
                }

                try
                {
                    switch (command)
                    {
                        case "info":
                            foreach (string ln in dev.Command("info", 30000)) Info(ln);
                            return 0;

                        case "status":
                            foreach (string ln in dev.Command("status", 20000)) Info(ln);
                            return 0;

                        case "unprotect":
                            Info(dev.Unprotect(Info));
                            return 0;

                        case "erase":
                            if (!yes) { Error("erase menghapus seluruh chip - tambahkan --yes"); return 2; }
                            Info(dev.ChipErase(Info));
                            return 0;

                        case "freq":
                            if (khz <= 0) { Error("butuh nilai kHz"); return 2; }
                            Info(dev.SetFrequency(khz));
                            return 0;

                        case "backup":
                        case "read":
                            return DoBackup(dev, file, startAddr);

                        case "verify":
                            return DoVerify(dev, file, startAddr);

                        case "write":
                            return DoWrite(dev, file, startAddr, noVerify);

                        default:
                            Error("perintah tidak dikenal: " + command);
                            Usage();
                            return 2;
                    }
                }
                catch (DeviceException ex)
                {
                    Error(ex.Message);
                    return 1;
                }
                catch (Exception ex)
                {
                    Error(ex.GetType().Name + ": " + ex.Message);
                    return 1;
                }
            }
        }

        /* ------------------------------------------------------------------ */

        private static int DoWrite(FlasherDevice dev, string file, uint addr, bool noVerify)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                Error("butuh file .bin yang ada");
                return 2;
            }

            byte[] data = File.ReadAllBytes(file);
            if (data.Length == 0) { Error("file kosong"); return 2; }
            Info("file " + file + " (" + data.Length + " byte)");

            dev.Unprotect(Info);

            int sectors = (data.Length + 4095) / 4096;
            Info("hapus " + sectors + " sector...");
            for (int s = 0; s < sectors; s += 64)
            {
                int n = Math.Min(64, sectors - s);
                dev.Erase(addr + (uint)(s * 4096), n, false, null);
                Console.Write("\rhapus {0}/{1}", s + n, sectors);
            }
            Console.WriteLine();

            Info("tulis data...");
            bool binary = dev.TryWriteFlashBinary(addr, data,
                (d, t) => Console.Write("\rtulis {0:0.0}%", 100.0 * d / t),
                CancellationToken.None, null);
            Console.WriteLine();
            if (!binary)
            {
                Info("firmware lama - pakai mode hex");
                dev.WriteFlash(addr, data,
                    (d, t) => Console.Write("\rtulis {0:0.0}%", 100.0 * d / t),
                    CancellationToken.None, null);
                Console.WriteLine();
            }

            if (noVerify)
            {
                Info("verifikasi dilewati (--no-verify)");
                return 0;
            }

            uint fileCrc = Crc32.Compute(data);
            uint chipCrc;
            if (dev.TryReadCrc(addr, (uint)data.Length, out chipCrc, null))
            {
                if (chipCrc != fileCrc)
                {
                    Error(string.Format(CultureInfo.InvariantCulture,
                        "CRC32 tidak cocok: file {0:X8}, chip {1:X8}", fileCrc, chipCrc));
                    return 1;
                }
                Info(string.Format(CultureInfo.InvariantCulture, "CRC32 cocok ({0:X8})", fileCrc));
                return 0;
            }

            Info("firmware lama - verifikasi byte-per-byte");
            dev.VerifyFlash(addr, data, null, CancellationToken.None, null);
            Info("VERIFY OK");
            return 0;
        }

        private static int DoVerify(FlasherDevice dev, string file, uint addr)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                Error("butuh file .bin yang ada");
                return 2;
            }
            byte[] data = File.ReadAllBytes(file);

            uint fileCrc = Crc32.Compute(data);
            uint chipCrc;
            if (dev.TryReadCrc(addr, (uint)data.Length, out chipCrc, null))
            {
                if (chipCrc != fileCrc)
                {
                    Error(string.Format(CultureInfo.InvariantCulture,
                        "CRC32 tidak cocok: file {0:X8}, chip {1:X8}", fileCrc, chipCrc));
                    return 1;
                }
                Info(string.Format(CultureInfo.InvariantCulture, "CRC32 cocok ({0:X8})", fileCrc));
                return 0;
            }

            dev.VerifyFlash(addr, data, null, CancellationToken.None, null);
            Info("VERIFY OK");
            return 0;
        }

        private static int DoBackup(FlasherDevice dev, string file, uint addr)
        {
            if (string.IsNullOrEmpty(file)) { Error("butuh nama file output"); return 2; }

            ChipInfo info = dev.GetInfo(null);
            if (!info.Detected || info.SizeBytes <= 0) { Error("chip tidak terdeteksi"); return 1; }

            uint size = (uint)info.SizeBytes;
            Info("baca " + size + " byte dari " + info.Name + "...");
            byte[] data = dev.ReadFlash(addr, size,
                (d, t) => Console.Write("\rbaca {0:0.0}%", 100.0 * d / t),
                CancellationToken.None, null);
            Console.WriteLine();

            File.WriteAllBytes(file, data);
            Info("tersimpan: " + file + " (" + data.Length + " byte)");
            return 0;
        }

        private static string SinglePort()
        {
            string[] ports = FlasherDevice.GetPorts();
            return ports.Length == 1 ? ports[0] : null;
        }

        private static bool TryParseUInt(string text, out uint value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text)) return false;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(text.Substring(2), NumberStyles.HexNumber,
                                     CultureInfo.InvariantCulture, out value);
            return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static void Info(string message)
        {
            if (!string.IsNullOrEmpty(message)) Console.WriteLine(message);
        }

        private static void Error(string message)
        {
            Console.Error.WriteLine("!! " + message);
        }

        private static void Usage()
        {
            Console.WriteLine(
@"BIOS Flasher - mode command line

  BiosFlasher.exe --cli <perintah> [opsi]

Perintah:
  info                     tampilkan chip (JEDEC, kapasitas, status)
  status                   status register 1/2/3
  unprotect                hapus block protect
  backup <file>            simpan isi chip ke file .bin
  write <file>             hapus + tulis + verifikasi (CRC32)
  verify <file>            bandingkan isi chip dengan file
  erase --yes              hapus seluruh chip
  freq <kHz>               ubah clock SPI

Opsi:
  -p, --port COM5          pilih port (default: auto-detect VID 303A)
  -a, --addr 0x0           alamat awal
  --no-verify              lewati verifikasi setelah write
  -y, --yes                konfirmasi untuk perintah berbahaya

Contoh:
  BiosFlasher.exe --cli info
  BiosFlasher.exe --cli write bios.bin -p COM5
  BiosFlasher.exe --cli backup backup.bin
");
        }
    }
}
