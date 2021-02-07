using System;
using System.Collections.Generic;

namespace TarkovMapOverlay
{
    [System.Serializable]
    class SavedSettings
    {
        public double visual_opacity;
        public bool visual_transparency;
        public List<string> customMapList;
        public string currentMapPath;

        public SavedSettings() {
            this.visual_transparency = false;
            this.visual_opacity = 1;
            this.customMapList = new List<string>();
            this.currentMapPath = "";
        }
    }

}
