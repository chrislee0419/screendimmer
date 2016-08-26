using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ScreenDimmer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // form setup
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // check for existing .xml file
            // if one exists, send information to form constructor
            // if one does not exist, use default form constructor
            try
            {
                XDocument xml_doc = XDocument.Load("settings.xml");
                var screens = xml_doc.Descendants("Screen");
            }
            catch (FileNotFoundException)
            {
                Application.Run(new OptionsForm());
            }
        }
    }
}
