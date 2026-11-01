using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BiosFlasher
{
    /// <summary>Hasil parsing keluaran perintah "info" dari firmware.</summary>
    public class ChipInfo
    {
        public bool Detected;
        public string Jedec = "-";
        public string Name = "TIDAK terdeteksi";
        public string SizeText = "";
        public long SizeBytes;
        public string Status1 = "-";
        public string Status2 = "-";
        public string Status3 = "-";
        public string Clock = "-";
        public string Pins = "-";

        private static readonly Regex MbRegex =
            new Regex(@"\((\d+)\s*MB", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static ChipInfo Parse(List<string> lines)
        {
            ChipInfo info = new ChipInfo();
            if (lines == null) return info;

            foreach (string raw in lines)
            {
                string ln = raw.Trim();
                int idx = ln.IndexOf(':');
                if (idx <= 0) continue;

                string key = ln.Substring(0, idx).Trim();
                string val = ln.Substring(idx + 1).Trim();

                switch (key)
                {
                    case "JEDEC ID":
                        info.Jedec = val;
                        break;

                    case "Chip":
                        if (val.StartsWith("TIDAK", StringComparison.OrdinalIgnoreCase))
                        {
                            info.Detected = false;
                            info.Name = "TIDAK terdeteksi";
                        }
                        else
                        {
                            info.Detected = true;
                            int par = val.IndexOf('(');
                            info.Name = par > 0 ? val.Substring(0, par).Trim() : val;

                            Match m = MbRegex.Match(val);
                            if (m.Success)
                            {
                                long mb;
                                if (long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                                                  CultureInfo.InvariantCulture, out mb))
                                {
                                    info.SizeBytes = mb * 1024L * 1024L;
                                }
                            }

                            int close = val.IndexOf(')');
                            if (par >= 0 && close > par)
                                info.SizeText = val.Substring(par + 1, close - par - 1).Trim();
                        }
                        break;

                    case "Status 1":
                        info.Status1 = val;
                        break;

                    case "Status 2":
                        info.Status2 = val;
                        break;

                    case "Status 3":
                        info.Status3 = val;
                        break;

                    case "SPI clock":
                        info.Clock = val;
                        break;

                    case "Pins":
                        info.Pins = val;
                        break;
                }
            }
            return info;
        }

        public string SizeHuman
        {
            get
            {
                if (SizeBytes <= 0) return "-";
                if (SizeBytes >= 1024L * 1024L)
                    return (SizeBytes / (1024.0 * 1024.0)).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
                return (SizeBytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            }
        }
    }
}
