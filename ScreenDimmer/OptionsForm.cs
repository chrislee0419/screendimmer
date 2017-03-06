using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Timers;

namespace ScreenDimmer
{
    public class OptionsForm : Form
    {
        //
        //  <ATTRIBUTES AND CONSTANTS>
        //

        // constants
        private const string FirstTimeText = "Hello user!\n\nScreenDimmer is an application that makes " +
            "it easier to change the brightness of your screens.\n\nThis application uses an XML " +
            "file to keep track of your settings. Please make sure that you keep this file to retain " +
            "your settings.";
        private const string BaseExceptionMessage =
            "Press \"OK\" if you would like to create a new XML file with the default settings. " +
            "Otherwise, press \"Cancel\" to exit the program.";
        private const string InvalidOperationExceptionMessage =
            "Your XML file is missing an element. " +
            "Please check if the \"settings.xml\" file is valid.\n\n" +
            BaseExceptionMessage;
        private const string FormatExceptionMessage =
            "Your XML file contain an invalid value. " +
            "Please check the \"settings.xml\" file for invalid values.\n\n" +
            BaseExceptionMessage;
        private const string OverflowExceptionMessage =
            "Your XML file contain an extremely large value. " +
            "Please check the \"settings.xml\" file for extremely large numerical values.\n\n" +
            BaseExceptionMessage;
        private static readonly string ArgumentOutOfRangeExceptionMessage =
            "Your XML file contains screen values that are too large or too small. " +
            "Please check the \"settings.xml\" file for very large or small numerical values.\n\n" +
            "ScreenDimmer allows (for individual or aggregated screens):\n" +
            "- Screen position (origin) of " +
            ScreenInfo.MinOriginX + " to " + ScreenInfo.MaxOriginX + " for the x-axis, and " +
            ScreenInfo.MinOriginY + " to " + ScreenInfo.MaxOriginY + " for the y-axis\n" +
            "- Screen resolution of " + ScreenInfo.MinRes + " to " + ScreenInfo.MaxRes + "\n\n" +
            BaseExceptionMessage;
        private const string InformationLabelText = "ScreenDimmer allows users to dim their screens" +
            " without changing the settings on the screen's control panel. Individual dimming" +
            " of multiple screens is also supported.";
        private const string TrayHelpText = "Double click me to quickly open the Settings window." +
            " Right click me to bring up the context menu.";
        private const double DEFAULT_OPACITY = 0.3;
        private const int TIMER_DURATION = 15000;

        private List<ScreenInfo> screen_list;
        private List<TabPage> screen_tabs;

        private ScreenInfo basic_screen;
        private TabPage basic_screen_tab;
        private bool use_separate_screens;

        private TabControl tab_control;
        private Label screen_count_label;

        private NotifyIcon tray;
        private System.Timers.Timer tray_click_timer;

        private System.Timers.Timer save_timer;
        private bool save_timer_triggered;

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
            MessageBox.Show(FirstTimeText, "ScreenDimmer");

            screen_list = new List<ScreenInfo>();
            basic_screen = new ScreenInfo("basic", 0, 0, 0, 0, 0, DEFAULT_OPACITY);
            DetectScreens();

            InitializeForm();
            InitializeTabs();
            InitializeTimer();
            SaveSettings();
        }

        // constructor for valid XML document
        // XElements can have invalid values
        public OptionsForm(XDocument xml)
        {
            bool use_default_values = false;
            screen_list = new List<ScreenInfo>();

            var screens = xml.Descendants("screen");

            // get the options and basic screen information from the XML file
            basic_screen = new ScreenInfo("basic", 0, 0, 0, 0, 0, DEFAULT_OPACITY);
            try
            {
                XElement options = xml.Descendants("options").First();
                XElement basic = xml.Descendants("basicScreen").First();
                use_separate_screens = (Boolean.Parse(options.Element("separateScreens").Value));

                basic_screen.OriginX = Int32.Parse(basic.Element("left").Value);
                basic_screen.OriginY = Int32.Parse(basic.Element("up").Value);
                basic_screen.ResolutionX = Int32.Parse(basic.Element("right").Value) - basic_screen.OriginX;
                basic_screen.ResolutionY = Int32.Parse(basic.Element("down").Value) - basic_screen.OriginY;
                basic_screen.Show = Boolean.Parse(basic.Element("enabled").Value);
                basic_screen.Opacity = Double.Parse(basic.Element("opacity").Value);
                basic_screen.Enabled = !use_separate_screens;

            }
            catch (InvalidOperationException)
            { use_default_values = ExceptionMessageBox(InvalidOperationExceptionMessage, "Warning: InvalidOperationException"); }
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
                            if (screen_enabled)
                                scrn_info.Show = true;

                            break;
                        }
                    }
                }
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
                SaveSettings();
            }

            InitializeForm();
            InitializeTabs();
            InitializeTimer();
        }

        //
        //  </CONSTRUCTORS>
        //



        //
        //  <INITIALIZATION>
        //

        // initializes the properties associated with this form
        private void InitializeForm()
        {
            this.SuspendLayout();

            // set size of form, disallow resizing
            this.Text = "ScreenDimmer";
            this.Icon = Properties.Resources.Icon;
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormClosing += CloseForm;

            // system tray icon setup
            tray = new NotifyIcon();
            tray.Text = "ScreenDimmer";
            tray.Icon = this.Icon;
            tray.Visible = true;

            tray.BalloonTipTitle = "Help";
            tray.BalloonTipText = TrayHelpText;
            tray.BalloonTipIcon = ToolTipIcon.Info;
            tray.MouseClick += TrayClicked;
            tray.MouseDoubleClick += TrayDoubleClicked;

            tray_click_timer = new System.Timers.Timer();
            tray_click_timer.Interval = SystemInformation.DoubleClickTime;
            tray_click_timer.Elapsed += TrayShowHelpBubble;

            // system tray context menu setup
            tray.ContextMenu = new ContextMenu();
            MenuItem menu_show = new MenuItem("Show Settings Window", TrayShowWindow);
            MenuItem menu_quit = new MenuItem("Exit Program", ExitButtonClicked);
            tray.ContextMenu.MenuItems.Add(menu_show);
            tray.ContextMenu.MenuItems.Add(menu_quit);

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
            exit_button.MouseClick += ExitButtonClicked;
            flp.Controls.Add(exit_button);

            Button default_button = new Button();
            default_button.Text = "Use Default Settings";
            default_button.Size = new Size(120, 30);
            default_button.MouseClick += UseDefaultButtonClicked;
            flp.Controls.Add(default_button);

            Button detect_button = new Button();
            detect_button.Text = "Detect Missing Screens";
            detect_button.Size = new Size(140, 30);
            detect_button.MouseClick += DetectScreensButtonClicked;
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

            LinkLabel lnklbl = new LinkLabel();
            lnklbl.Text = "Visit my GitHub repository for updates.";
            lnklbl.LinkArea = new LinkArea(9, 6);
            lnklbl.TextAlign = ContentAlignment.MiddleLeft;
            lnklbl.Font = new Font("Arial", 9);
            lnklbl.MaximumSize = new Size(360, 50);
            lnklbl.AutoSize = true;
            lnklbl.Location = new Point(8, 80);
            lnklbl.LinkClicked += GitHubLinkClicked;
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
            cb.MouseClick += IndividualScreensCheckBoxClicked;
            if (use_separate_screens) cb.Checked = true;
            gb.Controls.Add(cb);
            //  </GENERAL TAB SETUP>
            //

            screen_tabs = new List<TabPage>();

            CreateBasicScreenTab();
            CreateScreenTabs();
            ChangeTabs();

            this.ResumeLayout();
        }

        // initializes timer used for saving options to XML
        void InitializeTimer()
        {
            // set timer, turn off repeating
            save_timer = new System.Timers.Timer(TIMER_DURATION);
            save_timer.AutoReset = false;

            save_timer_triggered = false;
        }

        //
        //  </INITIALIZATION>
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
            scrn_info.Show = false;
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
                this.Close();

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
                // create new ScreenInfo object if this Screen object is not represented
                if (!found)
                {
                    int new_index = 0;
                    bool unavailable = true;

                    while (unavailable)
                    {
                        unavailable = false;
                        ++new_index;

                        foreach (ScreenInfo scrn_info in screen_list)
                        {
                            if (new_index == scrn_info.ScreenIndex)
                            {
                                unavailable = true;
                                break;
                            }
                        }
                    }

                    screen_list.Add(NewScreenInfo(scrn, new_index));
                    recreate_basic_screen = true;
                }

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
            // if NULL, then skip, as screen_count_label will be initialized soon
            try
            { screen_count_label.Text = "Number of monitors detected: " + screen_list.Count; }
            catch (NullReferenceException) { }
            
        }

        // clears screen_list, removed existing forms
        void ClearScreenList()
        {
            foreach (ScreenInfo scrn in screen_list)
                scrn.Destroy();
            screen_list.Clear();
        }

        // used to create tab for aggregated screen
        // can be used to update tab after changing settings
        void CreateBasicScreenTab()
        {
            this.SuspendLayout();

            basic_screen_tab = CreateScreenTab(basic_screen);
            basic_screen_tab.Text = "Dimmer Options";

            this.ResumeLayout();
        }

        // used to create tabs for ScreenInfo objects
        // can be used to update tabs after detecting new screens
        void CreateScreenTabs()
        {
            tab_control.SuspendLayout();

            // remove old tabs
            if (use_separate_screens)
                foreach (TabPage tab in screen_tabs)
                    tab_control.TabPages.Remove(tab);
            screen_tabs.Clear();

            foreach (ScreenInfo scrn_info in screen_list)
                screen_tabs.Add(CreateScreenTab(scrn_info));

            tab_control.ResumeLayout();
        }

        // creates a TabPage for a ScreenInfo
        TabPage CreateScreenTab(ScreenInfo scrn)
        {
            TabPage tab = new TabPage();
            tab.Text = "Screen " + scrn.ScreenIndex;
            tab.Tag = scrn;

            // TableLayoutPanel setup
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.RowCount = 3;
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
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

            // opacity groupbox
            gb = new GroupBox();
            gb.Text = "Opacity";
            gb.Dock = DockStyle.Fill;
            tbl.Controls.Add(gb, 0, 1);

            TrackBar tb = new TrackBar();
            tb.Tag = scrn;
            tb.Location = new Point(8, 14);
            tb.Width = 280;
            tb.TickStyle = TickStyle.Both;
            tb.TickFrequency = 1;
            tb.SmallChange = 1;
            tb.LargeChange = 10;

            tb.Maximum = (int)(ScreenInfo.MaxOpacity * 100);
            tb.Minimum = (int)(ScreenInfo.MinOpacity * 100);
            tb.Value = (int)(scrn.Opacity * 100);
            tb.Scroll += OpacityTrackBarScroll;
            gb.Controls.Add(tb);

            NumericUpDown nud = new NumericUpDown();
            nud.Tag = scrn;
            nud.Location = new Point(294, 26);
            nud.Size = new Size(60, 40);

            nud.Maximum = (int)(ScreenInfo.MaxOpacity * 100);
            nud.Minimum = (int)(ScreenInfo.MinOpacity * 100);
            nud.Value = (int)(scrn.Opacity * 100);
            nud.ValueChanged += OpacityNumericValueChanged;
            nud.KeyDown += OpacityNumericKeyDown;
            gb.Controls.Add(nud);

            // dimming checkbox
            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Fill;
            flp.FlowDirection = FlowDirection.LeftToRight;
            flp.Padding = new Padding(10, 6, 6, 10);
            tbl.Controls.Add(flp, 0, 2);

            CheckBox cb = new CheckBox();
            cb.Tag = scrn;
            cb.Text = "Enable dimming";
            cb.AutoSize = true;
            cb.Checked = scrn.Show;
            cb.MouseClick += DimmingCheckBoxClicked;
            flp.Controls.Add(cb);

            return tab;
        }

        // changes the tabs shown by tab_control
        // depends on the value of use_separate_screen
        void ChangeTabs()
        {
            tab_control.SuspendLayout();

            // clear tabs, keep general information tab
            TabPage general = tab_control.TabPages[0];
            tab_control.TabPages.RemoveAt(0);
            if (use_separate_screens)
                basic_screen.Enabled = false;
            else
                foreach (TabPage tab in screen_tabs)
                    (tab.Tag as ScreenInfo).Enabled = false;
            tab_control.TabPages.Clear();
            tab_control.TabPages.Add(general);

            // using separate screens
            if (use_separate_screens)
            {
                foreach (TabPage tab in screen_tabs)
                {
                    tab_control.TabPages.Add(tab);
                    (tab.Tag as ScreenInfo).Enabled = true;
                }
            }
            // using aggregated screen
            else
            {
                tab_control.TabPages.Add(basic_screen_tab);
                basic_screen.Enabled = true;
            }

            tab_control.ResumeLayout();
        }

        //
        //  </HELPER METHODS>
        //



        //
        //  <XML>
        //

        // called when a value is changed and needs to be recorded in the XML document
        void TriggerSave()
        {
            if (!save_timer_triggered)
            {
                save_timer.Stop();
                save_timer.Start();
                save_timer.Elapsed += SaveTimerElapsed;
                save_timer_triggered = true;
            }
        }

        // export application settings to "settings.xml"
        void SaveSettings()
        {
            XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement root = new XElement("root");

            // add common setting nodes
            root.Add(
                new XElement(
                    "options",
                    new XElement("separateScreens", use_separate_screens)
                ),
                new XElement(
                    "basicScreen",
                    new XElement("left", basic_screen.OriginX),
                    new XElement("up", basic_screen.OriginY),
                    new XElement("right", basic_screen.OriginX + basic_screen.ResolutionX),
                    new XElement("down", basic_screen.OriginY + basic_screen.ResolutionY),
                    new XElement("opacity", basic_screen.Opacity),
                    new XElement("enabled", basic_screen.Show)
                )
            );

            // add screen specific settings
            foreach (ScreenInfo scrn in screen_list)
                root.Add(
                    new XElement(
                        "screen",
                        new XElement("name", scrn.Name),
                        new XElement("index", scrn.ScreenIndex),
                        new XElement("originX", scrn.OriginX),
                        new XElement("originY", scrn.OriginY),
                        new XElement("resX", scrn.ResolutionX),
                        new XElement("resY", scrn.ResolutionY),
                        new XElement("opacity", scrn.Opacity),
                        new XElement("enabled", scrn.Show)
                    )
                );

            // finalize
            xml.Add(root);
            xml.Save("settings.xml");
        }

        //
        //  </XML>
        //



        //
        //  <CALLBACKS>
        //

        private void CloseForm(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
                tray.Visible = false;
        }

        private void GitHubLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { System.Diagnostics.Process.Start("https://github.com/chrislee0419/screendimmer"); }

        private void IndividualScreensCheckBoxClicked(object sender, MouseEventArgs e)
        {
            if ((sender as CheckBox).Checked)
                use_separate_screens = true;
            else
                use_separate_screens = false;

            ChangeTabs();
            TriggerSave();
        }

        private void ExitButtonClicked(object sender, EventArgs e)
        {
            // save options if necessary
            if (save_timer_triggered)
                SaveSettings();

            this.Hide();
            Application.Exit();
        }

        private void UseDefaultButtonClicked(object sender, EventArgs e)
        {
            ClearScreenList();
            DetectScreens();
            CreateScreenTabs();
            ChangeTabs();
            TriggerSave();
        }

        private void DetectScreensButtonClicked(object sender, EventArgs e)
        {
            DetectScreens();
            CreateScreenTabs();
            ChangeTabs();
            TriggerSave();
        }

        private void OpacityTrackBarScroll(object sender, EventArgs e)
        {
            TrackBar tb = sender as TrackBar;
            ScreenInfo scrn = tb.Tag as ScreenInfo;
            NumericUpDown nud = (tb.Parent as GroupBox).Controls[1] as NumericUpDown;

            scrn.Opacity = (double)tb.Value / 100;
            nud.Value = tb.Value;

            TriggerSave();
        }

        private void OpacityNumericValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = sender as NumericUpDown;
            ScreenInfo scrn = nud.Tag as ScreenInfo;
            TrackBar tb = (nud.Parent as GroupBox).Controls[0] as TrackBar;

            scrn.Opacity = (double)nud.Value / 100;
            tb.Value = (int)nud.Value;

            TriggerSave();
        }

        private void OpacityNumericKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                e.Handled = e.SuppressKeyPress = true;

            TriggerSave();
        }

        private void DimmingCheckBoxClicked(object sender, MouseEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            ScreenInfo scrn = cb.Tag as ScreenInfo;

            if (cb.Checked) scrn.Show = true;
            else scrn.Show = false;
        }

        private void SaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SaveSettings();
            save_timer.Elapsed -= SaveTimerElapsed;
            save_timer_triggered = false;
        }

        private void TrayShowWindow(object sender, EventArgs e)
        { this.Show(); }

        private void TrayShowHelpBubble(object sender, EventArgs e)
        {
            tray_click_timer.Stop();
            tray.ShowBalloonTip(4000);
        }

        private void TrayClicked(object sender, MouseEventArgs e)
        { tray_click_timer.Start(); }

        private void TrayDoubleClicked(object sender, MouseEventArgs e)
        {
            tray_click_timer.Stop();
            this.Show();
        }

        //
        //  </CALLBACKS>
        //
    }
}
