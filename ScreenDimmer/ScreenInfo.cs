using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScreenDimmer
{
    class ScreenInfo
    {
        //
        //  <ATTRIBUTES AND CONSTANTS>
        //

        private const int MAX_OPACITY = 90;
        public static int MaxOpacity { get { return MAX_OPACITY; } set { } }
        private const int MIN_ORIGINX = -20000;
        public static int MinOriginX { get { return MIN_ORIGINX; } set { } }
        private const int MAX_ORIGINX = 20000;
        public static int MaxOriginX { get { return MAX_ORIGINX; } set { } }
        private const int MIN_ORIGINY = -10000;
        public static int MinOriginY { get { return MIN_ORIGINY; } set { } }
        private const int MAX_ORIGINY = 10000;
        public static int MaxOriginY { get { return MAX_ORIGINY; } set { } }
        private const int MIN_RES = 1;
        public static int MinRes { get { return MIN_RES; } set { } }
        private const int MAX_RES = 10000;
        public static int MaxRes { get { return MAX_RES; } set { } }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private int originX;
        public int OriginX
        {
            get { return originX; }
            set { originX = value; form.Location = new Point(originX, originY); }
        }

        private int originY;
        public int OriginY
        {
            get { return originY; }
            set { originY = value; form.Location = new Point(originX, originY); }
        }

        private int resX;
        public int ResolutionX
        {
            get { return resX; }
            set { resX = value; form.Width = resX; }
        }

        private int resY;
        public int ResolutionY
        {
            get { return resY; }
            set { resY = value; form.Height = resY; }
        }

        private int opacity;
        public int Opacity
        {
            get { return opacity; }
            set { opacity = value; form.Opacity = opacity; }
        }

        private int screen_index;
        public int ScreenIndex { get { return screen_index; } set { screen_index = value; } }
        private TranslucentForm form;

        //
        //  </ATTRIBUTES AND CONSTANTS>
        //



        //
        //  <CONSTRUCTORS>
        //

        // constructor for when xml file is found
        public ScreenInfo(XElement screen)
        {
            name = screen.Attribute("name").Value;
            originX = Int32.Parse(screen.Attribute("originX").Value);
            originY = Int32.Parse(screen.Attribute("originY").Value);
            resX = Int32.Parse(screen.Attribute("resX").Value);
            resY = Int32.Parse(screen.Attribute("resY").Value);
            opacity = Int32.Parse(screen.Attribute("opacity").Value);
            screen_index = Int32.Parse(screen.Attribute("index").Value);

            // check for values that are not too large or too small
            bool originXcheck = originX < MIN_ORIGINX || originX > MAX_ORIGINX;
            bool originYcheck = originY < MIN_ORIGINY || originY > MAX_ORIGINY;
            bool resXcheck = resX < MIN_RES || resX > MAX_RES;
            bool resYcheck = resY < MIN_RES || resY > MAX_RES;

            if (originXcheck || originYcheck || resXcheck || resYcheck)
                throw new ArgumentOutOfRangeException();

            // check (0 <= opacity <= MAX_OPACITY)
            if (opacity > MAX_OPACITY)
                opacity = MAX_OPACITY;
            else if (opacity < 0)
                opacity = 0;

            form = new TranslucentForm(originX, originY, resX, resY, opacity);
            form.Hide();
        }

        // constructor for new screen
        public ScreenInfo(string n, int index, int x, int y, int width, int height)
        {
            name = n;
            originX = x;
            originY = y;
            resX = width;
            resY = height;
            opacity = 30;
            screen_index = index;

            // check (0 <= opacity <= MAX_OPACITY)
            if (opacity > MAX_OPACITY)
                opacity = MAX_OPACITY;
            else if (opacity < 0)
                opacity = 0;

            form = new TranslucentForm(originX, originY, resX, resY, opacity);
            form.Hide();
        }

        //
        //  </CONSTRUCTORS>
        //



        //
        //  <INTERFACE>
        //

        public void Show() { form.Show(); }
        public void Hide() { form.Hide(); }

        //
        //  </INTERFACE>
        //
    }
}
