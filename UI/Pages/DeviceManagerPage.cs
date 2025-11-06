using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MonitorWall.App.Models;
using MonitorWall.App.Services;
using MonitorWall.App.UI.Dialogs;

namespace MonitorWall.App.UI.Pages
{
    public class DeviceManagerPage : UserControl
    {
        private readonly ToolStrip _bar = new() { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
        private readonly ToolStripButton _btnAdd = new("Ekle");
        private readonly ToolStripButton _btnDel = new("Sil");
        private readonly ToolStripButton _btnImport = new("İçe Aktar");
        private readonly ToolStripButton _btnExport = new("Dışa Aktar");

        // Not: ToolStripTextBox'ta PlaceholderText yok → TextBox üzerinden yapacağız
        private readonly ToolStripTextBox _search = new() { Width = 180 };
        private readonly ToolStripLabel _lblCounts = new("Tüm Aygıtlar: 0   Çevrim İçi Cihazlar: 0");

        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };

        private readonly BindingSource _bs = new();
        private readonly DeviceService _svc = new();

        public DeviceManagerPage()
        {
            Dock = DockStyle.Fill;
            BuildToolbar();
            BuildGrid();

            Controls.Add(_grid);
            Controls.Add(_bar);

            // Placeholder (watermark) çözümü
            TrySetPlaceholder(_search, "Ara…");

            RefreshGrid();
        }

        private void BuildToolbar()
        {
            _bar.Items.Add(new ToolStripLabel("Otomatik Ara..."));
            _bar.Items.Add(new ToolStripSeparator());
            _bar.Items.Add(_btnAdd);
            _bar.Items.Add(_btnDel);
            _bar.Items.Add(_btnImport);
            _bar.Items.Add(_btnExport);
            _bar.Items.Add(new ToolStripSeparator());
            _bar.Items.Add(_search);
            _bar.Items.Add(new ToolStripSeparator());
            _bar.Items.Add(_lblCounts);

            _btnAdd.Click += (_, __) => AddDevice();
            _btnDel.Click += (_, __) => DeleteSelected();
            _btnImport.Click += (_, __) => ImportJson();
            _btnExport.Click += (_, __) => ExportJson();

            _search.TextChanged += (_, __) => ApplyFilter();
        }

        private static void TrySetPlaceholder(ToolStripTextBox host, string text)
        {
            // .NET 6/7/8 TextBox'ta PlaceholderText var; ToolStripTextBox'ta yok.
            // host.TextBox üzerinden deneyelim; yoksa watermark emulasyonu yaparız.
            try
            {
                host.TextBox.PlaceholderText = text; // varsa çalışır
            }
            catch
            {
                // Basit emülasyon
                var tb = host.TextBox;
                tb.ForeColor = Color.Gray;
                tb.Text = text;

                tb.GotFocus += (s, e) =>
                {
                    if (tb.ForeColor == Color.Gray)
                    {
                        tb.Text = "";
                        tb.ForeColor = SystemColors.WindowText;
                    }
                };
                tb.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.ForeColor = Color.Gray;
                        tb.Text = text;
                    }
                };
                tb.TextChanged += (s, e) =>
                {
                    // Filtrelemeyi yalnızca gerçek yazıda tetikle
                    if (tb.ForeColor == Color.Gray) return;
                };
            }
        }

        private void BuildGrid()
        {
            _grid.AutoGenerateColumns = false;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 252);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No.", Width = 50, DataPropertyName = "RowNo" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Adı", DataPropertyName = "Ad", Width = 160 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ip", HeaderText = "IP", DataPropertyName = "Ip", Width = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tur", HeaderText = "Cihaz Türü", DataPropertyName = "CihazTuru", Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Cihaz Modeli", DataPropertyName = "Model", Width = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Port", HeaderText = "Port", DataPropertyName = "Port", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kanal", HeaderText = "Kanal Sayısı", DataPropertyName = "KanalSayisi", Width = 110 });

            var colOnline = new DataGridViewTextBoxColumn { Name = "Online", HeaderText = "Çevrim İçi Durumu", DataPropertyName = "Online", Width = 120 };
            _grid.Columns.Add(colOnline);
            _grid.CellFormatting += (s, e) =>
            {
                if (_grid.Columns[e.ColumnIndex].Name == "Online" && e.Value is bool b)
                {
                    e.Value = b ? "● Çevrimiçi" : "○ Çevrimdışı";
                    e.CellStyle.ForeColor = b ? Color.ForestGreen : Color.Gray;
                }
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SN", HeaderText = "SN", DataPropertyName = "SeriNo", Width = 160 });

            _grid.Columns.Add(MakeButtonCol("Edit", "✎"));
            _grid.Columns.Add(MakeButtonCol("Settings", "⚙"));
            _grid.Columns.Add(MakeButtonCol("Copy", "⧉"));
            _grid.Columns.Add(MakeButtonCol("Delete", "🗑"));
            _grid.Columns.Add(MakeButtonCol("Logout", "⎋"));

            _grid.CellContentClick += Grid_CellContentClick;
        }

        private static DataGridViewButtonColumn MakeButtonCol(string name, string text)
            => new()
            {
                Name = name,
                HeaderText = "İşlem",
                Text = text,
                UseColumnTextForButtonValue = true,
                Width = 48,
                FlatStyle = FlatStyle.Flat
            };

        private void RefreshGrid()
        {
            var data = _svc.Tum().Select((d, idx) => new
            {
                RowNo = idx + 1,
                d.Id,
                d.Ad,
                d.Ip,
                d.CihazTuru,
                d.Model,
                d.Port,
                d.KanalSayisi,
                d.Online,
                SeriNo = d.SeriNo
            }).ToList();

            _bs.DataSource = data;
            _grid.DataSource = _bs;

            var all = _svc.Tum().Count;
            var on = _svc.Tum().Count(x => x.Online);
            _lblCounts.Text = $"Tüm Aygıtlar: {all}   Çevrim İçi Cihazlar: {on}";
        }

        private void ApplyFilter()
        {
            // Placeholder emülasyonu varsa gri yazıyı filtreleme kabul etme
            var tb = _search.TextBox;
            var raw = tb.Text ?? "";
            var q = (tb.ForeColor == Color.Gray) ? "" : raw.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(q))
            {
                RefreshGrid();
                return;
            }

            var filtered = _svc.Tum().Where(d =>
                (d.Ad ?? "").ToLower().Contains(q) ||
                (d.Ip ?? "").ToLower().Contains(q) ||
                (d.CihazTuru ?? "").ToLower().Contains(q) ||
                (d.Model ?? "").ToLower().Contains(q) ||
                (d.SeriNo ?? "").ToLower().Contains(q))
            .Select((d, idx) => new
            {
                RowNo = idx + 1,
                d.Id,
                d.Ad,
                d.Ip,
                d.CihazTuru,
                d.Model,
                d.Port,
                d.KanalSayisi,
                d.Online,
                SeriNo = d.SeriNo
            })
            .ToList();

            _bs.DataSource = filtered;
        }

        private async void AddDevice()
        {
            using var dlg = new DeviceEditDialog();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _svc.Ekle(dlg.Result);
                RefreshGrid();
                await ProbeAndRefresh(dlg.Result);
            }
        }

        private void DeleteSelected()
        {
            if (_grid.CurrentRow == null) return;

            var dataItem = _grid.CurrentRow.DataBoundItem;
            if (dataItem == null) return;

            var prop = dataItem.GetType().GetProperty("Id");
            if (prop == null) return;

            var idObj = prop.GetValue(dataItem);
            if (idObj is not Guid id) return;

            var name = _grid.CurrentRow.Cells["Ad"].Value?.ToString() ?? "(adı yok)";

            if (MessageBox.Show($"“{name}” silinsin mi?", "Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _svc.Sil(id);
                RefreshGrid();
            }
        }

        private void ImportJson()
        {
            using var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
            if (ofd.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _svc.IceAktar(ofd.FileName);
                RefreshGrid();
            }
        }

        private void ExportJson()
        {
            using var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = "devices.json" };
            if (sfd.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _svc.DisariAktar(sfd.FileName);
            }
        }

        private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = _grid.Rows[e.RowIndex];
            var dataItem = row.DataBoundItem;
            if (dataItem == null) return;

            var prop = dataItem.GetType().GetProperty("Id");
            if (prop == null) return;

            var idObj = prop.GetValue(dataItem);
            if (idObj is not Guid id) return;

            var dev = _svc.Tum().FirstOrDefault(x => x.Id == id);
            if (dev == null) return;

            var col = _grid.Columns[e.ColumnIndex].Name;
            switch (col)
            {
                case "Edit":
                    using (var dlg = new DeviceEditDialog(dev))
                    {
                        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                        {
                            _svc.Guncelle(dlg.Result);
                            RefreshGrid();
                            await ProbeAndRefresh(dlg.Result);
                        }
                    }
                    break;

                case "Settings":
                    MessageBox.Show("Ayarlar (SDK ile detaylandırılacak).", "Bilgi");
                    break;

                case "Copy":
                    Clipboard.SetText($"{dev.Ad} {dev.Ip}:{dev.Port}");
                    break;

                case "Delete":
                    if (MessageBox.Show("Bu cihaz silinsin mi?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _svc.Sil(dev.Id);
                        RefreshGrid();
                    }
                    break;

                case "Logout":
                    dev.Sifre = "";
                    _svc.Guncelle(dev);
                    RefreshGrid();
                    MessageBox.Show("Oturum bilgileri temizlendi.");
                    break;
            }
        }

        private async Task ProbeAndRefresh(DeviceInfo d)
        {
            await _svc.ProbeAsync(d);
            RefreshGrid();
        }
    }
}
