using System.Drawing;
using System.Collections.Generic;

namespace MonitorWall.App.Models
{
    public class MonitorWallModel
    {
        public string Ad { get; set; } = "Duvar-1";
        public int Sutun { get; set; } = 4;
        public int Satir { get; set; } = 4;
        public List<MonitorWallBlock> Bloklar { get; set; } = new();
    }

    public class MonitorWallBlock
    {
        public string Id { get; set; } = "";
        public string Ad { get; set; } = "";
        public Size TVBoyutu { get; set; } = new Size(2, 2);
        public Rectangle IzgaraDikdortgen { get; set; } = new Rectangle(0, 0, 2, 2);
        public List<int> Ciktilar { get; set; } = new();
    }
}
