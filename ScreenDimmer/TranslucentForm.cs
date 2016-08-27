using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScreenDimmer
{
    public class TranslucentForm : Form
    {
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public TranslucentForm(int x, int y, int width, int height, int max_opacity)
        {
            TopMost = true;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Black;
            Opacity = max_opacity;
            Width = width;
            Height = height;
            Location = new Point(x, y);
            StartPosition = FormStartPosition.Manual;

            // allow mouse click-through
            int window_long = GetWindowLong(this.Handle, -20);
            window_long |= 0x20;
            SetWindowLong(this.Handle, -20, window_long);
        }
    }
}
