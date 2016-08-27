using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScreenDimmer
{
    class ScreenInfo
    {
        private static const int MAX_OPACITY = 90;
        public int MaxOpacity { get { return MAX_OPACITY; } }

        // attributes
        public string name { get { return name; } set { name = value; } }
        public int originX { get { return originX; } set { originX = value; } }
        public int originY { get { return originY; } set { originY = value; } }
        public int resX { get { return resX; } set { resX = value; } }
        public int resY { get { return resY; } set { resY = value; } }
        public int opacity { get { return opacity; } set { opacity = value; } }
        private TranslucentForm form;

        // constructor for when xml file is found
        public ScreenInfo(XElement screen)
        {
            name = screen.Attribute("name").Value;
            originX = Int32.Parse(screen.Attribute("originX").Value);
            originY = Int32.Parse(screen.Attribute("originY").Value);
            resX = Int32.Parse(screen.Attribute("resX").Value);
            resY = Int32.Parse(screen.Attribute("resY").Value);
            opacity = Int32.Parse(screen.Attribute("opacity").Value);

            // check (0 <= opacity <= MAX_OPACITY)
            if (opacity > MAX_OPACITY)
                opacity = MAX_OPACITY;
            else if (opacity < 0)
                opacity = 0;

            form = new TranslucentForm(originX, originY, resX, resY, opacity);
        }

        // constructor for new screen
        public ScreenInfo(string n, int x, int y, int width, int height)
        {
            name = n;
            originX = x;
            originY = y;
            resX = width;
            resY = height;
            opacity = 30;

            // check (0 <= opacity <= MAX_OPACITY)
            if (opacity > MAX_OPACITY)
                opacity = MAX_OPACITY;
            else if (opacity < 0)
                opacity = 0;

            form = new TranslucentForm(originX, originY, resX, resY, opacity);
        }
    }
}
