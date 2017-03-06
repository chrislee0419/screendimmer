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

        private const double MIN_OPACITY = 0.05;
        public static double MinOpacity { get { return MIN_OPACITY; } set { } }
        private const double MAX_OPACITY = 0.9;
        public static double MaxOpacity { get { return MAX_OPACITY; } set { } }
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

        public int OriginX
        {
            get { return form.Location.X; }
            set
            {
                if (value > MAX_ORIGINX || value < MIN_ORIGINX)
                    throw new ArgumentOutOfRangeException();
                form.Location = new Point(value, form.Location.Y);
            }
        }

        public int OriginY
        {
            get { return form.Location.Y; }
            set
            {
                if (value > MAX_ORIGINY || value < MIN_ORIGINY)
                    throw new ArgumentOutOfRangeException();
                form.Location = new Point(form.Location.X, value);
            }
        }

        public int ResolutionX
        {
            get { return form.Width; }
            set
            {
                if (value > MAX_RES || value < MIN_RES)
                    throw new ArgumentOutOfRangeException();
                form.Width = value;
            }
        }

        public int ResolutionY
        {
            get { return form.Height; }
            set
            {
                if (value > MAX_RES || value < MIN_RES)
                    throw new ArgumentOutOfRangeException();
                form.Height = value;
            }
        }

        public double Opacity
        {
            get { return form.Opacity; }
            set
            {
                if (value < MIN_OPACITY || value > MAX_OPACITY)
                    throw new ArgumentOutOfRangeException();
                form.Opacity = value;
            }
        }
        private bool show_form;
        public bool Show
        {
            get { return show_form; }
            set
            {
                show_form = value;
                if (enabled && show_form) form.Show();
                else form.Hide();
            }
        }
        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (enabled && show_form) form.Show();
                else form.Hide();
            }
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
            name = screen.Element("name").Value;
            int originX = Int32.Parse(screen.Element("originX").Value);
            int originY = Int32.Parse(screen.Element("originY").Value);
            int resX = Int32.Parse(screen.Element("resX").Value);
            int resY = Int32.Parse(screen.Element("resY").Value);
            double opacity = Double.Parse(screen.Element("opacity").Value);
            screen_index = Int32.Parse(screen.Element("index").Value);
            show_form = false;
            enabled = false;

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
        public ScreenInfo(string n, int index, int x, int y, int width, int height, double opacity)
        {
            name = n;
            screen_index = index;
            show_form = false;
            enabled = false;

            // check (0 <= opacity <= MAX_OPACITY)
            if (opacity > MAX_OPACITY)
                opacity = MAX_OPACITY;
            else if (opacity < 0)
                opacity = 0;

            form = new TranslucentForm(x, y, width, height, opacity);
            form.Hide();
        }

        //
        //  </CONSTRUCTORS>
        //



        //
        //  <INTERFACE>
        //

        public void Destroy() { form.Close(); form = null; }
        
        //
        //  </INTERFACE>
        //
    }
}
