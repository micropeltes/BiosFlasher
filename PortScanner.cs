using System;
using Microsoft.Win32;

namespace BiosFlasher
{
    /// <summary>Cari port serial milik ESP32-C3 (USB VID 0x303A) lewat registry.</summary>
    public static class PortScanner
    {
        private const string EspressifVid = "VID_303A";

        public static string FindEsp32()
        {
            try
            {
                using (RegistryKey usb = Registry.LocalMachine.OpenSubKey(
                           @"SYSTEM\CurrentControlSet\Enum\USB"))
                {
                    if (usb == null) return null;

                    foreach (string vidKey in usb.GetSubKeyNames())
                    {
                        if (!vidKey.StartsWith(EspressifVid, StringComparison.OrdinalIgnoreCase))
                            continue;

                        using (RegistryKey dev = usb.OpenSubKey(vidKey))
                        {
                            if (dev == null) continue;

                            foreach (string inst in dev.GetSubKeyNames())
                            {
                                string port = ReadPortName(dev, inst);
                                if (!string.IsNullOrEmpty(port)) return port;
                            }
                        }
                    }
                }
            }
            catch
            {
                // registry tidak bisa dibaca -> biarkan pemanggil pakai fallback
            }
            return null;
        }

        private static string ReadPortName(RegistryKey dev, string instance)
        {
            try
            {
                using (RegistryKey k = dev.OpenSubKey(instance))
                {
                    if (k == null) return null;

                    string direct = k.GetValue("PortName") as string;
                    if (!string.IsNullOrEmpty(direct)) return direct;

                    using (RegistryKey dp = k.OpenSubKey("Device Parameters"))
                    {
                        if (dp != null)
                        {
                            string nested = dp.GetValue("PortName") as string;
                            if (!string.IsNullOrEmpty(nested)) return nested;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
