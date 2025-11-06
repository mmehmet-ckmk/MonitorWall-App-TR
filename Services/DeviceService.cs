using MonitorWall.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorWall.App.Services
{
    public class DeviceService
    {
        private readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonitorWall.App", "devices.json");

        private List<DeviceInfo> _cache = new();

        public DeviceService()
        {
            ReloadFromDisk();
        }

        /// <summary>devices.json dosyasını yeniden okur ve belleğe alır.</summary>
        public void ReloadFromDisk()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var list = JsonSerializer.Deserialize<List<DeviceInfo>>(json);
                    _cache = list ?? new List<DeviceInfo>();
                }
                else
                {
                    _cache = new List<DeviceInfo>();
                }
            }
            catch
            {
                _cache = new List<DeviceInfo>();
            }
        }

        public IReadOnlyList<DeviceInfo> Tum() => _cache;

        public void Kaydet()
        {
            try
            {
                var dir = Path.GetDirectoryName(_path)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, json);
            }
            catch { /* sessiz geç */ }
        }

        public void Ekle(DeviceInfo d)
        {
            _cache.Add(d);
            Kaydet();
        }

        public void Sil(Guid id)
        {
            _cache.RemoveAll(x => x.Id == id);
            Kaydet();
        }

        public void Guncelle(DeviceInfo d)
        {
            var i = _cache.FindIndex(x => x.Id == d.Id);
            if (i >= 0) _cache[i] = d;
            Kaydet();
        }

        public void DisariAktar(string file)
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }

        public void IceAktar(string file)
        {
            var txt = File.ReadAllText(file);
            var list = JsonSerializer.Deserialize<List<DeviceInfo>>(txt);
            if (list != null)
            {
                _cache.Clear();
                _cache.AddRange(list);
                Kaydet();
            }
        }

        /// <summary>
        /// Basit durum tespiti: RTSP (554) ve SDK portu (vars. 37777) için TCP bağlantı dener.
        /// SDK yoksa tahmini tür/model doldurur.
        /// </summary>
        public async Task ProbeAsync(DeviceInfo d, int timeoutMs = 1500, CancellationToken ct = default)
        {
            async Task<bool> TryConnect(string host, int port)
            {
                try
                {
                    using var tcp = new TcpClient();
                    var task = tcp.ConnectAsync(host, port);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(timeoutMs);
                    var done = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token));
                    return done == task && tcp.Connected;
                }
                catch { return false; }
            }

            var rtspOk = await TryConnect(d.Ip, d.RtspPort);
            var sdkOk = await TryConnect(d.Ip, d.Port);

            d.Online = rtspOk || sdkOk;

            if (sdkOk && (d.CihazTuru == "N/A" || string.IsNullOrWhiteSpace(d.CihazTuru)))
                d.CihazTuru = "NVR (tahmini)";
            if (rtspOk && (d.CihazTuru == "N/A" || string.IsNullOrWhiteSpace(d.CihazTuru)))
                d.CihazTuru = "IP Cam (tahmini)";

            if (string.IsNullOrWhiteSpace(d.Model) || d.Model == "N/A")
                d.Model = sdkOk ? "Dahua (tahmini)" : d.Model;

            if (string.IsNullOrWhiteSpace(d.KanalSayisi) || d.KanalSayisi == "N/A")
                d.KanalSayisi = sdkOk ? "16" : d.KanalSayisi;

            Guncelle(d);
        }
    }
}
