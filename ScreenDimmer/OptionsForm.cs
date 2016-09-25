﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ScreenDimmer
{
    public class OptionsForm : Form
    {
        //
        //  <ATTRIBUTES AND CONSTANTS>
        //

        // strings used for messages generated by exceptions
        private const string BaseExceptionMessage =
            "Press \"OK\" if you would like to create a new XML file with the default settings. " +
            "Otherwise, press \"Cancel\" to exit the program.";
        private const string ArgumentNullExceptionMessage =
            "XML file contains an empty tag. " +
            "Please check if the settings.xml file is valid.\n\n" +
            BaseExceptionMessage;
        private const string FormatExceptionMessage =
            "XML file contain an invalid value. " +
            "Please check the settings.xml file for invalid values.\n\n" +
            BaseExceptionMessage;
        private const string OverflowExceptionMessage =
            "XML file contain an extremely large value. " +
            "Please check the settings.xml file for extremely large numerical values.\n\n" +
            BaseExceptionMessage;
        private static readonly string ArgumentOutOfRangeExceptionMessage =
            "XML file contains screen values that are too large or too small. " +
            "Please check the settings.xml file for very large or small numerical values.\n\n" +
            "ScreenDimmer allows (for single or aggregated screens):\n" +
            "\tScreen position (origin) of " +
            ScreenInfo.MinOriginX + " to " + ScreenInfo.MaxOriginX + " for the x-axis, and" +
            ScreenInfo.MinOriginY + " to " + ScreenInfo.MaxOriginY + " for the y-axis\n" +
            "\tScreen resolution of " + ScreenInfo.MinRes + " to " + ScreenInfo.MaxRes + "\n\n" +
            BaseExceptionMessage;
        private const string InformationLabelText = "ScreenDimmer allows users to dim their screens" +
            " screens without changing the settings on the screen's control panel. Individual dimming" +
            " of multiple screens is also supported.";
        private const double DEFAULT_OPACITY = 0.3;

        private List<ScreenInfo> screen_list;
        private Dictionary<TabPage, ScreenInfo> screen_tabs;

        private ScreenInfo basic_screen;
        private TabPage basic_screen_tab;
        private bool use_separate_screens;

        private TabControl tab_control;
        private Label screen_count_label;

        //
        //  </ATTRIBUTES AND CONSTANTS>
        //



        //
        //  <CONSTRUCTORS>
        //

        // default constructor
        // used when "settings.xml" is not found, or contains invalid values
        public OptionsForm()
        {
            screen_list = new List<ScreenInfo>();
            DetectScreens();

            InitializeForm();
            InitializeTabs();
        }

        // constructor for valid XML document
        // XElements can have invalid values
        public OptionsForm(XDocument xml)
        {
            bool use_default_values = false;
            screen_list = new List<ScreenInfo>();

            var screens = xml.Descendants("screen");

            // get the options and basic screen information from the XML file
            try
            {
                XElement options = xml.Descendants("options").First();
                use_separate_screens = Boolean.Parse(options.Element("separateScreens").Value);

                XElement basic = xml.Descendants("basicScreen").First();
                int originX = Int32.Parse(basic.Element("left").Value);
                int originY = Int32.Parse(basic.Element("up").Value);
                int resX = Int32.Parse(basic.Element("right").Value) - originX;
                int resY = Int32.Parse(basic.Element("down").Value) - originY;
                bool basic_enabled = Boolean.Parse(basic.Element("enabled").Value);
                double opacity = Double.Parse(basic.Element("opacity").Value);

                basic_screen = new ScreenInfo("basic", 0, originX, originY, resX, resY, opacity);
                if (!use_separate_screens)
                    basic_screen.Enabled = true;
                if (basic_enabled)
                    basic_screen.Show = true;
            }
            catch (ArgumentNullException)
            { use_default_values = ExceptionMessageBox(ArgumentNullExceptionMessage, "Warning: ArgumentNullException"); }
            catch (FormatException)
            { use_default_values = ExceptionMessageBox(FormatExceptionMessage, "Warning: FormatException"); }
            catch (OverflowException)
            { use_default_values = ExceptionMessageBox(OverflowExceptionMessage, "Warning: OverflowException"); }
            catch (ArgumentOutOfRangeException)
            { use_default_values = ExceptionMessageBox(ArgumentOutOfRangeExceptionMessage, "Warning: ArgumentOutOfRangeException"); }

            // copy screen information from the XML file
            foreach (XElement screen in screens)
            {
                if (use_default_values)
                    break;

                // ensure that the XML file does not have invalid values
                try
                {
                    ScreenInfo scrn_info = new ScreenInfo(screen);
                    if (use_separate_screens)
                        scrn_info.Enabled = true;

                    // ignore screens that do not appear in Screen.AllScreens
                    // they will not be added to screen_list, so they should not get their own tab
                    foreach (Screen scrn in Screen.AllScreens)
                    {
                        if (scrn_info.Name.Equals(scrn.DeviceName))
                        {
                            screen_list.Add(scrn_info);

                            bool screen_enabled = Boolean.Parse(screen.Element("enabled").Value);
                            if (use_separate_screens && screen_enabled)
                                scrn_info.Show = true;

                            break;
                        }
                    }
                }
                catch (ArgumentNullException)
                { use_default_values = ExceptionMessageBox(ArgumentNullExceptionMessage, "Warning: ArgumentNullException"); }
                catch (FormatException)
                { use_default_values = ExceptionMessageBox(FormatExceptionMessage, "Warning: FormatException"); }
                catch (OverflowException)
                { use_default_values = ExceptionMessageBox(OverflowExceptionMessage, "Warning: OverflowException"); }
                catch (ArgumentOutOfRangeException)
                { use_default_values = ExceptionMessageBox(ArgumentOutOfRangeExceptionMessage, "Warning: ArgumentOutOfRangeException"); }
            }

            // if there was an error in the XML file, recreate screen_list using AllScreens
            if (use_default_values)
            {
                ClearScreenList();
                DetectScreens();
            }

            InitializeForm();
            InitializeTabs();
        }

        //
        //  </CONSTRUCTORS>
        //



        //
        //  <HELPER METHODS>
        //

        private ScreenInfo NewScreenInfo(Screen scrn, int index)
        {
            string name = scrn.DeviceName;
            int originX = scrn.Bounds.X;
            int originY = scrn.Bounds.Y;
            int resX = scrn.Bounds.Width;
            int resY = scrn.Bounds.Height;
            ScreenInfo scrn_info = new ScreenInfo(name, index, originX, originY, resX, resY, DEFAULT_OPACITY);
            scrn_info.Show = true;
            if (use_separate_screens)
                scrn_info.Enabled = true;
            return scrn_info;
        }

        // show MessageBox for exceptions thrown during the constructor for valid XML documents
        private bool ExceptionMessageBox(string msg, string title)
        {
            DialogResult res = MessageBox.Show(msg, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (res == DialogResult.OK)
                return true;
            else
                Application.Exit();
            return false;
        }

        // searches for new screens
        // fixes position and resolution of existing screens
        // rebuilds basic screen if necessary
        // can be used to set up default settings (needs empty screen_list)
        private void DetectScreens()
        {
            bool recreate_basic_screen = false;
            int basic_left = 0, basic_right = 0, basic_up = 0, basic_down = 0;

            // search for new screens using Screen.AllScreens
            foreach (Screen scrn in Screen.AllScreens)
            {
                bool found = false;
                int x = scrn.Bounds.X;
                int y = scrn.Bounds.Y;
                int w = scrn.Bounds.Width;
                int h = scrn.Bounds.Height;

                // look for existing ScreenInfo representation of Screen object 
                foreach (ScreenInfo scrn_info in screen_list)
                {
                    if (scrn_info.Name.Equals(scrn.DeviceName))
                    {
                        // check if information stored is still accurate
                        bool originX_check = scrn_info.OriginX != x;
                        bool originY_check = scrn_info.OriginY != y;
                        bool resX_check = scrn_info.ResolutionX != w;
                        bool resY_check = scrn_info.ResolutionY != h;

                        if ( originX_check || originY_check || resX_check || resY_check )
                        {
                            scrn_info.OriginX = x;
                            scrn_info.OriginY = y;
                            scrn_info.ResolutionX = w;
                            scrn_info.ResolutionY = h;

                            recreate_basic_screen = true;
                        }

                        found = true;
                        break;
                    }
                }
                if (!found)
                    screen_list.Add(NewScreenInfo(scrn, screen_list.Count + 1));

                // get largest screen size
                if (basic_left > x)
                    basic_left = x;
                if (basic_up > y)
                    basic_up = y;
                if (basic_right < x + w)
                    basic_right = x + w;
                if (basic_down < y + h)
                    basic_down = y + h;
            }

            // recreate basic screen if necessary
            if (recreate_basic_screen)
            {
                basic_screen.OriginX = basic_left;
                basic_screen.OriginY = basic_up;
                basic_screen.ResolutionX = basic_right - basic_left;
                basic_screen.ResolutionY = basic_down - basic_up;
            }

            // update screen count text
            screen_count_label.Text = "Number of monitors detected: " + screen_list.Count;
        }

        // clears screen_list, removed existing forms
        void ClearScreenList()
        {
            foreach (ScreenInfo scrn in screen_list)
                scrn.Destroy();
            screen_list.Clear();
        }

        // initializes the attributes associated with this form
        private void InitializeForm()
        {
            this.SuspendLayout();

            // set size of form, disallow resizing
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // TableLayoutPanel setup
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.RowCount = 2;
            tbl.ColumnCount = 1;
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 84));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 16));

            // tab control setup
            tab_control = new TabControl();
            tab_control.Dock = DockStyle.Fill;

            // buttons setup
            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Fill;
            flp.FlowDirection = FlowDirection.RightToLeft;
            tbl.Controls.Add(flp, 0, 1);

            Button exit_button = new Button();
            exit_button.Text = "Quit";
            exit_button.Size = new Size(80, 30);
            exit_button.MouseClick += new MouseEventHandler(ExitButtonClicked);
            flp.Controls.Add(exit_button);

            Button default_button = new Button();
            default_button.Text = "Use Default Settings";
            default_button.Size = new Size(120, 30);
            default_button.MouseClick += new MouseEventHandler(UseDefaultButtonClicked);
            flp.Controls.Add(default_button);

            Button detect_button = new Button();
            detect_button.Text = "Detect Missing Screens";
            detect_button.Size = new Size(140, 30);
            detect_button.MouseClick += new MouseEventHandler(DetectScreensButtonClicked);
            flp.Controls.Add(detect_button);

            tbl.Controls.Add(tab_control, 0, 0);
            this.Controls.Add(tbl);
            this.ResumeLayout();
        }

        // create tabs for general options menu and individual screens
        private void InitializeTabs()
        {
            this.SuspendLayout();

            // 
            //  <GENERAL TAB SETUP>
            TabPage general_tab = new TabPage();
            general_tab.Text = "General";
            tab_control.TabPages.Add(general_tab);

            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.RowCount = 2;
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            tbl.ColumnCount = 1;
            general_tab.Controls.Add(tbl);

            // information group
            GroupBox gb = new GroupBox();
            gb.Dock = DockStyle.Fill;
            gb.Text = "Information";
            tbl.Controls.Add(gb, 0, 0);

            Label lbl = new Label();
            lbl.Text = InformationLabelText;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Font = new Font("Arial", 9);
            lbl.MaximumSize = new Size(360, 150);
            lbl.AutoSize = true;
            lbl.Location = new Point(8, 20);
            gb.Controls.Add(lbl);

            lbl = new Label();
            lbl.Text = "Application created by Chris Lee.";
            lbl.TextAlign = ContentAlignment.MiddleRight;
            lbl.Font = new Font("Arial", 9);
            lbl.Size = new Size(360, 20);
            lbl.Location = new Point(6, 65);
            gb.Controls.Add(lbl);

            LinkLabel lnklbl = new LinkLabel();
            lnklbl.Text = "Visit my GitHub repository for updates.";
            lnklbl.LinkArea = new LinkArea(9, 6);
            lnklbl.TextAlign = ContentAlignment.MiddleLeft;
            lnklbl.Font = new Font("Arial", 9);
            lnklbl.MaximumSize = new Size(360, 50);
            lnklbl.AutoSize = true;
            lnklbl.Location = new Point(8, 84);
            lnklbl.LinkClicked += new LinkLabelLinkClickedEventHandler(this.GitHubLinkClicked);
            gb.Controls.Add(lnklbl);

            // settings group
            gb = new GroupBox();
            gb.Dock = DockStyle.Fill;
            gb.Text = "Settings";
            tbl.Controls.Add(gb, 0, 1);

            screen_count_label = new Label();
            screen_count_label.Text = "Number of monitors detected: " + screen_list.Count;
            screen_count_label.Location = new Point(14, 20);
            screen_count_label.AutoSize = true;
            gb.Controls.Add(screen_count_label);

            CheckBox cb = new CheckBox();
            cb.Text = "Enable individual screen controls";
            cb.Location = new Point(14, 44);
            cb.AutoSize = true;
            cb.MouseClick += new MouseEventHandler(IndividualScreensCheckBoxClicked);
            if (use_separate_screens) cb.Checked = true;
            gb.Controls.Add(cb);
            //  </GENERAL TAB SETUP>
            //

            screen_tabs = new Dictionary<TabPage, ScreenInfo>();

            CreateBasicScreenTab();
            CreateScreenTabs();
            ChangeTabs();
            
            this.ResumeLayout();
        }

        // used to create tab for aggregated screen
        // can be used to update tab after changing settings
        void CreateBasicScreenTab()
        {
            this.SuspendLayout();

            basic_screen_tab = CreateScreenTab(basic_screen);
            basic_screen_tab.Text = "Screen Options";

            this.ResumeLayout();
        }

        // used to create tabs for ScreenInfo objects
        // can be used to update tabs after detecting new screens
        void CreateScreenTabs()
        {
            this.SuspendLayout();

            // remove old tabs
            if (use_separate_screens)
                foreach (TabPage tab in screen_tabs.Keys)
                    tab_control.TabPages.Remove(tab);
            screen_tabs.Clear();

            foreach (ScreenInfo scrn_info in screen_list)
                screen_tabs.Add(CreateScreenTab(scrn_info), scrn_info);

            this.ResumeLayout();
        }

        // creates a TabPage for a ScreenInfo
        TabPage CreateScreenTab(ScreenInfo scrn)
        {
            TabPage tab = new TabPage();
            tab.Text = "Screen " + scrn.ScreenIndex;

            // TableLayoutPanel setup
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.RowCount = 2;
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
            tbl.ColumnCount = 1;
            tab.Controls.Add(tbl);

            // screen information groupbox
            GroupBox gb = new GroupBox();
            gb.Text = "Screen Information";
            gb.Dock = DockStyle.Fill;
            tbl.Controls.Add(gb, 0, 0);

            ListBox lb = new ListBox();
            lb.Dock = DockStyle.Fill;
            lb.Items.Add("Resolution: " + scrn.ResolutionX + " by " + scrn.ResolutionY);
            lb.Items.Add("Position: (" + scrn.OriginX + ", " + scrn.OriginY + ")");
            gb.Controls.Add(lb);

            // controls groupbox
            gb = new GroupBox();
            gb.Text = "Controls";
            gb.Dock = DockStyle.Fill;
            tbl.Controls.Add(gb, 0, 1);

            TrackBar tb = new TrackBar();
            tb.Location = new Point(10, 20);
            tb.Width = 280;
            tb.TickStyle = TickStyle.None;
            tb.TickFrequency = 1;
            tb.SmallChange = 1;
            tb.LargeChange = 10;

            tb.Maximum = (int)(ScreenInfo.MaxOpacity * 100);
            tb.Minimum = (int)(ScreenInfo.MinOpacity * 100);
            tb.Value = (int)(scrn.Opacity * 100);

            tb.Scroll += new EventHandler(OpacityTrackBarScroll);
            gb.Controls.Add(tb);

            return tab;
        }

        // changes the tabs shown by tab_control
        // depends on the value of use_separate_screen
        void ChangeTabs()
        {
            this.SuspendLayout();

            // clear tabs, keep general information tab
            TabPage general = tab_control.TabPages[0];
            tab_control.TabPages.RemoveAt(0);
            if (use_separate_screens)
                basic_screen.Enabled = false;
            else
                foreach (TabPage tab in screen_tabs.Keys)
                    screen_tabs[tab].Enabled = false;
            tab_control.TabPages.Clear();
            tab_control.TabPages.Add(general);

            // using separate screens
            if (use_separate_screens)
            {
                foreach (TabPage tab in screen_tabs.Keys)
                {
                    tab_control.TabPages.Add(tab);
                    screen_tabs[tab].Enabled = true;
                }
            }
            // using aggregated screen
            else
            {
                tab_control.TabPages.Add(basic_screen_tab);
                basic_screen.Enabled = true;
            }

            this.ResumeLayout();
        }

        //
        //  </HELPER METHODS>
        //



        //
        //  <CALLBACKS>
        //

        private void GitHubLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { System.Diagnostics.Process.Start("https://github.com/chrislee0419/screendimmer"); }

        private void IndividualScreensCheckBoxClicked(object sender, EventArgs e)
        {
            if ((sender as CheckBox).Checked)
                use_separate_screens = true;
            else
                use_separate_screens = false;

            ChangeTabs();
        }

        private void ExitButtonClicked(object sender, EventArgs e)
        { Application.Exit(); }

        private void UseDefaultButtonClicked(object sender, EventArgs e)
        {
            ClearScreenList();
            DetectScreens();
            CreateScreenTabs();
            ChangeTabs();
        }

        private void DetectScreensButtonClicked(object sender, EventArgs e)
        {
            DetectScreens();
            CreateScreenTabs();
            ChangeTabs();
        }

        private void OpacityTrackBarScroll(object sender, EventArgs e)
        {
            TrackBar tb = sender as TrackBar;
            // TrackBar -> GroupBox -> TableLayoutPanel -> TabPage
            TabPage tab = tb.Parent.Parent.Parent as TabPage;

            screen_tabs[tab].Opacity = (double)tb.Value / 100;
        }

        //
        //  </CALLBACKS>
        //
    }
}
