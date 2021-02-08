using System.Windows.Forms;
using System.Collections.Generic;

namespace TarkovMapOverlay
{
    [System.Serializable]
    public class SavedSettings
    {
        public double windowTop;
        public double windowLeft;
        public double windowWidth;
        public double windowHeight;

        public double visual_opacity;
        public bool visual_transparency;
        public List<string> customMapList;
        public string currentMapPath;
        public Keys minimizeKey;
        public MouseButtons minimizeMousebutton;

        public SavedSettings() 
        {
            this.windowTop = 200;
            this.windowLeft = 200;
            this.windowHeight = 300;
            this.windowWidth = 420;

            this.visual_transparency = false;
            this.visual_opacity = 1;
            this.customMapList = new List<string>();
            this.currentMapPath = "";
            this.minimizeKey = Keys.M;
            this.minimizeMousebutton = MouseButtons.None;
        }
    }

}
