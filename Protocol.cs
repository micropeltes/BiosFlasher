using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace BiosFlasher
{
    /// <summary>Kesalahan yang berasal dari firmware / port serial.</summary>
    public class DeviceException : Exception
    {
        public DeviceException(string message) : base(message) { }
    }

    /// <summary>
    /// Klien protokol firmware ESP32-C3 BIOS Flasher (src/main.cpp).
    ///   115200 8N1, teks ASCII, prompt "> ".
    ///   perintah  : info, status, unprotect, freq, reset, pins,
    ///               read, binread, write, verify, erase, berase,
    ///               chiperase confirm
    ///   balasan   : '+' ACK baris data, '#' info, '!' error,
    ///               'OK'/'DONE'/'VERIFY OK' sukses
    /// </summary>
    public class FlasherDevice : IDisposable
    {
        public const int ChunkSize = 256;   // batas 256 byte per baris hex di firmware

        private SerialPort _sp;
        private Stream _transport;   // dipakai oleh simulator / self-test

        public string PortName { get; private set; }

        public bool IsOpen
        {
            get { return _transport != null || (_sp != null && _sp.IsOpen); }
        }

        /// <summary>Buat instance yang bicara lewat Stream (untuk self-test/simulator).</summary>
        public static FlasherDevice CreateForStream(Stream transport)
        {
            if (transport == null) throw new ArgumentNullException("transport");
            FlasherDevice dev = new FlasherDevice();
            dev._transport = transport;
            dev.PortName = "(simulator)";
            dev.Sync(1500);   // buang prompt awal, sama seperti Open()
            return dev;
        }

        public static string[] GetPorts()
        {
            try { return SerialPort.GetPortNames(); }
            catch { return new string[0]; }
        }

        public void Open(string port)
        {
            Close();

            SerialPort sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
            sp.ReadTimeout = 50;
            sp.WriteTimeout = 5000;
            sp.Handshake = Handshake.None;
            sp.DtrEnable = false;   // jangan reset chip saat port dibuka
            sp.RtsEnable = false;
            sp.Open();

            try { sp.DiscardInBuffer(); sp.DiscardOutBuffer(); } catch { }

            _sp = sp;
            PortName = port;

            Sync(1500);   // buang sapaan / prompt yang masih mengalir
        }

        public void Close()
        {
            if (_sp == null)
            {
                _transport = null;
                PortName = null;
                return;
            }
            try { _sp.Close(); } catch { }
            try { _sp.Dispose(); } catch { }
            _sp = null;
            PortName = null;
        }

        public void Dispose()
        {
            Close();
        }

        /* ------------------------------------------------------------------
         *  low level
         * ---------------------------------------------------------------- */

        private void Write(string text)
        {
            Write(Encoding.ASCII.GetBytes(text));
        }

        private void Write(byte[] buffer)
        {
            if (!IsOpen) throw new DeviceException("port serial belum dibuka");
            try
            {
                if (_transport != null)
                {
                    _transport.Write(buffer, 0, buffer.Length);
                    _transport.Flush();
                }
                else
                {
                    _sp.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                throw new DeviceException("gagal mengirim data: " + ex.Message);
            }
        }

        /// <summary>Baca 1 byte; -1 kalau timeout.</summary>
        private int ReadByte(int timeoutMs)
        {
            if (!IsOpen) throw new DeviceException("port serial belum dibuka");
            int end = Environment.TickCount + Math.Max(1, timeoutMs);
            while (true)
            {
                try
                {
                    if (_transport != null)
                    {
                        byte[] one = new byte[1];
                        int n = _transport.Read(one, 0, 1);
                        if (n > 0) return one[0];
                    }
                    else
                    {
                        return _sp.ReadByte();
                    }
                }
                catch (TimeoutException)
                {
                    // belum ada data, ulangi sampai batas waktu
                }
                catch (Exception ex)
                {
                    throw new DeviceException("koneksi terputus: " + ex.Message);
                }
                if (Environment.TickCount >= end) return -1;
            }
        }

        /// <summary>Baca satu baris (dipisah CR/LF). null kalau timeout.</summary>
        public string ReadLine(int timeoutMs)
        {
            StringBuilder sb = new StringBuilder();
            int end = Environment.TickCount + Math.Max(1, timeoutMs);
            while (Environment.TickCount < end)
            {
                int c = ReadByte(end - Environment.TickCount);
                if (c < 0) break;
                if (c == '\n') return sb.ToString();
                if (c == '\r') continue;
                sb.Append((char)c);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>Baca keluaran firmware sampai prompt "&gt; " muncul.</summary>
        public List<string> ReadUntilPrompt(int timeoutMs)
        {
            List<string> lines = new List<string>();
            StringBuilder sb = new StringBuilder();
            int end = Environment.TickCount + timeoutMs;

            while (Environment.TickCount < end)
            {
                int c = ReadByte(end - Environment.TickCount);
                if (c < 0) break;
                if (c == '\n')
                {
                    lines.Add(sb.ToString().TrimEnd());
                    sb.Length = 0;
                    continue;
                }
                if (c == '\r') continue;
                sb.Append((char)c);

                int len = sb.Length;
                // prompt "> " harus berdiri sendiri di awal baris (bukan bagian dari "-> ")
                if (len == 2 && sb[0] == '>' && sb[1] == ' ')
                {
                    return lines;
                }
            }
            throw new DeviceException("timeout menunggu prompt (device tidak merespon?)");
        }

        /// <summary>Buang sisa keluaran sampai prompt (best effort).</summary>
        public void Sync(int timeoutMs)
        {
            try { ReadUntilPrompt(timeoutMs); }
            catch (DeviceException) { }
        }

        /// <summary>Kirim perintah, kembalikan semua baris balasan.</summary>
        public List<string> Command(string command, int timeoutMs)
        {
            Write(command + "\n");
            List<string> lines = ReadUntilPrompt(timeoutMs);
            foreach (string ln in lines)
            {
                if (ln.StartsWith("!ERR"))
                    throw new DeviceException(ln.Substring(4).Trim());
            }
            return lines;
        }

        /* ------------------------------------------------------------------
         *  operasi streaming: write / verify
         * ---------------------------------------------------------------- */

        /// <summary>Baca ACK sebuah baris data: "+" atau pesan error.</summary>
        private string ReadAck(int timeoutMs)
        {
            int end = Environment.TickCount + timeoutMs;
            while (Environment.TickCount < end)
            {
                int c = ReadByte(end - Environment.TickCount);
                if (c < 0) break;
                if (c == '+') return "+";
                if (c == '!')
                {
                    string rest = ReadLine(5000);
                    return "!" + (rest ?? "");
                }
            }
            return "";
        }

        /// <summary>
        /// Kirim blok data ke firmware (perintah "write" atau "verify").
        /// progress(doneBytes, totalBytes) dipanggil setiap 256 byte.
        /// </summary>
        public string StreamData(string command, byte[] data, Action<long, long> progress,
                                 CancellationToken ct, Action<string> log)
        {
            if (data == null || data.Length == 0)
                throw new DeviceException("data kosong");

            Write(command + "\n");

            string first = ReadLine(15000);
            if (first == null)
                throw new DeviceException("tidak ada balasan untuk '" + command + "'");
            if (first.StartsWith("!ERR"))
                throw new DeviceException(first.Substring(4).Trim().TrimStart('\n'));
            if (log != null) log(first.Trim());

            long total = data.Length;
            StringBuilder hex = new StringBuilder(ChunkSize * 2);

            for (int off = 0; off < data.Length; off += ChunkSize)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);

                int n = Math.Min(ChunkSize, data.Length - off);
                hex.Length = 0;
                for (int i = 0; i < n; i++) hex.Append(data[off + i].ToString("X2"));

                Write(hex.ToString() + "\n");

                string ack = ReadAck(20000);
                if (ack != "+")
                {
                    string msg = ack.Length > 1 ? ack.Substring(1).Trim() : "tidak ada ACK dari device";
                    throw new DeviceException(msg);
                }
                if (progress != null) progress(off + n, total);
            }

            Write(".\n");

            while (true)
            {
                string ln = ReadLine(180000);
                if (ln == null) throw new DeviceException("timeout menunggu hasil dari device");
                ln = ln.Trim();
                if (ln.Length == 0) continue;
                if (ln.StartsWith("!"))
                    throw new DeviceException(ln.TrimStart('!').Trim());
                if (ln.StartsWith("MISMATCH"))
                {
                    if (log != null) log(ln);
                    continue;
                }
                if (ln.StartsWith("DONE") || ln.StartsWith("VERIFY OK"))
                {
                    Sync(3000);
                    return ln;
                }
            }
        }

        /// <summary>Program flash mulai alamat addr.</summary>
        public string WriteFlash(uint addr, byte[] data, Action<long, long> progress,
                                 CancellationToken ct, Action<string> log)
        {
            return StreamData("write " + addr, data, progress, ct, log);
        }

        /// <summary>Bandingkan isi flash dengan data.</summary>
        public string VerifyFlash(uint addr, byte[] data, Action<long, long> progress,
                                  CancellationToken ct, Action<string> log)
        {
            return StreamData("verify " + addr, data, progress, ct, log);
        }

        /* ------------------------------------------------------------------
         *  baca data (binread) + perintah lain
         * ---------------------------------------------------------------- */

        /// <summary>Baca sejumlah byte biner; -1 di akhir kalau timeout.</summary>
        private int ReadBlock(byte[] buffer, int offset, int count, int idleTimeoutMs)
        {
            int got = 0;
            int end = Environment.TickCount + idleTimeoutMs;
            while (got < count)
            {
                try
                {
                    int n = _transport != null
                        ? _transport.Read(buffer, offset + got, count - got)
                        : _sp.Read(buffer, offset + got, count - got);
                    if (n > 0)
                    {
                        got += n;
                        end = Environment.TickCount + idleTimeoutMs;
                    }
                }
                catch (TimeoutException)
                {
                    // tunggu data berikutnya
                }
                catch (Exception ex)
                {
                    throw new DeviceException("koneksi terputus: " + ex.Message);
                }
                if (Environment.TickCount >= end) break;
            }
            return got;
        }

        /// <summary>Dump isi flash (perintah binread) ke array byte.</summary>
        public byte[] ReadFlash(uint addr, uint length, Action<long, long> progress,
                                CancellationToken ct, Action<string> log)
        {
            if (length == 0) throw new DeviceException("panjang baca 0");
            if (length > 64u * 1024u * 1024u) throw new DeviceException("panjang baca terlalu besar");

            Write("binread " + addr + " " + length + "\n");

            while (true)
            {
                string ln = ReadLine(15000);
                if (ln == null) throw new DeviceException("timeout menunggu header @BIN");
                if (ln.StartsWith("!ERR")) throw new DeviceException(ln.Substring(4).Trim());
                if (ln.Contains("@BIN")) break;
            }

            byte[] data = new byte[length];
            int got = 0;
            const int blockSize = 4096;

            while (got < length)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                int want = (int)Math.Min(blockSize, (long)length - got);
                int n = ReadBlock(data, got, want, 20000);
                if (n <= 0) throw new DeviceException("data biner tidak lengkap (" + got + "/" + length + " byte)");
                got += n;
                if (progress != null) progress(got, length);
            }

            // tunggu penutup "@END"
            for (int i = 0; i < 20; i++)
            {
                string ln = ReadLine(5000);
                if (ln == null) break;
                if (log != null && ln.Trim().Length > 0) log(ln.Trim());
                if (ln.Contains("@END")) break;
            }
            Sync(3000);
            return data;
        }

        /// <summary>Baca informasi chip (perintah info).</summary>
        public ChipInfo GetInfo(Action<string> log)
        {
            List<string> lines = Command("info", 30000);
            if (log != null)
            {
                foreach (string ln in lines) log(ln);
            }
            return ChipInfo.Parse(lines);
        }

        /// <summary>Hapus sejumlah sector (4 KB) atau block (64 KB).</summary>
        public string Erase(uint addr, int count, bool block, Action<string> log)
        {
            if (count <= 0) count = 1;
            long unit = block ? 65536L : 4096L;
            int timeout = (int)Math.Min(600000L, 30000L + count * unit / 1024L * 12L);

            string cmd = (block ? "berase " : "erase ") + addr + " " + count;
            List<string> lines = Command(cmd, timeout);
            if (log != null)
            {
                foreach (string ln in lines) log(ln);
            }
            return lines.Count > 0 ? lines[lines.Count - 1] : "";
        }

        /// <summary>Hapus seluruh chip.</summary>
        public string ChipErase(Action<string> log)
        {
            List<string> lines = Command("chiperase confirm", 420000);
            if (log != null)
            {
                foreach (string ln in lines) log(ln);
            }
            return lines.Count > 0 ? lines[lines.Count - 1] : "";
        }

        /// <summary>Bersihkan bit proteksi (BP).</summary>
        public string Unprotect(Action<string> log)
        {
            List<string> lines = Command("unprotect", 30000);
            if (log != null)
            {
                foreach (string ln in lines) log(ln);
            }
            return lines.Count > 0 ? lines[lines.Count - 1] : "";
        }

        public string SetFrequency(int khz)
        {
            List<string> lines = Command("freq " + khz, 20000);
            return lines.Count > 0 ? lines[lines.Count - 1] : "";
        }

        public void ResetChip()
        {
            Command("reset", 20000);
        }
    }
}
