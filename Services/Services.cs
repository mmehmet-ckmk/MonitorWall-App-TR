using MonitorWall.App.Models;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonitorWall.App.Services
{
    public class ConfigService
    {
        private MonitorWallModel _cache = new();
        private readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonitorWall.App.TR", "ayar.json");

        public MonitorWallModel Al() {
            try {
                if (File.Exists(_path)) {
                    var json = File.ReadAllText(_path);
                    var m = JsonSerializer.Deserialize<MonitorWallModel>(json);
                    if (m != null) _cache = m;
                }
            } catch {}
            return _cache;
        }

        public void Kaydet(MonitorWallModel m) {
            _cache = m;
            try {
                var dir = Path.GetDirectoryName(_path)!;
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(m, new JsonSerializerOptions{ WriteIndented = true });
                File.WriteAllText(_path, json);
            } catch {}
        }
    }
}
