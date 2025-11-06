using MonitorWall.App.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorWall.App.UI.Dialogs
{
    public class DeviceEditDialog : Form
    {
        private readonly TextBox txtAd = new() { Width = 220 };
        private readonly TextBox txtIp = new() { Width = 220 };
        private readonly NumericUpDown numPort = new() { Width = 100, Minimum = 1, Maximum = 65535, Value = 37777 };
        private readonly NumericUpDown numRtsp = new() { Width = 100, Minimum = 1, Maximum = 65535, Value = 554 };
        private readonly TextBox txtUser = new() { Width = 220, Text = "admin" };
        private readonly TextBox txtPass = new() { Width = 220, UseSystemPasswordChar = true };

        public DeviceInfo Result { get; private set; }

        public DeviceEditDialog(DeviceInfo? d = null)
        {
            Text = d == null ? "Cihaz Ekle" : "Cihazı Düzenle";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            Padding = new Padding(12);
            Width = 420; Height = 320;

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, AutoSize = true };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            grid.Controls.Add(new Label { Text = "Ad:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
            grid.Controls.Add(txtAd, 1, 0);
            grid.Controls.Add(new Label { Text = "IP:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 1);
            grid.Controls.Add(txtIp, 1, 1);
            grid.Controls.Add(new Label { Text = "Port (SDK):", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 2);
            grid.Controls.Add(numPort, 1, 2);
            grid.Controls.Add(new Label { Text = "RTSP Port:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 3);
            grid.Controls.Add(numRtsp, 1, 3);
            grid.Controls.Add(new Label { Text = "Kullanıcı:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 4);
            grid.Controls.Add(txtUser, 1, 4);
            grid.Controls.Add(new Label { Text = "Şifre:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 5);
            grid.Controls.Add(txtPass, 1, 5);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40 };
            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Width = 100 };
            var btnCancel = new Button { Text = "Vazgeç", DialogResult = DialogResult.Cancel, Width = 100 };
            buttons.Controls.Add(btnOk); buttons.Controls.Add(btnCancel);

            Controls.Add(grid); Controls.Add(buttons);

            if (d != null)
            {
                txtAd.Text = d.Ad;
                txtIp.Text = d.Ip;
                numPort.Value = d.Port;
                numRtsp.Value = d.RtspPort;
                txtUser.Text = d.Kullanici;
                txtPass.Text = d.Sifre;
                Result = d;
            }
            else Result = new DeviceInfo();

            btnOk.Click += (_, __) =>
            {
                Result.Ad = txtAd.Text.Trim();
                Result.Ip = txtIp.Text.Trim();
                Result.Port = (int)numPort.Value;
                Result.RtspPort = (int)numRtsp.Value;
                Result.Kullanici = txtUser.Text.Trim();
                Result.Sifre = txtPass.Text;
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
