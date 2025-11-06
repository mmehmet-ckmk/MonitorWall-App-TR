using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonitorWall.App.Models;
using MonitorWall.App.Services;
using MonitorWall.App.UI.Controls;

namespace MonitorWall.App.UI.Pages
{
    public class LiveViewPage : UserControl
    {
        private const int DefaultLeft = 280;
        private const int MinLeft = 220;
        private const int MaxLeft = 320;

        private readonly SplitContainer _split = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 3,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = MinLeft,
            Panel2MinSize = 0
        };

        private readonly TreeView _tree = new()
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            BorderStyle = BorderStyle.None
        };

        private readonly Panel _right = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(33, 36, 38) };
        private readonly FlowLayoutPanel _topBar = new()
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = Color.FromArgb(245, 247, 250)
        };
        private readonly Label _lblSplit = new() { Text = "Bölünmüş Görünüm:", AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(0, 6, 8, 0) };
        private readonly ComboBox _cbSplit = new() { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };

        private readonly LiveGridControl _grid = new();
        private readonly DeviceService _devices = new();
        private FileSystemWatcher? _watcher;

        public LiveViewPage()
        {
            Dock = DockStyle.Fill;

            var leftHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            leftHost.Controls.Add(_tree);
            _split.Panel1.Controls.Add(leftHost);

            _cbSplit.Items.AddRange(new object[] { "4", "9", "16", "25", "36", "64" });
            _cbSplit.SelectedIndex = 2; // 16
            _cbSplit.SelectedIndexChanged += (_, __) =>
            {
                if (int.TryParse(_cbSplit.SelectedItem?.ToString(), out int n))
                {
                    _grid.StopAll();
                    _grid.SetSplit(n);
                }
            };

            _topBar.Controls.Add(_lblSplit);
            _topBar.Controls.Add(_cbSplit);

            _right.Controls.Add(_grid);
            _right.Controls.Add(_topBar);
            _grid.Dock = DockStyle.Fill;

            _split.Panel2.Controls.Add(_right);
            Controls.Add(_split);

            // Tree olayları
            _tree.BeforeExpand += (_, e) =>
            {
                if (e.Node.Tag is DeviceInfo dev && e.Node.Nodes.Count == 1 && (e.Node.Nodes[0].Text is "…" or "..."))
                    ExpandChannels(e.Node, dev);
            };
            _tree.NodeMouseDoubleClick += Tree_NodeMouseDoubleClick;

            // İlk yükleme
            ReloadDevices();

            // Splitter güvenli ayar
            HandleCreated += (_, __) => SafeSetSplitter(DefaultLeft);
            Load += (_, __) => SafeSetSplitter(DefaultLeft);
            SizeChanged += (_, __) => SafeSetSplitter(DefaultLeft);
            VisibleChanged += (_, __) => { if (Visible) SafeSetSplitter(DefaultLeft); };

            AttachWatcher();
        }

        public void ReloadDevices()
        {
            _devices.ReloadFromDisk();
            LoadDeviceTree();
            SafeSetSplitter(DefaultLeft);
        }

        private void LoadDeviceTree()
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                var root = new TreeNode("Cihazlar");
                _tree.Nodes.Add(root);

                var list = _devices.Tum().ToList();
                if (list.Count == 0)
                    list.Add(new DeviceInfo { Ad = "Örnek NVR", Ip = "10.0.0.10", KanalSayisi = "16", Kullanici = "admin", Sifre = "admin" });

                foreach (var dev in list)
                {
                    var node = new TreeNode($"{dev.Ad} ({dev.Ip})") { Tag = dev };
                    node.Nodes.Add(new TreeNode("…")); // lazy
                    root.Nodes.Add(node);
                }

                root.Expand();
                _tree.SelectedNode = root;
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void ExpandChannels(TreeNode parent, DeviceInfo dev)
        {
            parent.Nodes.Clear();
            int chCount = ParseChannelCount(dev.KanalSayisi, 16);
            for (int i = 1; i <= chCount; i++)
                parent.Nodes.Add(new TreeNode($"Kanal {i}") { Tag = (dev, i) });
        }

        // >>> Cihaza ÇİFT TIK: tüm kanalları grid’e dağıt
        private void Tree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is ValueTuple<DeviceInfo, int> t)
            {
                // Tek kanal RTSP oynamak istersen eski davranış duruyor:
                var (dev, ch) = t;
                _grid.PlayMany(new[] { dev.RtspUrl(ch, 0) });
            }
            else if (e.Node.Tag is DeviceInfo dev)
            {
                // >>> SDK ile Tüm kanallar
                _grid.PlayDeviceViaSdk(dev);
            }
        }


        private void PlayAllChannels(DeviceInfo dev)
        {
            int chCount = ParseChannelCount(dev.KanalSayisi, 16);
            int capacity = BestCapacity(chCount); // 4/9/16/25/36/64’ten uygun olanı seç
            _grid.StopAll();
            _grid.SetSplit(capacity);

            // Kanalları 1..capacity arasında doldur
            var urls = Enumerable.Range(1, Math.Min(chCount, capacity))
                                 .Select(i => dev.RtspUrl(i, 0))
                                 .ToList();

            _grid.PlayMany(urls);
        }

        private static int BestCapacity(int channels)
        {
            if (channels <= 1) return 1;
            if (channels <= 4) return 4;
            if (channels <= 9) return 9;
            if (channels <= 16) return 16;
            if (channels <= 25) return 25;
            if (channels <= 36) return 36;
            return 64;
        }

        private int ParseChannelCount(string? s, int def = 16)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            var first = s.Split('/')[0];
            return int.TryParse(first, out int n) ? Math.Max(1, n) : def;
        }

        // --- Splitter güvenli ayar ---
        private void SafeSetSplitter(int desired)
        {
            if (_split.Width <= 0)
            {
                BeginInvoke(new Action(() => SafeSetSplitter(desired)));
                return;
            }

            int min = Math.Max(0, _split.Panel1MinSize);
            int max = Math.Max(min, _split.Width - _split.Panel2MinSize - _split.SplitterWidth);
            int clamped = Math.Max(min, Math.Min(desired, max));
            if (_split.SplitterDistance != clamped)
                _split.SplitterDistance = clamped;
        }

        private void AttachWatcher()
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorWall.App");
                if (!Directory.Exists(dir)) return;

                _watcher = new FileSystemWatcher(dir, "devices.json")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                void reload(object? s, FileSystemEventArgs e) => BeginInvoke(new Action(ReloadDevices));
                _watcher.Changed += reload;
                _watcher.Created += reload;
                _watcher.Renamed += (_, __) => BeginInvoke(new Action(ReloadDevices));
                _watcher.Deleted += (_, __) => BeginInvoke(new Action(ReloadDevices));
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _watcher?.Dispose(); _watcher = null; }
            base.Dispose(disposing);
        }
    }
}
