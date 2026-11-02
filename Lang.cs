using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BiosFlasher
{
    public enum Lang
    {
        Indonesian = 0,
        English = 1,
        Chinese = 2
    }

    /// <summary>
    /// Kamus teks 3 bahasa: Indonesia (0), English (1), 中文 (2).
    /// Pilihan bahasa disimpan di %APPDATA%\BiosFlasher\settings.ini
    /// </summary>
    public static class L
    {
        public static Lang Current = Lang.Indonesian;

        private static readonly Dictionary<string, string[]> _t =
            new Dictionary<string, string[]>(StringComparer.Ordinal);

        static L()
        {
            /* ---- judul & label ---- */
            Add("app.title", "BIOS Flasher  -  ESP32-C3", "BIOS Flasher  -  ESP32-C3", "BIOS 烧录器  -  ESP32-C3");

            Add("grp.conn", "Koneksi", "Connection", "连接");
            Add("lbl.port", "Port USB:", "USB port:", "USB 端口:");
            Add("lbl.lang", "Bahasa:", "Language:", "语言:");
            Add("btn.refresh", "Muat Ulang", "Refresh", "刷新");
            Add("btn.connect", "Hubungkan", "Connect", "连接");
            Add("btn.disconnect", "Putuskan", "Disconnect", "断开");
            Add("lbl.offline", "Terputus", "Disconnected", "未连接");
            Add("lbl.online", "Terhubung: ", "Connected: ", "已连接: ");
            Add("lang.hint", "Pilih bahasa tampilan", "Choose display language", "选择界面语言");

            Add("grp.chip", "Chip BIOS", "BIOS chip", "BIOS 芯片");
            Add("btn.identify", "Identifikasi", "Identify", "识别芯片");
            Add("lbl.nodetected", "TIDAK terdeteksi", "NOT detected", "未检测到");
            Add("lbl.jedec", "JEDEC: ", "JEDEC: ", "JEDEC: ");
            Add("lbl.size", "Kapasitas: ", "Capacity: ", "容量: ");
            Add("lbl.status", "Status: ", "Status: ", "状态: ");

            Add("grp.file", "File BIOS", "BIOS file", "BIOS 文件");
            Add("btn.browse", "Pilih File...", "Choose file...", "选择文件...");
            Add("lbl.nofile", "Belum ada file dipilih.", "No file selected yet.", "尚未选择文件。");
            Add("file.info", "{0}  -  {1}  ({2:N0} byte)", "{0}  -  {1}  ({2:N0} bytes)", "{0}  -  {1}  ({2:N0} 字节)");

            Add("grp.action", "Aksi", "Actions", "操作");
            Add("btn.write", "TULIS KE CHIP", "WRITE TO CHIP", "写入芯片");
            Add("btn.verify", "Verifikasi", "Verify", "校验");
            Add("btn.backup", "Backup / Baca", "Backup / Read", "备份 / 读取");
            Add("btn.erase", "Hapus Chip", "Erase Chip", "擦除芯片");
            Add("btn.cancel", "Batal", "Cancel", "取消");
            Add("lbl.note", "Tulis = hapus + program + verifikasi", "Write = erase + program + verify", "写入 = 擦除 + 编程 + 校验");

            Add("status.ready", "Siap.", "Ready.", "就绪。");
            Add("status.connectfirst", "Hubungkan dulu ke port ESP32-C3.",
                "Connect to the ESP32-C3 port first.", "请先连接到 ESP32-C3 端口。");

            /* ---- pesan dialog ---- */
            Add("msg.selectport",
                "Pilih port USB ESP32-C3 dulu.\n\nKalau port tidak muncul: pakai kabel USB data, " +
                "lepas-pasang board, lalu klik 'Muat Ulang'.",
                "Select the ESP32-C3 USB port first.\n\nIf no port appears: use a data cable, " +
                "unplug/replug the board, then click 'Refresh'.",
                "请先选择 ESP32-C3 的 USB 端口。\n\n如果没有端口：请使用数据线，重新插拔开发板，" +
                "然后点击“刷新”。");

            Add("msg.nofile", "Pilih file BIOS (.bin) dulu.",
                "Choose a BIOS file (.bin) first.", "请先选择 BIOS 文件 (.bin)。");
            Add("msg.connectfail", "Gagal membuka {0}:\n{1}",
                "Failed to open {0}:\n{1}", "无法打开 {0}:\n{1}");
            Add("msg.badfile", "Gagal membaca file:\n{0}",
                "Failed to read file:\n{0}", "无法读取文件:\n{0}");
            Add("msg.badfile.empty", "file kosong", "file is empty", "文件为空");
            Add("msg.badfile.big", "file lebih dari 32 MB, tidak masuk akal untuk chip BIOS",
                "file is larger than 32 MB - too big for a BIOS chip",
                "文件大于 32 MB，不像 BIOS 芯片镜像");
            Add("msg.badfile.hex",
                "file ini sepertinya Intel HEX, bukan biner. Konversi dulu ke .bin",
                "this file looks like Intel HEX, not binary. Convert it to .bin first",
                "该文件看起来是 Intel HEX，不是二进制。请先转换为 .bin");
            Add("msg.needid", "Klik 'Identifikasi' dulu supaya ukuran chip diketahui.",
                "Click 'Identify' first so the chip size is known.",
                "请先点“识别芯片”以获取芯片容量。");

            Add("msg.confirm.write",
                "Tulis {0} ke chip BIOS mulai alamat 0x00000000 ?\n\n" +
                "Sector yang tercakup akan DIHAPUS, lalu ditulis, lalu diverifikasi.\n" +
                "Jangan cabut kabel selama proses berjalan.",
                "Write {0} to the BIOS chip starting at address 0x00000000 ?\n\n" +
                "Covered sectors will be ERASED, then programmed and verified.\n" +
                "Do not unplug the cable while the process runs.",
                "将 {0} 写入 BIOS 芯片，起始地址 0x00000000 ？\n\n" +
                "涉及的扇区将被擦除，然后写入并校验。\n处理过程中请勿拔线。");

            Add("msg.confirm.erase",
                "Hapus SELURUH isi chip BIOS?\n\nData lama tidak bisa dikembalikan " +
                "(kecuali sudah punya backup).",
                "Erase the ENTIRE BIOS chip?\n\nOld data cannot be recovered " +
                "(unless you already have a backup).",
                "擦除芯片中的全部内容？\n\n旧数据无法恢复（除非已有备份）。");

            Add("msg.confirm.close", "Operasi masih berjalan. Tutup paksa?",
                "An operation is still running. Force close?", "操作仍在进行。强制关闭？");

            Add("title.confirm.write", "Konfirmasi tulis", "Confirm write", "确认写入");
            Add("title.confirm.erase", "Konfirmasi hapus", "Confirm erase", "确认擦除");
            Add("title.close", "Tutup", "Close", "关闭");
            Add("title.error", "Kesalahan", "Error", "错误");
            Add("title.info", "Informasi", "Information", "提示");
            Add("title.failed", "{0} gagal", "{0} failed", "{0} 失败");

            /* ---- nama operasi ---- */
            Add("op.identify", "Identifikasi chip", "Identify chip", "识别芯片");
            Add("op.write", "Tulis BIOS", "Write BIOS", "写入 BIOS");
            Add("op.verify", "Verifikasi", "Verify", "校验");
            Add("op.backup", "Backup chip", "Backup chip", "备份芯片");
            Add("op.erase", "Hapus chip", "Erase chip", "擦除芯片");

            /* ---- log ---- */
            Add("log.noports", "(tidak ada port serial terdeteksi)",
                "(no serial port detected)", "（未检测到串口）");
            Add("log.opening", "membuka {0} ...", "opening {0} ...", "正在打开 {0} ...");
            Add("log.connected", "terhubung ke {0}", "connected to {0}", "已连接到 {0}");
            Add("log.closed", "port ditutup ({0})", "port closed ({0})", "端口已关闭 ({0})");
            Add("log.openfail", "!! gagal membuka {0}: {1}",
                "!! failed to open {0}: {1}", "!! 打开 {0} 失败：{1}");
            Add("log.cancelreq", "permintaan batal dikirim...",
                "cancel requested...", "已发送取消请求...");
            Add("log.cancelled", "!! dibatalkan oleh pengguna",
                "!! cancelled by user", "!! 已被用户取消");
            Add("log.error", "!! ERROR: {0}", "!! ERROR: {0}", "!! 错误：{0}");
            Add("log.unprotect", "bersihkan proteksi (unprotect)...",
                "clearing write protection (unprotect)...", "清除写保护 (unprotect)...");
            Add("log.erasecount", "hapus {0} sector (4 KB) = {1}",
                "erasing {0} sectors (4 KB) = {1}", "擦除 {0} 个扇区 (4 KB) = {1}");
            Add("log.writing", "mulai menulis data...", "writing data...", "开始写入数据...");
            Add("log.verifying", "verifikasi isi flash...", "verifying flash content...", "校验 Flash 内容...");
            Add("log.reading", "baca {0} dari chip...", "reading {0} from chip...", "从芯片读取 {0}...");
            Add("log.saving", "menyimpan file...", "saving file...", "正在保存文件...");
            Add("log.saved", "tersimpan: {0} ({1})", "saved: {0} ({1})", "已保存：{0} ({1})");
            Add("log.filesel", "file dipilih: {0} ({1})",
                "file selected: {0} ({1})", "已选择文件：{0} ({1})");

            /* ---- progress ---- */
            Add("prog.stage", "{0}: {1:0.0}%  ({2} / {3})",
                "{0}: {1:0.0}%  ({2} / {3})", "{0}: {1:0.0}%  ({2} / {3})");
            Add("prog.erase", "Hapus", "Erase", "擦除");
            Add("prog.write", "Tulis", "Write", "写入");
            Add("prog.verify", "Verifikasi", "Verify", "校验");
            Add("prog.read", "Baca", "Read", "读取");
            Add("prog.erase.n", "Hapus: {0} / {1} sector", "Erase: {0} / {1} sectors", "擦除: {0} / {1} 扇区");
            Add("prog.done.write", "SELESAI - BIOS berhasil ditulis dan terverifikasi.",
                "DONE - BIOS written and verified successfully.", "完成 - BIOS 已写入并通过校验。");
            Add("prog.done.verify", "Isi chip SAMA dengan file.",
                "Chip content MATCHES the file.", "芯片内容与文件一致。");
            Add("prog.done.backup", "Backup selesai: {0}", "Backup done: {0}", "备份完成：{0}");
            Add("prog.done.erase", "Chip kosong (semua 0xFF).",
                "Chip is blank (all 0xFF).", "芯片已擦空（全部 0xFF）。");
            Add("prog.chip", "Chip: {0} / {1}", "Chip: {0} / {1}", "芯片：{0} / {1}");
            Add("prog.nochip", "Chip TIDAK terdeteksi - cek kabel & 3V3",
                "Chip NOT detected - check wiring & 3V3", "未检测到芯片 - 请检查接线和 3V3");
            Add("prog.file.ready", "File siap ditulis.", "File ready to write.", "文件已准备好写入。");
            Add("prog.eraseall", "menghapus seluruh chip (bisa 1-3 menit)...",
                "erasing the whole chip (may take 1-3 minutes)...", "正在擦除整个芯片（可能需要 1-3 分钟）...");
            Add("prog.failed", "Gagal: {0}", "Failed: {0}", "失败：{0}");
            Add("prog.cancelled.status", "Dibatalkan.", "Cancelled.", "已取消。");

            /* ---- fitur cepat: biner, CRC32, blank check ---- */
            Add("log.wbin", "kirim data biner (1024 byte/chunk)...",
                "sending binary data (1024 bytes/chunk)...", "发送二进制数据（每块 1024 字节）...");
            Add("log.wbin.old", "firmware belum mendukung mode biner - pakai hex",
                "firmware has no binary mode - using hex", "固件不支持二进制模式 - 改用十六进制");
            Add("log.blank.check", "cek area kosong (0xFF)...",
                "checking blank region (0xFF)...", "检查空白区域（0xFF）...");
            Add("log.blank.ok", "OK - area kosong (0xFF)", "OK - region is blank (0xFF)",
                "OK - 区域为空（0xFF）");
            Add("log.blank.fail", "PERINGATAN: {0} byte bukan 0xFF (offset {1})",
                "WARNING: {0} bytes are not 0xFF (offset {1})",
                "警告：{0} 字节不是 0xFF（偏移 {1}）");
            Add("log.blank.old", "firmware belum mendukung cek kosong - dilewati",
                "firmware has no blank check - skipped", "固件不支持空白检查 - 已跳过");
            Add("log.crc.check", "verifikasi CRC32...", "verifying CRC32...", "校验 CRC32...");
            Add("log.crc.match", "CRC32 COCOK ({0})", "CRC32 MATCH ({0})", "CRC32 一致（{0}）");
            Add("log.crc.mismatch", "CRC32 TIDAK COCOK - file {0}, chip {1}",
                "CRC32 MISMATCH - file {0}, chip {1}", "CRC32 不一致 - 文件 {0}，芯片 {1}");
            Add("log.crc.old", "firmware belum mendukung CRC32 - verifikasi penuh byte-per-byte",
                "firmware has no CRC32 - doing full byte-by-byte verify",
                "固件不支持 CRC32 - 改用逐字节完整校验");
            Add("prog.eta", "sisa ~{0}", "ETA ~{0}", "剩余 ~{0}");

            /* ---- Sprint 2: port, log, tentang ---- */
            Add("log.espport", "port ESP32-C3 terdeteksi: {0}",
                "ESP32-C3 port detected: {0}", "已检测到 ESP32-C3 端口：{0}");
            Add("btn.savelog", "Simpan Log", "Save log", "保存日志");
            Add("btn.about", "Tentang", "About", "关于");
            Add("log.exported", "log disimpan ke {0}", "log saved to {0}", "日志已保存到 {0}");
            Add("log.startfile", "log file: {0}", "log file: {0}", "日志文件：{0}");
            Add("about.text",
                "BIOS Flasher versi {0}\n\n" +
                "Programmer SPI-NOR / chip BIOS untuk ESP32-C3-DevKitM-1\n" +
                "lewat USB native (USB-Serial/JTAG), tanpa USB-TTL.\n\n" +
                "Firmware : SPI_ESP32C3_biosFLASHER (src/main.cpp)\n" +
                "Bahasa   : Indonesia / English / 中文\n\n" +
                "Dipakai dengan hati-hati - selalu buat backup dulu.",
                "BIOS Flasher version {0}\n\n" +
                "SPI-NOR / BIOS chip programmer for ESP32-C3-DevKitM-1\n" +
                "over native USB (USB-Serial/JTAG), no USB-TTL needed.\n\n" +
                "Firmware : SPI_ESP32C3_biosFLASHER (src/main.cpp)\n" +
                "Languages: Indonesia / English / 中文\n\n" +
                "Use with care - always make a backup first.",
                "BIOS 烧录器 版本 {0}\n\n" +
                "用于 ESP32-C3-DevKitM-1 的 SPI-NOR / BIOS 芯片烧录工具，\n" +
                "通过原生 USB（USB-Serial/JTAG），无需 USB-TTL。\n\n" +
                "固件：SPI_ESP32C3_biosFLASHER (src/main.cpp)\n" +
                "语言：Indonesia / English / 中文\n\n" +
                "请谨慎使用 - 务必先备份。");
        }

        /* ------------------------------------------------------------------
         *  akses kamus
         * ---------------------------------------------------------------- */

        private static void Add(string key, string id, string en, string zh)
        {
            _t[key] = new string[] { id, en, zh };
        }

        /// <summary>Teks untuk kunci tertentu pada bahasa aktif.</summary>
        public static string T(string key)
        {
            string[] row;
            if (_t.TryGetValue(key, out row))
            {
                int i = (int)Current;
                if (i >= 0 && i < row.Length && row[i] != null) return row[i];
                return row[0];
            }
            return "[" + key + "]";
        }

        /// <summary>Teks berformat (string.Format dengan InvariantCulture).</summary>
        public static string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T(key), args);
        }

        /// <summary>Nama bahasa untuk ditampilkan di combo box.</summary>
        public static string DisplayName(Lang lang)
        {
            switch (lang)
            {
                case Lang.English: return "English";
                case Lang.Chinese: return "中文（简体）";
                default: return "Bahasa Indonesia";
            }
        }

        /// <summary>Bahasa yang dipakai saat aplikasi mulai (dari settings, default Indonesia).</summary>
        public static void AutoDetect()
        {
            Lang saved;
            if (TryLoad(out saved))
            {
                Current = saved;
                return;
            }

            // belum pernah dipilih: ikuti bahasa Windows
            string ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (ui == "en") Current = Lang.English;
            else if (ui == "zh") Current = Lang.Chinese;
            else Current = Lang.Indonesian;
        }

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BiosFlasher");
                return Path.Combine(dir, "settings.ini");
            }
        }

        public static bool TryLoad(out Lang lang)
        {
            lang = Lang.Indonesian;
            try
            {
                string path = SettingsPath;
                if (!File.Exists(path)) return false;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!line.Substring(0, eq).Trim().Equals("lang", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string v = line.Substring(eq + 1).Trim().ToLowerInvariant();
                    if (v == "id") { lang = Lang.Indonesian; return true; }
                    if (v == "en") { lang = Lang.English; return true; }
                    if (v == "zh") { lang = Lang.Chinese; return true; }
                }
            }
            catch { }
            return false;
        }

        public static void Save()
        {
            try
            {
                string path = SettingsPath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string code = Current == Lang.English ? "en" : (Current == Lang.Chinese ? "zh" : "id");
                File.WriteAllText(path, "lang=" + code + Environment.NewLine);
            }
            catch { }
        }
    }
}


