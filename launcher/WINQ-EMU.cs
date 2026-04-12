using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WINQ_EMU
{
    public class MainForm : Form
    {
        // --- State ---
        string qemuBinDir;
        List<PortForward> portForwards = new List<PortForward>();

        // --- Controls ---
        TabControl tabs;
        // VM tab
        TextBox txtDiskImage, txtIsoImage, txtCores, txtRam;
        ComboBox cmbBootDevice;
        Button btnBrowseDisk, btnCreateDisk, btnBrowseIso, btnClearIso;
        // Display tab
        CheckBox chkVenus;
        TextBox txtHostMem;
        TrackBar sldHostMem;
        Label lblHostMemValue;
        // Devices tab
        ComboBox cmbSound, cmbNetwork;
        DataGridView dgvPorts;
        Button btnAddPort, btnRemovePort;
        // Bottom
        TextBox txtCommandPreview;
        Button btnLaunch, btnSaveBat, btnLoadBat;
        StatusStrip statusBar;
        ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            // Find QEMU binary relative to this exe
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            qemuBinDir = Path.Combine(exeDir, "bin");
            if (!File.Exists(Path.Combine(qemuBinDir, "qemu-system-x86_64.exe")))
            {
                // Try same directory
                if (File.Exists(Path.Combine(exeDir, "qemu-system-x86_64.exe")))
                    qemuBinDir = exeDir;
            }

            InitializeForm();
            InitializeVMTab();
            InitializeDisplayTab();
            InitializeDevicesTab();
            InitializeBottomPanel();
            UpdateCommandPreview();
        }

        void InitializeForm()
        {
            Text = "WINQ-EMU Alpha 5";
            Size = new Size(780, 680);
            MinimumSize = new Size(700, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.FromArgb(245, 245, 248);

            // Try to load icon from several locations
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                string[] iconPaths = new[] {
                    Path.Combine(exeDir, "winq-emu.ico"),
                    Path.Combine(exeDir, "icons", "winq-emu.ico"),
                    Path.Combine(exeDir, "..", "winq-emu.ico"),
                    Path.Combine(exeDir, "..", "icons", "winq-emu.ico")
                };
                foreach (string p in iconPaths)
                {
                    if (File.Exists(p))
                    {
                        Icon = new Icon(p);
                        break;
                    }
                }
            }
            catch { }

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new Point(16, 6);
            tabs.Font = new Font("Segoe UI", 10f);
            Controls.Add(tabs);
        }

        // --- Helpers for building UI ---
        Panel MakeSection(string title, Control parent, int top, int height)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(20, top),
                AutoSize = true
            };
            parent.Controls.Add(lbl);

            var panel = new Panel
            {
                Location = new Point(20, top + 22),
                Size = new Size(parent.Width > 0 ? parent.Width - 50 : 690, height),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(14)
            };
            panel.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using (var pen = new Pen(Color.FromArgb(210, 210, 215)))
                    e.Graphics.DrawRectangle(pen, r);
            };
            parent.Controls.Add(panel);
            return panel;
        }

        Label MakeLabel(string text, Control parent, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y + 2),
                AutoSize = true,
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        TextBox MakeTextBox(Control parent, int x, int y, int width, string text = "")
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 26),
                Text = text,
                BorderStyle = BorderStyle.FixedSingle
            };
            tb.TextChanged += (s, e) => UpdateCommandPreview();
            parent.Controls.Add(tb);
            return tb;
        }

        ComboBox MakeComboBox(Control parent, int x, int y, int width, string[] items, int selected = 0)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cb.Items.AddRange(items);
            if (selected >= 0 && selected < items.Length)
                cb.SelectedIndex = selected;
            cb.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            parent.Controls.Add(cb);
            return cb;
        }

        Button MakeButton(string text, Control parent, int x, int y, int width = 90, EventHandler onClick = null)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 235, 240),
                ForeColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 205);
            if (onClick != null) btn.Click += onClick;
            parent.Controls.Add(btn);
            return btn;
        }

        // --- VM Tab ---
        void InitializeVMTab()
        {
            var page = new TabPage("  VM  ");
            page.BackColor = Color.FromArgb(245, 245, 248);
            page.AutoScroll = true;
            tabs.TabPages.Add(page);

            // Disk Image section
            var secDisk = MakeSection("DISK IMAGE", page, 12, 72);
            MakeLabel("Image:", secDisk, 14, 10);
            txtDiskImage = MakeTextBox(secDisk, 75, 8, 380);
            btnBrowseDisk = MakeButton("Browse...", secDisk, 464, 7, 80, BrowseDisk_Click);
            btnCreateDisk = MakeButton("Create New...", secDisk, 550, 7, 105, CreateDisk_Click);
            MakeLabel("Supports qcow2 and raw formats", secDisk, 75, 38);
            secDisk.Controls[secDisk.Controls.Count - 1].ForeColor = Color.FromArgb(140, 140, 140);
            ((Label)secDisk.Controls[secDisk.Controls.Count - 1]).Font = new Font("Segoe UI", 8.5f);

            // ISO / CD-ROM section
            var secIso = MakeSection("CD-ROM / ISO", page, 112, 44);
            MakeLabel("ISO:", secIso, 14, 10);
            txtIsoImage = MakeTextBox(secIso, 75, 8, 380);
            btnBrowseIso = MakeButton("Browse...", secIso, 464, 7, 80, BrowseIso_Click);
            btnClearIso = MakeButton("Clear", secIso, 550, 7, 60, (s, e) => { txtIsoImage.Text = ""; });

            // Boot Device section
            var secBoot = MakeSection("BOOT", page, 184, 44);
            MakeLabel("Boot device:", secBoot, 14, 10);
            cmbBootDevice = MakeComboBox(secBoot, 110, 8, 180,
                new[] { "Hard Disk (default)", "CD-ROM", "Network (PXE)" }, 0);

            // CPU & RAM section
            var secCpu = MakeSection("CPU & MEMORY", page, 256, 50);
            MakeLabel("CPU cores:", secCpu, 14, 12);
            int defaultCores = Math.Max(1, Environment.ProcessorCount / 2);
            txtCores = MakeTextBox(secCpu, 110, 10, 60, defaultCores.ToString());
            MakeLabel("of " + Environment.ProcessorCount + " available", secCpu, 178, 12);
            ((Label)secCpu.Controls[secCpu.Controls.Count - 1]).ForeColor = Color.FromArgb(140, 140, 140);

            MakeLabel("RAM:", secCpu, 310, 12);
            txtRam = MakeTextBox(secCpu, 360, 10, 60, "8");
            MakeLabel("GB", secCpu, 428, 12);
        }

        // --- Display Tab ---
        void InitializeDisplayTab()
        {
            var page = new TabPage("  Display  ");
            page.BackColor = Color.FromArgb(245, 245, 248);
            tabs.TabPages.Add(page);

            // Venus / GPU section
            var secGpu = MakeSection("GPU ACCELERATION", page, 12, 150);
            chkVenus = new CheckBox
            {
                Text = "Enable Venus Vulkan GPU forwarding",
                Location = new Point(14, 12),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            chkVenus.CheckedChanged += (s, e) =>
            {
                sldHostMem.Enabled = chkVenus.Checked;
                txtHostMem.Enabled = chkVenus.Checked;
                UpdateCommandPreview();
            };
            secGpu.Controls.Add(chkVenus);

            MakeLabel("GPU host memory:", secGpu, 14, 48);
            sldHostMem = new TrackBar
            {
                Location = new Point(155, 42),
                Size = new Size(300, 30),
                Minimum = 1,
                Maximum = 16,
                Value = 4,
                TickFrequency = 1,
                SmallChange = 1,
                LargeChange = 2
            };
            sldHostMem.ValueChanged += (s, e) =>
            {
                txtHostMem.Text = sldHostMem.Value.ToString();
                lblHostMemValue.Text = sldHostMem.Value + " GB";
                UpdateCommandPreview();
            };
            secGpu.Controls.Add(sldHostMem);

            txtHostMem = MakeTextBox(secGpu, 465, 44, 50, "4");
            txtHostMem.Leave += (s, e) =>
            {
                int val;
                if (int.TryParse(txtHostMem.Text, out val))
                {
                    val = Math.Max(1, Math.Min(16, val));
                    sldHostMem.Value = val;
                }
                UpdateCommandPreview();
            };

            lblHostMemValue = MakeLabel("4 GB", secGpu, 523, 46);

            var lblGpuNote = new Label
            {
                Text = "Venus forwards Vulkan calls from the Linux guest to your host GPU.\n" +
                       "Requires a host GPU with Vulkan drivers. Blob resources enable\n" +
                       "efficient shared memory between host and guest.",
                Location = new Point(14, 82),
                AutoSize = true,
                ForeColor = Color.FromArgb(130, 130, 130),
                Font = new Font("Segoe UI", 8.5f)
            };
            secGpu.Controls.Add(lblGpuNote);
        }

        // --- Devices Tab ---
        void InitializeDevicesTab()
        {
            var page = new TabPage("  Devices  ");
            page.BackColor = Color.FromArgb(245, 245, 248);
            page.AutoScroll = true;
            tabs.TabPages.Add(page);

            // Sound section
            var secSound = MakeSection("SOUND", page, 12, 44);
            MakeLabel("Sound device:", secSound, 14, 10);
            cmbSound = MakeComboBox(secSound, 130, 8, 220,
                new[] { "Virtio Sound (recommended)", "Intel HDA", "AC97", "None" }, 0);

            // Network section
            var secNet = MakeSection("NETWORK", page, 84, 44);
            MakeLabel("Network adapter:", secNet, 14, 10);
            cmbNetwork = MakeComboBox(secNet, 145, 8, 220,
                new[] { "VirtIO (recommended)", "Intel E1000", "None" }, 0);

            // Port Forwarding section
            var secPorts = MakeSection("PORT FORWARDING", page, 156, 180);
            dgvPorts = new DataGridView
            {
                Location = new Point(14, 8),
                Size = new Size(540, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(230, 230, 230),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 248, 250),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(248, 248, 250),
                    SelectionForeColor = Color.FromArgb(60, 60, 60)
                }
            };
            dgvPorts.Columns.Add("Protocol", "Protocol");
            dgvPorts.Columns.Add("HostPort", "Host Port");
            dgvPorts.Columns.Add("GuestPort", "Guest Port");
            dgvPorts.Columns[0].FillWeight = 30;
            dgvPorts.Columns[1].FillWeight = 35;
            dgvPorts.Columns[2].FillWeight = 35;
            dgvPorts.CellEndEdit += (s, e) => UpdateCommandPreview();
            secPorts.Controls.Add(dgvPorts);

            // Add default SSH forwarding
            dgvPorts.Rows.Add("tcp", "2223", "22");
            portForwards.Add(new PortForward { Protocol = "tcp", HostPort = "2223", GuestPort = "22" });

            btnAddPort = MakeButton("Add", secPorts, 14, 138, 80, AddPort_Click);
            btnRemovePort = MakeButton("Remove", secPorts, 100, 138, 80, RemovePort_Click);

            var lblPortNote = new Label
            {
                Text = "SSH is pre-configured on port 2223. Connect with: ssh -p 2223 user@localhost",
                Location = new Point(190, 142),
                AutoSize = true,
                ForeColor = Color.FromArgb(130, 130, 130),
                Font = new Font("Segoe UI", 8.5f)
            };
            secPorts.Controls.Add(lblPortNote);
        }

        // --- Bottom Panel ---
        void InitializeBottomPanel()
        {
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 165,
                BackColor = Color.FromArgb(250, 250, 252),
                Padding = new Padding(20, 8, 20, 8)
            };
            bottomPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 225)))
                    e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
            };
            Controls.Add(bottomPanel);

            var lblCmd = new Label
            {
                Text = "COMMAND PREVIEW",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, 8),
                AutoSize = true
            };
            bottomPanel.Controls.Add(lblCmd);

            txtCommandPreview = new TextBox
            {
                Location = new Point(20, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5f),
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.FromArgb(200, 220, 200),
                BorderStyle = BorderStyle.None,
                WordWrap = true
            };
            bottomPanel.Controls.Add(txtCommandPreview);

            // Button row
            var btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(250, 250, 252)
            };
            Controls.Add(btnPanel);

            btnLaunch = new Button
            {
                Text = "Launch VM",
                Size = new Size(130, 36),
                Location = new Point(20, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += Launch_Click;
            btnPanel.Controls.Add(btnLaunch);

            btnSaveBat = MakeButton("Save as .bat", btnPanel, 165, 10, 110, SaveBat_Click);
            btnSaveBat.Height = 30;
            btnLoadBat = MakeButton("Load .bat", btnPanel, 285, 10, 100, LoadBat_Click);
            btnLoadBat.Height = 30;

            statusBar = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusBar.Items.Add(statusLabel);
            statusBar.Items.Add(new ToolStripStatusLabel("WINQ-EMU Alpha 5") {
                Alignment = ToolStripItemAlignment.Right,
                ForeColor = Color.FromArgb(140, 140, 140)
            });
            Controls.Add(statusBar);

            // Fix command preview sizing after layout
            bottomPanel.Resize += (s, e) =>
            {
                txtCommandPreview.Size = new Size(bottomPanel.Width - 40, bottomPanel.Height - 36);
            };
            txtCommandPreview.Size = new Size(bottomPanel.Width - 40, bottomPanel.Height - 36);
        }

        // --- Command Building ---
        List<string> BuildArgs()
        {
            var args = new List<string>();
            args.Add("-machine q35,accel=whpx");
            args.Add("-cpu host");

            int cores;
            if (!int.TryParse(txtCores.Text, out cores) || cores < 1)
                cores = 1;
            args.Add("-smp " + cores);

            int ram;
            if (!int.TryParse(txtRam.Text, out ram) || ram < 1)
                ram = 4;
            args.Add("-m " + ram + "G");

            string disk = txtDiskImage.Text.Trim();
            if (disk.Length > 0)
            {
                string fmt = disk.EndsWith(".qcow2", StringComparison.OrdinalIgnoreCase) ? "qcow2" : "raw";
                args.Add("-drive file=\"" + disk + "\",format=" + fmt + ",if=virtio");
            }

            string iso = txtIsoImage.Text.Trim();
            if (iso.Length > 0)
                args.Add("-cdrom \"" + iso + "\"");

            switch (cmbBootDevice.SelectedIndex)
            {
                case 1: args.Add("-boot d"); break;
                case 2: args.Add("-boot n"); break;
            }

            if (chkVenus.Checked)
            {
                int hostmem;
                if (!int.TryParse(txtHostMem.Text, out hostmem) || hostmem < 1)
                    hostmem = 4;
                args.Add("-device virtio-vga-gl,blob=on,hostmem=" + hostmem + "G,venus=on");
            }
            else
            {
                args.Add("-device virtio-vga-gl");
            }

            args.Add("-display sdl,gl=on");

            switch (cmbSound.SelectedIndex)
            {
                case 0: args.Add("-device virtio-sound-pci"); break;
                case 1: args.Add("-device intel-hda"); args.Add("-device hda-duplex"); break;
                case 2: args.Add("-device AC97"); break;
            }

            string netdev = "";
            switch (cmbNetwork.SelectedIndex)
            {
                case 0: netdev = "virtio-net-pci"; break;
                case 1: netdev = "e1000"; break;
            }
            if (netdev.Length > 0)
            {
                args.Add("-device " + netdev + ",netdev=net0");
                var fwds = BuildPortForwards();
                args.Add("-netdev user,id=net0" + fwds);
            }

            args.Add("-usb");
            args.Add("-device usb-tablet");

            return args;
        }

        string BuildCommand(bool forBatchFile = false)
        {
            string qemu = "\"" + Path.Combine(qemuBinDir, "qemu-system-x86_64w.exe") + "\"";

            var args = BuildArgs();
            string sep = forBatchFile ? " ^\r\n  " : "\r\n  ";
            return qemu + sep + string.Join(sep, args);
        }

        string BuildPortForwards()
        {
            var sb = new StringBuilder();
            foreach (DataGridViewRow row in dgvPorts.Rows)
            {
                string proto = (row.Cells[0].Value ?? "").ToString().Trim();
                string hp = (row.Cells[1].Value ?? "").ToString().Trim();
                string gp = (row.Cells[2].Value ?? "").ToString().Trim();
                if (hp.Length > 0 && gp.Length > 0)
                {
                    if (proto != "tcp" && proto != "udp") proto = "tcp";
                    sb.Append(",hostfwd=" + proto + "::" + hp + "-:" + gp);
                }
            }
            return sb.ToString();
        }

        void UpdateCommandPreview()
        {
            if (txtCommandPreview != null)
                txtCommandPreview.Text = BuildCommand(false);
        }

        // --- Event Handlers ---
        void BrowseDisk_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select VM Disk Image";
                ofd.Filter = "Disk Images|*.qcow2;*.raw;*.img|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtDiskImage.Text = ofd.FileName;
            }
        }

        void CreateDisk_Click(object sender, EventArgs e)
        {
            using (var dlg = new CreateDiskDialog(qemuBinDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtDiskImage.Text = dlg.CreatedPath;
            }
        }

        void BrowseIso_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select ISO Image";
                ofd.Filter = "ISO Images|*.iso|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtIsoImage.Text = ofd.FileName;
            }
        }

        void AddPort_Click(object sender, EventArgs e)
        {
            dgvPorts.Rows.Add("tcp", "", "");
            UpdateCommandPreview();
        }

        void RemovePort_Click(object sender, EventArgs e)
        {
            if (dgvPorts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvPorts.SelectedRows)
                    dgvPorts.Rows.Remove(row);
                UpdateCommandPreview();
            }
        }

        void Launch_Click(object sender, EventArgs e)
        {
            string disk = txtDiskImage.Text.Trim();
            string iso = txtIsoImage.Text.Trim();
            if ((disk.Length == 0 || !File.Exists(disk)) && (iso.Length == 0 || !File.Exists(iso)))
            {
                MessageBox.Show("Please select a disk image or ISO first.", "WINQ-EMU",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string qemuExe = Path.Combine(qemuBinDir, "qemu-system-x86_64w.exe");
                string flatArgs = string.Join(" ", BuildArgs());

                var psi = new ProcessStartInfo
                {
                    FileName = qemuExe,
                    Arguments = flatArgs,
                    UseShellExecute = false,
                    WorkingDirectory = qemuBinDir
                };
                Process.Start(psi);
                statusLabel.Text = "VM launched.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch QEMU:\n" + ex.Message, "WINQ-EMU",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void SaveBat_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Save VM Configuration";
                sfd.Filter = "Batch Files|*.bat";
                sfd.DefaultExt = "bat";
                sfd.FileName = "my-vm.bat";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("@echo off");
                    sb.AppendLine("REM WINQ-EMU Alpha 5 - Generated VM Configuration");
                    sb.AppendLine("REM " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    sb.AppendLine();
                    sb.AppendLine(BuildCommand(true));
                    sb.AppendLine();
                    sb.AppendLine("if errorlevel 1 pause");
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    statusLabel.Text = "Saved: " + Path.GetFileName(sfd.FileName);
                }
            }
        }

        void LoadBat_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Load VM Configuration";
                ofd.Filter = "Batch Files|*.bat|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ParseBatFile(File.ReadAllText(ofd.FileName));
                        statusLabel.Text = "Loaded: " + Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not parse batch file:\n" + ex.Message, "WINQ-EMU",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        void ParseBatFile(string content)
        {
            string cmd = content.Replace("^\r\n", " ").Replace("^\n", " ");
            cmd = Regex.Replace(cmd, @"\s+", " ");

            var diskMatch = Regex.Match(cmd, @"-drive\s+file=""?([^"",]+)""?");
            if (diskMatch.Success) txtDiskImage.Text = diskMatch.Groups[1].Value;

            var isoMatch = Regex.Match(cmd, @"-cdrom\s+""?([^""]+)""?");
            if (isoMatch.Success) txtIsoImage.Text = isoMatch.Groups[1].Value;
            else txtIsoImage.Text = "";

            if (cmd.Contains("-boot d")) cmbBootDevice.SelectedIndex = 1;
            else if (cmd.Contains("-boot n")) cmbBootDevice.SelectedIndex = 2;
            else cmbBootDevice.SelectedIndex = 0;

            var smpMatch = Regex.Match(cmd, @"-smp\s+(\d+)");
            if (smpMatch.Success) txtCores.Text = smpMatch.Groups[1].Value;

            var ramMatch = Regex.Match(cmd, @"-m\s+(\d+)G");
            if (ramMatch.Success) txtRam.Text = ramMatch.Groups[1].Value;

            chkVenus.Checked = cmd.Contains("venus=on");

            var hmMatch = Regex.Match(cmd, @"hostmem=(\d+)G");
            if (hmMatch.Success)
            {
                txtHostMem.Text = hmMatch.Groups[1].Value;
                int hm;
                if (int.TryParse(hmMatch.Groups[1].Value, out hm))
                    sldHostMem.Value = Math.Max(1, Math.Min(16, hm));
            }

            if (cmd.Contains("virtio-sound")) cmbSound.SelectedIndex = 0;
            else if (cmd.Contains("intel-hda") || cmd.Contains("hda-duplex")) cmbSound.SelectedIndex = 1;
            else if (cmd.Contains("AC97")) cmbSound.SelectedIndex = 2;
            else cmbSound.SelectedIndex = 3;

            if (cmd.Contains("virtio-net")) cmbNetwork.SelectedIndex = 0;
            else if (cmd.Contains("e1000")) cmbNetwork.SelectedIndex = 1;
            else cmbNetwork.SelectedIndex = 2;

            dgvPorts.Rows.Clear();
            portForwards.Clear();
            var pfMatches = Regex.Matches(cmd, @"hostfwd=(tcp|udp)::(\d+)-:(\d+)");
            foreach (Match m in pfMatches)
            {
                dgvPorts.Rows.Add(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
                portForwards.Add(new PortForward
                {
                    Protocol = m.Groups[1].Value,
                    HostPort = m.Groups[2].Value,
                    GuestPort = m.Groups[3].Value
                });
            }
            if (portForwards.Count == 0)
            {
                dgvPorts.Rows.Add("tcp", "2223", "22");
                portForwards.Add(new PortForward { Protocol = "tcp", HostPort = "2223", GuestPort = "22" });
            }

            UpdateCommandPreview();
        }
    }

    // --- Create Disk Dialog ---
    public class CreateDiskDialog : Form
    {
        TextBox txtPath, txtSize;
        ComboBox cmbFormat;
        public string CreatedPath { get; private set; }
        string qemuBinDir;

        public CreateDiskDialog(string binDir)
        {
            qemuBinDir = binDir;
            Text = "Create New Disk Image";
            Size = new Size(500, 240);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.FromArgb(245, 245, 248);

            var lblPath = new Label { Text = "Save to:", Location = new Point(20, 22), AutoSize = true };
            txtPath = new TextBox
            {
                Location = new Point(90, 20),
                Size = new Size(280, 26),
                BorderStyle = BorderStyle.FixedSingle
            };
            var btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(380, 19),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 235, 240)
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 205);
            btnBrowse.Click += (s, e) =>
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "QCOW2 Image|*.qcow2|Raw Image|*.raw";
                    sfd.DefaultExt = "qcow2";
                    if (sfd.ShowDialog() == DialogResult.OK)
                        txtPath.Text = sfd.FileName;
                }
            };

            var lblFormat = new Label { Text = "Format:", Location = new Point(20, 62), AutoSize = true };
            cmbFormat = new ComboBox
            {
                Location = new Point(90, 60),
                Size = new Size(120, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFormat.Items.AddRange(new[] { "qcow2", "raw" });
            cmbFormat.SelectedIndex = 0;

            var lblSize = new Label { Text = "Size:", Location = new Point(230, 62), AutoSize = true };
            txtSize = new TextBox
            {
                Location = new Point(275, 60),
                Size = new Size(60, 26),
                Text = "64",
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblGB = new Label { Text = "GB", Location = new Point(342, 62), AutoSize = true };

            var lblNote = new Label
            {
                Text = "qcow2 images grow on demand and only use actual disk space needed.",
                Location = new Point(20, 100),
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 120, 120),
                Font = new Font("Segoe UI", 8.5f)
            };

            var btnCreate = new Button
            {
                Text = "Create",
                Location = new Point(290, 140),
                Size = new Size(90, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += Create_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(390, 140),
                Size = new Size(80, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 235, 240),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 205);

            Controls.AddRange(new Control[] {
                lblPath, txtPath, btnBrowse,
                lblFormat, cmbFormat, lblSize, txtSize, lblGB,
                lblNote, btnCreate, btnCancel
            });

            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        void Create_Click(object sender, EventArgs e)
        {
            string path = txtPath.Text.Trim();
            if (path.Length == 0)
            {
                MessageBox.Show("Please specify a file path.", "Create Disk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int size;
            if (!int.TryParse(txtSize.Text, out size) || size < 1)
            {
                MessageBox.Show("Please enter a valid size in GB.", "Create Disk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string format = cmbFormat.SelectedItem.ToString();
            string qemuImg = Path.Combine(qemuBinDir, "qemu-img.exe");

            if (!File.Exists(qemuImg))
            {
                if (format == "raw")
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Create))
                            fs.SetLength((long)size * 1024 * 1024 * 1024);
                        CreatedPath = path;
                        DialogResult = DialogResult.OK;
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to create image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                MessageBox.Show("qemu-img.exe not found. Cannot create qcow2 images.\nTry raw format instead.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = qemuImg,
                    Arguments = "create -f " + format + " \"" + path + "\" " + size + "G",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                proc.WaitForExit(10000);
                if (proc.ExitCode == 0)
                {
                    CreatedPath = path;
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    string err = proc.StandardError.ReadToEnd();
                    MessageBox.Show("qemu-img failed:\n" + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to run qemu-img:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class PortForward
    {
        public string Protocol = "tcp";
        public string HostPort = "";
        public string GuestPort = "";
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
