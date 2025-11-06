using System;
using System.Drawing;
using System.Windows.Forms;
using MonitorWall.App.UI.Controls;     // HomeViewControl
using MonitorWall.App.UI.Pages;        // LiveViewPage, DeviceManagerPage

namespace MonitorWall.App.UI
{
    public class MainForm : Form
    {
        // Üst gezinme çubuğu ve durum çubuğu
        private readonly ToolStrip _navbar = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        private readonly StatusStrip _status = new();
        private readonly ToolStripStatusLabel _lblStat = new("Hazır");

        // İçerik alanı (sayfalar buraya yüklenir)
        private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };

        // Sayfalar
        private readonly HomeViewControl _home = new();
        private readonly LiveViewPage _live = new();
        private readonly DeviceManagerPage _devices = new();

        public MainForm()
        {
            Text = "SmartPSS lite (TR) - Demo";
            MinimumSize = new Size(1100, 680);
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            // Üst navbar
            BuildNavbar();

            // Alt status bar
            _status.Items.Add(_lblStat);

            // Form düzeni
            Controls.Add(_content);
            Controls.Add(_status);
            Controls.Add(_navbar);

            // Varsayılan sayfa
            ShowHome();
            // Ev sayfasındaki "Cihaz Yöneticisi" kutusu tıklanınca aç
            _home.CihazYoneticisiSecildi += () => ShowDevices();

            // Home ekranındaki kutuların olayları
            _home.CanliGorunumSecildi += () => ShowLive();
            _home.KayittanOynatSecildi += () => ShowToast("Kayıttan Oynat (demo)");
            _home.CihazYoneticisiSecildi += () => ShowDevices();
            _home.GunlukSorgulamaSecildi += () => ShowToast("Günlük Sorgulama (demo)");
            _home.OlayYapilandirmasiSecildi += () => ShowToast("Olay Yapılandırması (demo)");
            _home.KullanimKilavuzuSecildi += () => ShowToast("Kullanım Kılavuzu (demo)");
        }

        private void BuildNavbar()
        {
            // Sol logo/başlık
            var lblBrand = new ToolStripLabel("SmartPSS lite")
            {
                ForeColor = Color.SteelBlue,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };

            // Butonlar
            var btnEv = new ToolStripButton("Ev") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            var btnCanli = new ToolStripButton("Canlı Görünüm");
            var btnKayit = new ToolStripButton("Kayıttan Oynat");
            var btnCihaz = new ToolStripButton("Cihaz Yöneticisi");
            var btnAyar = new ToolStripButton("Ayarlar");

            _navbar.Items.Add(lblBrand);
            _navbar.Items.Add(new ToolStripSeparator());
            _navbar.Items.Add(btnEv);
            _navbar.Items.Add(btnCanli);
            _navbar.Items.Add(btnKayit);
            _navbar.Items.Add(btnCihaz);
            _navbar.Items.Add(new ToolStripSeparator());
            _navbar.Items.Add(btnAyar);

            // Click olayları
            btnEv.Click += (_, __) => ShowHome();
            btnCanli.Click += (_, __) => ShowLive();
            btnKayit.Click += (_, __) => ShowToast("Kayıttan Oynat (demo)");
            btnCihaz.Click += (_, __) => ShowDevices();
            btnAyar.Click += (_, __) => ShowToast("Ayarlar (demo)");
        }

        private void ShowHome()
        {
            _content.SuspendLayout();
            _content.Controls.Clear();
            _content.Controls.Add(_home);
            _home.Dock = DockStyle.Fill;
            _content.ResumeLayout();
            _lblStat.Text = "Ev";
        }

        private void ShowLive()
        {
            _content.SuspendLayout();
            _content.Controls.Clear();
            _content.Controls.Add(_live);
            _live.Dock = DockStyle.Fill;

            _live.ReloadDevices();   // <<< BU SATIRI EKLE

            _content.ResumeLayout();
            _lblStat.Text = "Canlı Görünüm";
        }


        private void ShowDevices()
        {
            _content.SuspendLayout();
            _content.Controls.Clear();
            _content.Controls.Add(_devices);
            _devices.Dock = DockStyle.Fill;
            _content.ResumeLayout();
            _lblStat.Text = "Cihaz Yöneticisi";
        }

        // Basit toast bildirimi
        private void ShowToast(string text)
        {
            _lblStat.Text = text;
            using var f = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(35, 35, 35),
                Opacity = 0.92,
                Size = new Size(320, 48),
                TopMost = true
            };
            var lbl = new Label
            {
                Text = text,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            f.Controls.Add(lbl);
            f.Location = PointToScreen(new Point(Width - f.Width - 20, Height - f.Height - 60));
            f.Show();
            var t = new System.Windows.Forms.Timer { Interval = 1600 };
            t.Tick += (s, e) => { t.Stop(); f.Close(); t.Dispose(); };
            t.Start();
        }
    }
}
