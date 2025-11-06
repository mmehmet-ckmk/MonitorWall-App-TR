using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorWall.App.UI.Controls
{
    public class Expander : Panel
    {
        private readonly Panel _header = new() { Height = 32, Dock = DockStyle.Top, Cursor = Cursors.Hand };
        private readonly Label _lbl = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0) };
        private readonly Label _arrow = new() { Dock = DockStyle.Left, Width = 24, TextAlign = ContentAlignment.MiddleCenter };
        public readonly Panel Body = new() { Dock = DockStyle.Fill, Visible = true };

        public string Title { get => _lbl.Text; set => _lbl.Text = value; }
        public bool Expanded { get => Body.Visible; set { Body.Visible = value; _arrow.Text = value ? "▾" : "▸"; } }

        public Expander()
        {
            BackColor = Color.White;
            BorderStyle = BorderStyle.None;
            _header.BackColor = Color.FromArgb(245, 246, 248);
            _header.Controls.Add(_lbl);
            _header.Controls.Add(_arrow);
            Controls.Add(Body);
            Controls.Add(_header);

            _header.Click += (_, __) => Toggle();
            _lbl.Click += (_, __) => Toggle();
            _arrow.Click += (_, __) => Toggle();
            Expanded = true;
        }

        private void Toggle() => Expanded = !Expanded;
    }
}
