using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorWall.App.UI.Controls
{
    public class HomeViewControl : UserControl
    {
        // Tıklama olayları
        public event Action? CanliGorunumSecildi;
        public event Action? KayittanOynatSecildi;
        public event Action? CihazYoneticisiSecildi;
        public event Action? GunlukSorgulamaSecildi;
        public event Action? OlayYapilandirmasiSecildi;
        public event Action? KullanimKilavuzuSecildi;

        // Kutuların koordinatları
        private Rectangle _secCanli;
        private Rectangle _secKayit;
        private Rectangle _secCihaz;
        private Rectangle _secGunluk;
        private Rectangle _secOlay;
        private Rectangle _secKlavuz;

        // Görünmez butonlar (tıklamayı garanti eder)
        private readonly Button _btnCanli = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };
        private readonly Button _btnKayit = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };
        private readonly Button _btnCihaz = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };
        private readonly Button _btnGunluk = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };
        private readonly Button _btnOlay = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };
        private readonly Button _btnKlavuz = new() { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, TabStop = false };

        public HomeViewControl()
        {
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BackColor = Color.WhiteSmoke;
            Padding = new Padding(24, 16, 24, 16);

            // Olaylar
            _btnCanli.Click += (_, __) => CanliGorunumSecildi?.Invoke();
            _btnKayit.Click += (_, __) => KayittanOynatSecildi?.Invoke();
            _btnCihaz.Click += (_, __) => CihazYoneticisiSecildi?.Invoke();
            _btnGunluk.Click += (_, __) => GunlukSorgulamaSecildi?.Invoke();
            _btnOlay.Click += (_, __) => OlayYapilandirmasiSecildi?.Invoke();
            _btnKlavuz.Click += (_, __) => KullanimKilavuzuSecildi?.Invoke();

            foreach (var b in new[] { _btnCanli, _btnKayit, _btnCihaz, _btnGunluk, _btnOlay, _btnKlavuz })
            {
                b.FlatAppearance.BorderSize = 0;
                b.Cursor = Cursors.Hand;
                Controls.Add(b);
                b.BringToFront();
            }

            Resize += (_, __) => LayoutTiles();
            LayoutTiles();
        }

        private void LayoutTiles()
        {
            // Basit konumlandırma (piksel tabanlı)
            _secCanli = new Rectangle(60, 80, 260, 130);
            _secKayit = new Rectangle(360, 80, 260, 130);

            _secCihaz = new Rectangle(60, 340, 180, 100);
            _secGunluk = new Rectangle(260, 340, 180, 100);
            _secOlay = new Rectangle(460, 340, 180, 100);
            _secKlavuz = new Rectangle(660, 340, 180, 100);

            _btnCanli.Bounds = _secCanli;
            _btnKayit.Bounds = _secKayit;
            _btnCihaz.Bounds = _secCihaz;
            _btnGunluk.Bounds = _secGunluk;
            _btnOlay.Bounds = _secOlay;
            _btnKlavuz.Bounds = _secKlavuz;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Başlıklar
            using var fntTitle = new Font(Font.FontFamily, 12, FontStyle.Bold);
            using var fntSub = new Font(Font.FontFamily, 9);
            g.DrawString("SmartPSS lite", fntTitle, Brushes.SteelBlue, 16, 14);
            g.DrawString("Ev", fntTitle, Brushes.DimGray, 150, 14);

            // Büyük kutular
            DrawTile(g, _secCanli, "Canlı Görünüm",
                "Kanal grubunun canlı görüntülenmesi. PTZ, anlık görüntü vb.", Color.SkyBlue);
            DrawTile(g, _secKayit, "Kayıttan Oynat",
                "Video kayıtlarını arayın, oynatın ve dışa aktarın.", Color.LightGreen);

            // Yönetim başlığı ve küçük kutular
            g.DrawString("Yönetim", fntTitle, Brushes.DimGray, 60, 300);
            DrawMiniTile(g, _secCihaz, "Cihaz Yöneticisi");
            DrawMiniTile(g, _secGunluk, "Günlük Sorgulama");
            DrawMiniTile(g, _secOlay, "Olay Yapılandırması");
            DrawMiniTile(g, _secKlavuz, "Kullanım Kılavuzu");
        }

        private static void DrawTile(Graphics g, Rectangle rect, string title, string desc, Color accent)
        {
            using var brBack = new SolidBrush(Color.White);
            using var pen = new Pen(Color.Gainsboro);
            g.FillRectangle(brBack, rect);
            g.DrawRectangle(pen, rect);

            using var fntTitle = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
            using var fntDesc = new Font(SystemFonts.DefaultFont.FontFamily, 8.5f);
            g.DrawString(title, fntTitle, Brushes.Black, rect.Left + 12, rect.Top + 16);
            g.DrawString(desc, fntDesc, Brushes.Gray, new RectangleF(rect.Left + 12, rect.Top + 38, rect.Width - 24, 60));

            using var brAccent = new SolidBrush(accent);
            g.FillEllipse(brAccent, rect.Left + 12, rect.Bottom - 16, 60, 6);
        }

        private static void DrawMiniTile(Graphics g, Rectangle rect, string title)
        {
            using var brBack = new SolidBrush(Color.White);
            using var pen = new Pen(Color.Gainsboro);
            g.FillRectangle(brBack, rect);
            g.DrawRectangle(pen, rect);

            using var fnt = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
            var textPt = new PointF(rect.Left + 12, rect.Top + 36);
            g.DrawString(title, fnt, Brushes.Black, textPt);
        }
    }
}
