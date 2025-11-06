using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using MonitorWall.App.Models;
using MonitorWall.App.Services;

namespace MonitorWall.App.UI.Controls
{
    public class LiveGridControl : UserControl
    {
        private TableLayoutPanel? _table;
        private readonly List<Panel> _cells = new();
        private readonly List<VideoView> _views = new();
        private readonly List<MediaPlayer> _players = new();
        private readonly List<Media?> _medias = new();
        private readonly List<string?> _lastUrls = new();
        private readonly List<Panel> _statusDots = new();     // yeşil/kırmızı LED

        private LibVLC _lib = null!;
        private int _cellCount = 16;
        private Label _watermark = null!;

        // Zoom
        private bool _isZoomed = false;
        private int _zoomIndex = -1;
        private int _savedCellCount = 16;

        // Dahua SDK (kullanıyorsan StopAll için temizleriz)
        private DahuaSdk.Session? _sdkSession;

        // Sağ tık menüsü
        private readonly ContextMenuStrip _ctx = new();

        // Basit otomatik tekrar deneme (RTSP)
        private readonly Timer _retryTimer = new() { Interval = 3000 }; // 3 sn
        private readonly HashSet<int> _needRetry = new();

        public LiveGridControl()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(33, 36, 38);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.StandardClick |
                     ControlStyles.StandardDoubleClick, true);

            Core.Initialize();
            _lib = new LibVLC();

            BuildContextMenu();
            BuildWatermark();
            CreateOrExpandCells(NeededTotal(_cellCount));
            BuildTableForCount(_cellCount);

            _retryTimer.Tick += (_, __) => TryAutoReconnect();
            Resize += (_, __) => _table?.PerformLayout();
        }

        // ---------- Genel API ----------
        public void SetSplit(int cells)
        {
            _cellCount = Math.Max(1, cells);
            if (_isZoomed) ExitZoom();

            CreateOrExpandCells(NeededTotal(_cellCount));
            BuildTableForCount(_cellCount);
        }

        // RTSP ile oynat (çoklu)
        public void PlayMany(IEnumerable<string> rtspUrls)
        {
            StopSdk(); // SDK açıksa kapat

            var list = rtspUrls.ToList();
            int count = Math.Min(list.Count, _players.Count);

            for (int i = 0; i < count; i++)
            {
                PlayOneRtsp(i, list[i]);
            }
        }

        // SDK ile cihaz: grid’i doldur
        public void PlayDeviceViaSdk(DeviceInfo dev)
        {
            try
            {
                StopAll();
                _sdkSession = DahuaSdk.Session.Login(dev.Ip, dev.Port, dev.Kullanici ?? "admin", dev.Sifre ?? "");

                int chCount = Math.Max(1, _sdkSession.ChannelCount);
                int capacity = BestCapacity(chCount);
                SetSplit(capacity);

                for (int i = 0; i < _cells.Count; i++) _ = _cells[i].Handle; // hwnd hazırla

                int playN = Math.Min(chCount, capacity);
                for (int i = 0; i < playN; i++)
                {
                    var hwnd = _cells[i].Handle;
                    _sdkSession.StartPreviewTo(hwnd, i, 0);
                    SetStatus(i, true);
                }
                _watermark.Text = $"SDK: {dev.Ad} ({playN}/{chCount})";
            }
            catch (Exception ex)
            {
                MessageBox.Show("SDK canlı izleme başlatılamadı: " + ex.Message,
                    "Dahua SDK", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopSdk();
            }
        }

        public void StopAll()
        {
            // RTSP
            for (int i = 0; i < _players.Count; i++)
            {
                try { _players[i].Stop(); } catch { }
                _medias.ElementAtOrDefault(i)?.Dispose();
                _medias[i] = null;
                _lastUrls[i] = null;
                SetStatus(i, false);
            }
            _needRetry.Clear();

            // SDK
            StopSdk();
        }

        protected override void Dispose(bool disposing)
        {
            StopAll();
            foreach (var p in _players) p.Dispose();
            foreach (var v in _views) v.Dispose();
            _table?.Dispose();
            _lib?.Dispose();
            _retryTimer.Dispose();
            base.Dispose(disposing);
        }

        // ---------- İç yardımcılar ----------
        private static int NeededTotal(int cells)
        {
            int side = (int)Math.Ceiling(Math.Sqrt(Math.Max(1, cells)));
            return side * side;
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

        private void BuildWatermark()
        {
            _watermark = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(80, 0, 0, 0),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(6, 3, 6, 3),
                Visible = true
            };
            Controls.Add(_watermark);
            _watermark.BringToFront();
            _watermark.Location = new Point(6, 6);
        }

        private void BuildContextMenu()
        {
            var miStop = new ToolStripMenuItem("Bu kanalı durdur", null, (_, __) => Ctx_StopOne());
            var miStopAll = new ToolStripMenuItem("Tümünü durdur", null, (_, __) => StopAll());
            var miRestart = new ToolStripMenuItem("Yeniden başlat", null, (_, __) => Ctx_RestartOne());
            var miCopy = new ToolStripMenuItem("RTSP URL kopyala", null, (_, __) => Ctx_CopyUrl());
            var miShot = new ToolStripMenuItem("Snapshot al", null, (_, __) => Ctx_Snapshot());

            _ctx.Items.AddRange(new ToolStripItem[] { miStop, miStopAll, miRestart, new ToolStripSeparator(), miCopy, miShot });
            _ctx.Opening += (_, e) =>
            {
                int idx = _ctx.Tag as int? ?? -1;
                bool hasUrl = idx >= 0 && idx < _lastUrls.Count && !string.IsNullOrWhiteSpace(_lastUrls[idx]);
                miRestart.Enabled = hasUrl;
                miCopy.Enabled = hasUrl;
                miShot.Enabled = (idx >= 0 && idx < _players.Count);
            };
        }

        private void HookDoubleClickAndContext(Control ctrl, int idx)
        {
            // Çift tık zoom
            ctrl.DoubleClick += (s, e) => ToggleZoom(idx);
            ctrl.MouseDoubleClick += (s, e) => { if (e.Button == MouseButtons.Left) ToggleZoom(idx); };
            ctrl.MouseDown += (s, e) =>
            {
                // Sağ tık menüsü
                if (e.Button == MouseButtons.Right)
                {
                    _ctx.Tag = idx;
                    _ctx.Show(ctrl, e.Location);
                }
                // DoubleClick garanti
                if (e.Button == MouseButtons.Left && e.Clicks == 2)
                    ToggleZoom(idx);
            };
        }

        private void CreateOrExpandCells(int totalNeeded)
        {
            while (_cells.Count < totalNeeded)
            {
                int idx = _cells.Count;

                // Hücre paneli
                var cell = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    BackColor = Color.Black,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // Video
                var view = new VideoView { Dock = DockStyle.Fill, BackColor = Color.Black };
                var player = new MediaPlayer(_lib);
                view.MediaPlayer = player;
                cell.Controls.Add(view);

                // Durum LED (sağ üst)
                var dot = new Panel
                {
                    Size = new Size(12, 12),
                    BackColor = Color.Red,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(cell.Width - 14, 2)
                };
                dot.BringToFront();
                cell.Controls.Add(dot);
                cell.Resize += (_, __) => dot.Location = new Point(cell.Width - 14, 2);

                // Olaylar
                HookDoubleClickAndContext(cell, idx);
                HookDoubleClickAndContext(view, idx);

                // RTSP durum olayları
                int capIdx = idx;
                player.EncounteredError += (_, __) => { BeginInvoke(new Action(() => { SetStatus(capIdx, false); MarkRetry(capIdx); })); };
                player.Stopped += (_, __) => { BeginInvoke(new Action(() => { SetStatus(capIdx, false); })); };
                player.Playing += (_, __) => { BeginInvoke(new Action(() => { SetStatus(capIdx, true); })); };

                _cells.Add(cell);
                _statusDots.Add(dot);
                _views.Add(view);
                _players.Add(player);
                _medias.Add(null);
                _lastUrls.Add(null);
            }
        }

        private void DetachAllCellsFromCurrentTable()
        {
            if (_table == null) return;
            foreach (var cell in _cells)
            {
                if (cell.Parent == _table)
                    _table.Controls.Remove(cell);
            }
        }

        private void BuildTableForCount(int cells)
        {
            SuspendLayout();

            if (_table != null)
            {
                DetachAllCellsFromCurrentTable();
                Controls.Remove(_table);
                _table.Dispose();
                _table = null;
            }

            int total = NeededTotal(cells);
            CreateOrExpandCells(total);

            int side = (int)Math.Ceiling(Math.Sqrt(cells));
            _table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = BackColor,
                ColumnCount = side,
                RowCount = side
            };
            _table.SuspendLayout();
            _table.ColumnStyles.Clear();
            _table.RowStyles.Clear();
            for (int i = 0; i < side; i++)
            {
                _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / side));
                _table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / side));
            }

            for (int i = 0; i < total; i++)
            {
                int r = i / side;
                int c = i % side;
                _cells[i].Visible = true;
                _table.Controls.Add(_cells[i], c, r);
            }

            Controls.Add(_table);
            _table.ResumeLayout(true);

            _watermark.Text = $"{cells} hücre";
            _watermark.BringToFront();
            _watermark.Location = new Point(6, 6);

            ResumeLayout(true);
            Invalidate();
            Update();
        }

        // ---------- Zoom ----------
        private void ToggleZoom(int idx)
        {
            if (!_isZoomed) EnterZoom(idx);
            else if (_zoomIndex == idx) ExitZoom();
            else { ExitZoom(); EnterZoom(idx); }
        }

        private void EnterZoom(int idx)
        {
            _isZoomed = true;
            _zoomIndex = idx;
            _savedCellCount = _cellCount;

            SuspendLayout();

            if (_table != null)
            {
                DetachAllCellsFromCurrentTable();
                Controls.Remove(_table);
                _table.Dispose();
                _table = null;
            }

            _table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = BackColor,
                ColumnCount = 1,
                RowCount = 1
            };
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (int i = 0; i < _cells.Count; i++)
                _cells[i].Visible = (i == idx);

            _table.Controls.Add(_cells[idx], 0, 0);
            Controls.Add(_table);

            _watermark.Text = "Zoom (çift tıkla geri dön)";
            _watermark.BringToFront();

            ResumeLayout(true);
            Invalidate();
            Update();
        }

        private void ExitZoom()
        {
            if (!_isZoomed) return;

            _isZoomed = false;
            int restore = _savedCellCount > 0 ? _savedCellCount : _cellCount;

            for (int i = 0; i < _cells.Count; i++)
                _cells[i].Visible = true;

            BuildTableForCount(restore);
        }

        // ---------- Sağ tık eylemleri ----------
        private void Ctx_StopOne()
        {
            int idx = _ctx.Tag as int? ?? -1;
            if (idx < 0 || idx >= _players.Count) return;

            try { _players[idx].Stop(); } catch { }
            _medias[idx]?.Dispose();
            _medias[idx] = null;
            _lastUrls[idx] = null;
            SetStatus(idx, false);
        }

        private void Ctx_RestartOne()
        {
            int idx = _ctx.Tag as int? ?? -1;
            if (idx < 0 || idx >= _players.Count) return;

            var url = _lastUrls[idx];
            if (string.IsNullOrWhiteSpace(url)) return;

            PlayOneRtsp(idx, url!);
        }

        private void Ctx_CopyUrl()
        {
            int idx = _ctx.Tag as int? ?? -1;
            if (idx < 0 || idx >= _lastUrls.Count) return;
            var url = _lastUrls[idx];
            if (string.IsNullOrWhiteSpace(url)) return;

            try { Clipboard.SetText(url!); } catch { }
        }

        private void Ctx_Snapshot()
        {
            int idx = _ctx.Tag as int? ?? -1;
            if (idx < 0 || idx >= _players.Count) return;

            try
            {
                var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var name = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}_{idx + 1}.png";
                var path = Path.Combine(pictures, name);

                // 0,0 -> orijinal boy
                bool ok = _players[idx].TakeSnapshot(0u, path, 0u, 0u);
                MessageBox.Show(ok ? $"Kaydedildi:\n{path}" : "Snapshot başarısız.", "Snapshot",
                    MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Snapshot hatası: " + ex.Message);
            }
        }

        // ---------- RTSP helper & status ----------
        private void PlayOneRtsp(int index, string url)
        {
            try
            {
                StopSdk(); // RTSP'e geçiyoruz
                _players[index].Stop();
                _medias[index]?.Dispose();

                var media = new Media(_lib, url, FromType.FromLocation);
                _medias[index] = media;
                _lastUrls[index] = url;

                // Oynat
                _players[index].Play(media);
                // ilk an LED sarı yapmak istersen: SetStatus(index, false);
            }
            catch
            {
                SetStatus(index, false);
                MarkRetry(index);
            }
        }

        private void SetStatus(int idx, bool ok)
        {
            if (idx < 0 || idx >= _statusDots.Count) return;
            _statusDots[idx].BackColor = ok ? Color.LimeGreen : Color.Red;
        }

        private void MarkRetry(int idx)
        {
            if (idx >= 0 && idx < _players.Count)
            {
                _needRetry.Add(idx);
                if (!_retryTimer.Enabled) _retryTimer.Start();
            }
        }

        private void TryAutoReconnect()
        {
            if (_needRetry.Count == 0)
            {
                _retryTimer.Stop();
                return;
            }

            // bir seferde birkaç hücre dene
            var tryNow = _needRetry.ToList();
            _needRetry.Clear();

            foreach (var i in tryNow)
            {
                var url = (i >= 0 && i < _lastUrls.Count) ? _lastUrls[i] : null;
                if (!string.IsNullOrWhiteSpace(url))
                    PlayOneRtsp(i, url!);
            }
        }

        private void StopSdk()
        {
            if (_sdkSession != null)
            {
                try { _sdkSession.Dispose(); } catch { }
                _sdkSession = null;
            }
        }
    }
}
