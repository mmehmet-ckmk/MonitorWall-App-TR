using System;

namespace MonitorWall.App.Models
{
    public class DeviceInfo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Ad { get; set; } = "";
        public string Ip { get; set; } = "";
        public int Port { get; set; } = 37777;   // SDK portu
        public int RtspPort { get; set; } = 554; // RTSP portu
        public string Kullanici { get; set; } = "admin";
        public string Sifre { get; set; } = "";
        public string? CihazTuru { get; set; } = "N/A";
        public string? Model { get; set; } = "N/A";
        public string? KanalSayisi { get; set; } = "16";
        public bool Online { get; set; }
        public string SeriNo { get; set; } = "";

        /// <summary>
        /// Dahua için tipik RTSP:
        /// ana akış: subtype=0, alt akış: subtype=1
        /// </summary>
        public string RtspUrl(int channel, int subtype = 0)
        {
            var user = Uri.EscapeDataString(Kullanici ?? "admin");
            var pass = Uri.EscapeDataString(Sifre ?? "");
            int ch = Math.Max(1, channel);
            int st = Math.Max(0, Math.Min(1, subtype));
            int rp = RtspPort > 0 ? RtspPort : 554;

            // Örn: rtsp://admin:pwd@10.6.0.100:554/cam/realmonitor?channel=1&subtype=0
            return $"rtsp://{user}:{pass}@{Ip}:{rp}/cam/realmonitor?channel={ch}&subtype={st}";
        }
        
    }
}
