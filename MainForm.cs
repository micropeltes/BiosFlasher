using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BiosFlasher
{
    /// <summary>Jendela utama - tampilan sederhana gaya Windows 2000.</summary>
    public class MainForm : Form
    {
        private readonly FlasherDevice _dev = new FlasherDevice();
        private byte[] _fileData;
        private string _filePath;
        private ChipInfo _chip = new ChipInfo();
        private CancellationTokenSource _cts;
        private bool _busy;

        private ComboBox cmbPort, cmbLang;
        private Button btnRefresh, btnConnect, btnInfo, btnBrowse;
        private Button btnWrite, btnVerify, btnBackup, btnErase, btnCancel;
        private Button btnSaveLog, btnAbout;
        private Label lblConn, lblPort, lblLang, lblLangHint, lblChipName;
        private Label lblJedec, lblSize, lblStatus, lblFileInfo, lblProg, lblNote;
        private GroupBox gbPort, gbChip, gbFile, gbAction;
        private TextBox txtFile, txtLog;
        private ProgressBar pb;
        private string _logPath;

        private string _progKey;          // status statis agar bisa diterjemahkan ulang
        private object[] _progArgs;

        public MainForm()
        {
            L.AutoDetect();

            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(660, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = SystemColors.Control;
            ClientSize = new Size(632, 604);

            BuildUi();
            ApplyLanguage(L.Current, false);
            RefreshPorts();
            SetBusy(false);

            Log(L.F("log.startfile", EnsureLogFile()));

            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            Resize += (s, e) => ApplyLayout();

            FormClosing += OnFormClosing;
        }

        private void BuildUi()
        {
            /* ---------- 1. port + bahasa ---------- */
            gbPort = new GroupBox();
            gbPort.SetBounds(8, 6, 616, 78);
            Controls.Add(gbPort);

            lblPort = new Label();
            lblPort.SetBounds(12, 24, 56, 16);
            gbPort.Controls.Add(lblPort);

            cmbPort = new ComboBox();
            cmbPort.SetBounds(70, 20, 108, 21);
            cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
            gbPort.Controls.Add(cmbPort);

            btnRefresh = MakeButton("", 182, 19, 84, 24);
            btnRefresh.Click += (s, e) => RefreshPorts();
            gbPort.Controls.Add(btnRefresh);

            btnConnect = MakeButton("", 270, 19, 90, 24);
            btnConnect.Click += BtnConnect_Click;
            gbPort.Controls.Add(btnConnect);

            lblConn = new Label();
            lblConn.ForeColor = Color.Firebrick;
            lblConn.SetBounds(366, 24, 246, 16);
            gbPort.Controls.Add(lblConn);

            lblLang = new Label();
            lblLang.SetBounds(12, 52, 54, 16);
            gbPort.Controls.Add(lblLang);

            cmbLang = new ComboBox();
            cmbLang.SetBounds(70, 48, 150, 21);
            cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLang.Items.Add(L.DisplayName(Lang.Indonesian));
            cmbLang.Items.Add(L.DisplayName(Lang.English));
            cmbLang.Items.Add(L.DisplayName(Lang.Chinese));
            cmbLang.SelectedIndex = (int)L.Current;
            cmbLang.SelectedIndexChanged += CmbLang_SelectedIndexChanged;
            gbPort.Controls.Add(cmbLang);

            lblLangHint = new Label();
            lblLangHint.SetBounds(228, 52, 384, 16);
            lblLangHint.ForeColor = SystemColors.GrayText;
            gbPort.Controls.Add(lblLangHint);

            /* ---------- 2. chip ---------- */
            gbChip = new GroupBox();
            gbChip.SetBounds(8, 90, 616, 104);
            Controls.Add(gbChip);

            btnInfo = MakeButton("", 12, 20, 104, 26);
            btnInfo.Click += BtnInfo_Click;
            gbChip.Controls.Add(btnInfo);

            lblChipName = new Label();
            lblChipName.SetBounds(126, 25, 480, 16);
            gbChip.Controls.Add(lblChipName);

            lblJedec = MakeValueLabel("", 16, 52, 262);
            gbChip.Controls.Add(lblJedec);

            lblSize = MakeValueLabel("", 286, 52, 318);
            gbChip.Controls.Add(lblSize);

            lblStatus = MakeValueLabel("", 16, 74, 588);
            gbChip.Controls.Add(lblStatus);

            /* ---------- 3. file ---------- */
            gbFile = new GroupBox();
            gbFile.SetBounds(8, 200, 616, 80);
            Controls.Add(gbFile);

            txtFile = new TextBox();
            txtFile.SetBounds(12, 20, 466, 21);
            txtFile.ReadOnly = true;
            txtFile.BackColor = SystemColors.Window;
            gbFile.Controls.Add(txtFile);

            btnBrowse = MakeButton("", 484, 19, 118, 25);
            btnBrowse.Click += BtnBrowse_Click;
            gbFile.Controls.Add(btnBrowse);

            lblFileInfo = MakeValueLabel("", 12, 48, 590);
            gbFile.Controls.Add(lblFileInfo);

            /* ---------- 4. aksi ---------- */
            gbAction = new GroupBox();
            gbAction.SetBounds(8, 286, 616, 110);
            Controls.Add(gbAction);

            btnWrite = MakeButton("", 12, 20, 192, 36);
            btnWrite.Click += BtnWrite_Click;
            gbAction.Controls.Add(btnWrite);

            btnVerify = MakeButton("", 210, 20, 192, 36);
            btnVerify.Click += BtnVerify_Click;
            gbAction.Controls.Add(btnVerify);

            btnBackup = MakeButton("", 408, 20, 192, 36);
            btnBackup.Click += BtnBackup_Click;
            gbAction.Controls.Add(btnBackup);

            btnErase = MakeButton("", 12, 64, 192, 36);
            btnErase.Click += BtnErase_Click;
            gbAction.Controls.Add(btnErase);

            btnCancel = MakeButton("", 408, 64, 192, 36);
            btnCancel.Click += BtnCancel_Click;
            gbAction.Controls.Add(btnCancel);

            lblNote = new Label();
            lblNote.SetBounds(212, 76, 190, 16);
            lblNote.ForeColor = SystemColors.GrayText;
            gbAction.Controls.Add(lblNote);

            /* ---------- progress + log ---------- */
            pb = new ProgressBar();
            pb.SetBounds(8, 404, 616, 18);
            pb.Minimum = 0;
            pb.Maximum = 100;
            Controls.Add(pb);

            lblProg = new Label();
            lblProg.SetBounds(8, 426, 616, 16);
            Controls.Add(lblProg);

            txtLog = new TextBox();
            txtLog.SetBounds(8, 446, 616, 130);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BackColor = SystemColors.Window;
            txtLog.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
            Controls.Add(txtLog);

            /* ---------- tombol bawah ---------- */
            btnSaveLog = MakeButton("", 8, 0, 120, 26);
            btnSaveLog.Click += BtnSaveLog_Click;
            Controls.Add(btnSaveLog);

            btnAbout = MakeButton("", 136, 0, 100, 26);
            btnAbout.Click += BtnAbout_Click;
            Controls.Add(btnAbout);

            ApplyLayout();
        }

        /// <summary>Atur posisi elemen yang ikut berubah saat jendela di-resize.</summary>
        private void ApplyLayout()
        {
            if (txtLog == null) return;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int bottomY = h - 34;

            gbPort.Width = w - 16;
            gbChip.Width = w - 16;
            gbFile.Width = w - 16;
            gbAction.Width = w - 16;

            btnRefresh.Left = w - 616 + 182;
            btnConnect.Left = w - 616 + 270;
            lblConn.Left = w - 616 + 366;
            lblConn.Width = w - lblConn.Left - 12;
            lblLangHint.Width = w - lblLangHint.Left - 12;

            txtFile.Width = w - 16 - 12 - 130;
            btnBrowse.Left = txtFile.Right + 6;
            lblFileInfo.Width = w - 24;

            btnWrite.Width = (w - 40) / 3;
            btnVerify.Width = btnWrite.Width;
            btnBackup.Width = btnWrite.Width;
            btnVerify.Left = btnWrite.Right + 6;
            btnBackup.Left = btnVerify.Right + 6;

            btnErase.Width = btnWrite.Width;
            btnCancel.Width = btnWrite.Width;
            btnCancel.Left = btnBackup.Left;

            lblNote.Left = btnWrite.Right + 6;
            lblNote.Width = btnCancel.Left - lblNote.Left - 12;

            pb.Width = w - 16;
            lblProg.Width = w - 16;

            int logTop = 446;
            int logHeight = Math.Max(70, bottomY - 8 - logTop);
            txtLog.SetBounds(8, logTop, w - 16, logHeight);

            btnSaveLog.Top = bottomY;
            btnAbout.Top = bottomY;
        }

        private Button MakeButton(string text, int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, h);
            b.UseVisualStyleBackColor = false;
            return b;
        }

        private Label MakeValueLabel(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.SetBounds(x, y, w, 16);
            l.AutoEllipsis = true;
            return l;
        }

        /* ------------------------------------------------------------------
         *  bahasa (Indonesia / English / 中文)
         * ---------------------------------------------------------------- */

        private bool _changingLang;

        private static Font BaseFont()
        {
            if (L.Current == Lang.Chinese)
            {
                // font yang punya glyph Han; kalau tidak ada, pakai fallback Windows
                return new Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point);
            }
            return new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        }

        private static void ApplyFontToChildren(Control parent, Font font)
        {
            foreach (Control c in parent.Controls)
            {
                c.Font = font;
                if (c.HasChildren) ApplyFontToChildren(c, font);
            }
        }

        private void CmbLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_changingLang) return;
            int idx = cmbLang.SelectedIndex;
            if (idx < 0) return;
            ApplyLanguage((Lang)idx, true);
        }

        /// <summary>Terapkan bahasa ke seluruh kontrol (tanpa membuat ulang form).</summary>
        private void ApplyLanguage(Lang lang, bool save)
        {
            L.Current = lang;
            if (save) L.Save();

            Font baseFont = BaseFont();
            Font = baseFont;

            Text = L.T("app.title");
            gbPort.Text = L.T("grp.conn");
            gbChip.Text = L.T("grp.chip");
            gbFile.Text = L.T("grp.file");
            gbAction.Text = L.T("grp.action");

            lblPort.Text = L.T("lbl.port");
            lblLang.Text = L.T("lbl.lang");
            lblLangHint.Text = L.T("lang.hint");
            btnRefresh.Text = L.T("btn.refresh");
            btnInfo.Text = L.T("btn.identify");
            btnBrowse.Text = L.T("btn.browse");
            btnWrite.Text = L.T("btn.write");
            btnVerify.Text = L.T("btn.verify");
            btnBackup.Text = L.T("btn.backup");
            btnErase.Text = L.T("btn.erase");
            btnCancel.Text = L.T("btn.cancel");
            lblNote.Text = L.T("lbl.note");
            btnSaveLog.Text = L.T("btn.savelog");
            btnAbout.Text = L.T("btn.about");

            ApplyFontToChildren(this, baseFont);
            lblChipName.Font = new Font(baseFont, FontStyle.Bold);
            btnWrite.Font = new Font(baseFont, FontStyle.Bold);
            txtLog.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point);

            // combo bahasa: nama bahasa selalu ditulis dalam bahasanya sendiri
            _changingLang = true;
            cmbLang.Items.Clear();
            cmbLang.Items.Add(L.DisplayName(Lang.Indonesian));
            cmbLang.Items.Add(L.DisplayName(Lang.English));
            cmbLang.Items.Add(L.DisplayName(Lang.Chinese));
            cmbLang.SelectedIndex = (int)L.Current;
            _changingLang = false;

            UpdateConnUi();
            UpdateChipUi(_chip);
            UpdateFileUi();

            if (_progKey != null) lblProg.Text = L.F(_progKey, _progArgs);
            else lblProg.Text = L.T("status.ready");
        }

        private void UpdateConnUi()
        {
            if (_dev.IsOpen)
            {
                btnConnect.Text = L.T("btn.disconnect");
                lblConn.Text = L.T("lbl.online") + _dev.PortName;
                lblConn.ForeColor = Color.DarkGreen;
            }
            else
            {
                btnConnect.Text = L.T("btn.connect");
                lblConn.Text = L.T("lbl.offline");
                lblConn.ForeColor = Color.Firebrick;
            }
        }

        private void UpdateFileUi()
        {
            if (_fileData == null)
            {
                txtFile.Text = "";
                lblFileInfo.Text = L.T("lbl.nofile");
                return;
            }
            txtFile.Text = _filePath ?? "";
            lblFileInfo.Text = L.F("file.info",
                Path.GetFileName(_filePath ?? ""), Bytes(_fileData.Length), _fileData.Length);
        }

        /* ------------------------------------------------------------------
         *  helper UI
         * ---------------------------------------------------------------- */

        private void RefreshPorts()
        {
            string keep = cmbPort.SelectedItem as string;
            cmbPort.Items.Clear();
            string[] ports = FlasherDevice.GetPorts();
            foreach (string p in ports) cmbPort.Items.Add(p);

            // utamakan port USB ESP32-C3 (VID 0x303A)
            string esp = PortScanner.FindEsp32();
            if (esp != null && cmbPort.Items.Contains(esp))
            {
                cmbPort.SelectedItem = esp;
            }
            else if (keep != null && cmbPort.Items.Contains(keep))
            {
                cmbPort.SelectedItem = keep;
            }
            else if (cmbPort.Items.Count > 0)
            {
                cmbPort.SelectedIndex = cmbPort.Items.Count - 1;
            }

            if (ports.Length == 0) Log(L.T("log.noports"));
            else if (esp != null) Log(L.F("log.espport", esp));
        }

        private void Ui(Action action)
        {
            if (action == null) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        private void Log(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            string line = message.Replace("\r", "").TrimEnd();
            txtLog.AppendText(line + Environment.NewLine);
            AppendLogFile(line);
        }

        private string EnsureLogFile()
        {
            if (_logPath != null) return _logPath;
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BiosFlasher", "logs");
                Directory.CreateDirectory(dir);
                _logPath = Path.Combine(dir, "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            }
            catch
            {
                _logPath = "";
            }
            return _logPath;
        }

        private void AppendLogFile(string line)
        {
            try
            {
                string path = EnsureLogFile();
                if (path.Length == 0) return;
                File.AppendAllText(path,
                    DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine);
            }
            catch { }
        }

        private void ReportLog(string message)
        {
            Ui(() => Log(message));
        }

        private void SetStatus(string text)
        {
            _progKey = null;
            _progArgs = null;
            lblProg.Text = text;
        }

        /// <summary>Status statis (bisa diterjemahkan ulang saat bahasa diganti).</summary>
        private void SetStatusKey(string key, params object[] args)
        {
            _progKey = key;
            _progArgs = args;
            lblProg.Text = L.F(key, args);
        }

        private void SetProgress(double percent, string text)
        {
            int v = (int)Math.Round(percent);
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            pb.Value = v;
            if (text != null)
            {
                _progKey = null;
                _progArgs = null;
                lblProg.Text = text;
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            cmbPort.Enabled = !busy;
            btnRefresh.Enabled = !busy;
            btnConnect.Enabled = !busy;
            btnInfo.Enabled = !busy && _dev.IsOpen;
            btnBrowse.Enabled = !busy;
            btnWrite.Enabled = !busy && _dev.IsOpen;
            btnVerify.Enabled = !busy && _dev.IsOpen;
            btnBackup.Enabled = !busy && _dev.IsOpen;
            btnErase.Enabled = !busy && _dev.IsOpen;
            btnCancel.Enabled = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        /// <summary>Jalankan operasi di thread latar belakang.</summary>
        private async void DoWork(string title, Action<FlasherDevice, CancellationToken> work)
        {
            if (_busy) return;
            if (!_dev.IsOpen)
            {
                MessageBox.Show(this, L.T("status.connectfirst"), L.T("app.title"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken ct = _cts.Token;
            SetBusy(true);
            SetStatus(title + "...");
            Log("");
            Log("=== " + title + " ===");

            try
            {
                await Task.Run(() => work(_dev, ct), ct);
            }
            catch (OperationCanceledException)
            {
                Log(L.T("log.cancelled"));
                if (_dev.IsOpen)
                {
                    try { _dev.Sync(1500); } catch { }
                }
                SetStatusKey("prog.cancelled.status");
                pb.Value = 0;
            }
            catch (Exception ex)
            {
                Log(L.F("log.error", ex.Message));
                SetProgress(0, L.F("prog.failed", ex.Message));
                MessageBox.Show(this, ex.Message, L.F("title.failed", title),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_cts != null) { _cts.Dispose(); _cts = null; }
                SetBusy(false);
            }
        }

        private void UpdateChipUi(ChipInfo info)
        {
            _chip = info;
            if (info == null) return;

            lblChipName.Text = info.Detected ? info.Name : L.T("lbl.nodetected");
            lblChipName.ForeColor = info.Detected ? Color.DarkGreen : Color.Firebrick;
            lblJedec.Text = L.T("lbl.jedec") + info.Jedec;
            lblSize.Text = L.T("lbl.size") + info.SizeHuman +
                           (info.SizeText.Length > 0 ? "  (" + info.SizeText + ")" : "");
            lblStatus.Text = L.T("lbl.status") + info.Status1;
        }

        private static string Bytes(long n)
        {
            if (n >= 1024L * 1024L)
                return (n / (1024.0 * 1024.0)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
            if (n >= 1024L)
                return (n / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            return n + " byte";
        }

        private DateTime _stageStart = DateTime.UtcNow;
        private string _stageName;

        private void ProgressRange(long done, long total, double from, double to, string stageKey)
        {
            if (_stageName != stageKey)
            {
                _stageName = stageKey;
                _stageStart = DateTime.UtcNow;
            }

            double frac = total > 0 ? (double)done / total : 1.0;
            double pct = from + (to - from) * frac;

            double seconds = (DateTime.UtcNow - _stageStart).TotalSeconds;
            string text = L.F("prog.stage", L.T(stageKey), frac * 100.0, Bytes(done), Bytes(total));

            if (seconds > 0.4 && done > 0 && done < total)
            {
                double rate = done / seconds;
                double left = (total - done) / rate;
                if (left >= 0 && left < 86400)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(left);
                    string eta = ts.TotalMinutes >= 1.0
                        ? ((int)ts.TotalMinutes) + ":" + ts.Seconds.ToString("00")
                        : ts.Seconds + "s";
                    text += "  " + L.F("prog.eta", eta);
                }
            }

            Ui(() => SetProgress(pct, text));
        }

        /* ------------------------------------------------------------------
         *  event handler
         * ---------------------------------------------------------------- */

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (_dev.IsOpen)
            {
                string old = _dev.PortName;
                _dev.Close();
                UpdateConnUi();
                Log(L.F("log.closed", old));
                SetBusy(false);
                return;
            }

            string port = cmbPort.SelectedItem as string;
            if (string.IsNullOrEmpty(port))
            {
                MessageBox.Show(this, L.T("msg.selectport"), L.T("title.info"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                Log(L.F("log.opening", port));
                _dev.Open(port);
                UpdateConnUi();
                Log(L.F("log.connected", port));
            }
            catch (Exception ex)
            {
                Log(L.F("log.openfail", port, ex.Message));
                MessageBox.Show(this, L.F("msg.connectfail", port, ex.Message), L.T("app.title"),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                _dev.Close();
                UpdateConnUi();
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
                SetBusy(false);
            }

            BtnInfo_Click(null, EventArgs.Empty);
        }

        private void BtnInfo_Click(object sender, EventArgs e)
        {
            DoWork(L.T("op.identify"), (dev, ct) =>
            {
                ChipInfo info = dev.GetInfo(ReportLog);
                Ui(() => UpdateChipUi(info));
                if (info.Detected)
                    Ui(() => SetProgress(100, L.F("prog.chip", info.Name, info.SizeHuman)));
                else
                    Ui(() => SetStatusKey("prog.nochip"));
            });
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = L.T("grp.file");
                ofd.Filter = "BIN (*.bin)|*.bin|ROM (*.rom)|*.rom|*.*|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                LoadFile(ofd.FileName);
            }
        }

        /// <summary>Deteksi file Intel HEX ('\'':'' + digit hex di awal baris).</summary>
        private static bool LooksLikeIntelHex(byte[] data)
        {
            if (data == null || data.Length < 11) return false;
            if (data[0] != (byte)':') return false;
            for (int i = 1; i < 11; i++)
            {
                byte b = data[i];
                bool hex = (b >= '0' && b <= '9') || (b >= 'a' && b <= 'f') || (b >= 'A' && b <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        /// <summary>Muat file BIOS (dipakai tombol 'Pilih File' dan drag &amp; drop).</summary>
        private void LoadFile(string path)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length == 0) throw new Exception(L.T("msg.badfile.empty"));
                if (fi.Length > 32L * 1024L * 1024L) throw new Exception(L.T("msg.badfile.big"));

                byte[] data = File.ReadAllBytes(path);
                if (LooksLikeIntelHex(data))
                    throw new Exception(L.T("msg.badfile.hex"));

                _fileData = data;
                _filePath = path;
                UpdateFileUi();
                Log(L.F("log.filesel", path, Bytes(data.Length)));
                SetStatusKey("prog.file.ready");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.F("msg.badfile", ex.Message), L.T("title.error"),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /* ------------------------------------------------------------------
         *  drag & drop file .bin
         * ---------------------------------------------------------------- */

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (_busy || e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length != 1 || !File.Exists(files[0])) return;

            string ext = Path.GetExtension(files[0]).ToLowerInvariant();
            if (ext == ".bin" || ext == ".rom" || ext == ".img" || ext == ".fd" || ext == "")
                e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (_busy || e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;
            LoadFile(files[0]);
        }

        /* ------------------------------------------------------------------
         *  simpan log + tentang
         * ---------------------------------------------------------------- */

        private void BtnSaveLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = L.T("btn.savelog");
                sfd.Filter = "TXT (*.txt)|*.txt|LOG (*.log)|*.log|*.*|*.*";
                sfd.FileName = "biosflasher-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".txt";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.WriteAllText(sfd.FileName, txtLog.Text);
                    MessageBox.Show(this, L.F("log.exported", sfd.FileName), L.T("title.info"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, L.T("title.error"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            string version = "1.0.0";
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null) version = v.ToString(3);
            }
            catch { }

            MessageBox.Show(this, L.F("about.text", version), L.T("btn.about"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnWrite_Click(object sender, EventArgs e)
        {
            if (_fileData == null)
            {
                MessageBox.Show(this, L.T("msg.nofile"), L.T("title.info"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ask = L.F("msg.confirm.write", Bytes(_fileData.Length));
            if (MessageBox.Show(this, ask, L.T("title.confirm.write"), MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                != DialogResult.Yes) return;

            byte[] data = _fileData;
            DoWork(L.T("op.write"), (dev, ct) =>
            {
                ReportLog(L.T("log.unprotect"));
                dev.Unprotect(ReportLog);

                int sectors = (data.Length + 4095) / 4096;
                ReportLog(L.F("log.erasecount", sectors, Bytes(sectors * 4096L)));
                for (int s = 0; s < sectors; s += 64)
                {
                    ct.ThrowIfCancellationRequested();
                    int n = Math.Min(64, sectors - s);
                    dev.Erase((uint)(s * 4096), n, false, ReportLog);
                    int done = s + n;
                    Ui(() =>
                    {
                        pb.Value = (int)Math.Round(25.0 * done / sectors);
                        lblProg.Text = L.F("prog.erase.n", done, sectors);
                    });
                }

                /* blank check: pastikan hasil erase benar-benar 0xFF */
                ct.ThrowIfCancellationRequested();
                ReportLog(L.T("log.blank.check"));
                uint badFirst, badCount;
                if (dev.TryBlankCheck(0, (uint)data.Length, out badFirst, out badCount, ReportLog))
                {
                    if (badCount == 0) ReportLog(L.T("log.blank.ok"));
                    else ReportLog(L.F("log.blank.fail", badCount, "0x" + badFirst.ToString("X")));
                }
                else
                {
                    ReportLog(L.T("log.blank.old"));
                }

                /* tulis: mode biner (cepat) dengan fallback hex */
                ReportLog(L.T("log.wbin"));
                bool binary = dev.TryWriteFlashBinary(0, data,
                    (d, t) => ProgressRange(d, t, 25, 70, "prog.write"), ct, ReportLog);

                if (!binary)
                {
                    ReportLog(L.T("log.wbin.old"));
                    ReportLog(L.T("log.writing"));
                    ReportLog(dev.WriteFlash(0, data,
                        (d, t) => ProgressRange(d, t, 25, 70, "prog.write"), ct, ReportLog));
                }

                /* verifikasi: CRC32 (cepat), fallback byte-per-byte */
                ReportLog(L.T("log.crc.check"));
                uint fileCrc = Crc32.Compute(data);
                uint chipCrc;
                if (dev.TryReadCrc(0, (uint)data.Length, out chipCrc, ReportLog))
                {
                    if (chipCrc != fileCrc)
                    {
                        throw new DeviceException(L.F("log.crc.mismatch",
                            fileCrc.ToString("X8"), chipCrc.ToString("X8")));
                    }
                    ReportLog(L.F("log.crc.match", fileCrc.ToString("X8")));
                }
                else
                {
                    ReportLog(L.T("log.crc.old"));
                    ReportLog(dev.VerifyFlash(0, data,
                        (d, t) => ProgressRange(d, t, 70, 100, "prog.verify"), ct, ReportLog));
                }

                Ui(() => SetProgress(100, L.T("prog.done.write")));
            });
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            if (_fileData == null)
            {
                MessageBox.Show(this, L.T("msg.nofile"), L.T("title.info"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            byte[] data = _fileData;
            DoWork(L.T("op.verify"), (dev, ct) =>
            {
                string r = dev.VerifyFlash(0, data,
                    (d, t) => ProgressRange(d, t, 0, 100, "prog.verify"), ct, ReportLog);
                ReportLog(r);
                Ui(() => SetProgress(100, L.T("prog.done.verify")));
            });
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            if (!_chip.Detected || _chip.SizeBytes <= 0)
            {
                MessageBox.Show(this, L.T("msg.needid"), L.T("title.info"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = L.T("btn.backup");
                sfd.Filter = "BIN (*.bin)|*.bin|*.*|*.*";
                sfd.FileName = _chip.Name.Replace(' ', '_') + "_backup.bin";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                string path = sfd.FileName;
                uint size = (uint)_chip.SizeBytes;

                DoWork(L.T("op.backup"), (dev, ct) =>
                {
                    ReportLog(L.F("log.reading", Bytes(size)));
                    byte[] data = dev.ReadFlash(0, size,
                        (d, t) => ProgressRange(d, t, 0, 95, "prog.read"), ct, ReportLog);

                    Ui(() => SetProgress(97, L.T("log.saving")));
                    File.WriteAllBytes(path, data);
                    ReportLog(L.F("log.saved", path, Bytes(data.Length)));
                    Ui(() => SetProgress(100, L.F("prog.done.backup", Path.GetFileName(path))));
                });
            }
        }

        private void BtnErase_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, L.T("msg.confirm.erase"), L.T("title.confirm.erase"),
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            DoWork(L.T("op.erase"), (dev, ct) =>
            {
                dev.Unprotect(ReportLog);
                Ui(() =>
                {
                    pb.Value = 20;
                    lblProg.Text = L.T("prog.eraseall");
                });
                string r = dev.ChipErase(ReportLog);
                ReportLog(r);
                Ui(() => SetProgress(100, L.T("prog.done.erase")));
            });
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                Log(L.T("log.cancelreq"));
                _cts.Cancel();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy)
            {
                if (MessageBox.Show(this, L.T("msg.confirm.close"), L.T("title.close"),
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                if (_cts != null) _cts.Cancel();
            }
            _dev.Close();
        }
    }
}
