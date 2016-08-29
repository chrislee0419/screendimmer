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
            // if one exists, do a basic check to see if the information is accurate and send information to OptionsForm constructor
            // if one does not exist, use Screen to get information and use the default OptionsForm constructor
            try
            {
                XDocument xml_doc = XDocument.Load("settings.xml");

                // check if xml document is empty
                if (!xml_doc.Root.Elements().Any())
                {
                    string msg = "XML file does not contain any information.\n\n" +
                            "Press \"OK\" if you would like to create a new XML file with the default settings. Otherwise, press \"Cancel\" to exit the program";
                    DialogResult res = MessageBox.Show(msg, "Warning: Empty XML Document", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (res == DialogResult.Cancel)
                        return;
                    else
                        Application.Run(new OptionsForm());
                }
                else
                    Application.Run(new OptionsForm(xml_doc));
            }
            catch (FileNotFoundException) { Application.Run(new OptionsForm()); }
        }
    }
}
