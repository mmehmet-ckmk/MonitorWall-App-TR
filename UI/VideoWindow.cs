using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace MonitorWall.App.UI
{
    public class VideoWindow : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _player;
        private VideoView _videoView;
        private Media? _media; // yaşam döngüsünü biz yönetiyoruz

        public VideoWindow(string rtspUrl, string title = "Canlı Görünüm")
        {
            Text = title;
            Width = 960;
            Height = 540;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;

            Core.Initialize();
            _libVLC = new LibVLC();
            _player = new MediaPlayer(_libVLC);

            _videoView = new VideoView { Dock = DockStyle.Fill, MediaPlayer = _player };
            Controls.Add(_videoView);

            Shown += (s, e) =>
            {
                try
                {
                    _media?.Dispose();
                    _media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
                    // PlayAsync yok; senkron Play kullanıyoruz
                    if (!_player.Play(_media))
                        MessageBox.Show("Akış başlatılamadı (Play false döndü).");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Akış başlatılamadı: " + ex.Message);
                }
            };

            FormClosing += (s, e) =>
            {
                try
                {
                    _player?.Stop();
                    _media?.Dispose();
                    _player?.Dispose();
                    _libVLC?.Dispose();
                }
                catch { }
            };
        }
    }
}
