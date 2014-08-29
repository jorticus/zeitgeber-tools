using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ViscTronics.ZeitgeberGUI
{
    /// <summary>
    /// Modified TextBox with auto-scrolling.
    /// Auto-scrolling only happens if the caret is at the end of text,
    /// otherwise the textbox doesn't scroll. 
    /// (useful if the user wants to copy or look at text)
    /// </summary>
    class ConsoleTextBox : TextBox
    {
        private const int SB_VERT = 0x1;
        private const int WM_VSCROLL = 0x115;
        private const int SB_THUMBPOSITION = 0x4;
        private const int SB_BOTTOM = 0x7;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll")]
        private static extern bool PostMessageA(IntPtr hWnd, int nBar, int wParam, int lParam);

        public ConsoleTextBox() : base()
        {
            this.KeyDown += ConsoleTextBox_KeyDown;
        }

        void ConsoleTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.End)
                this.ScrollToBottom();
        }

        public new void AppendText(string text)
        {
            this.SuspendLayout();

            int oldSelectionStart = this.SelectionStart;
            int oldSelectionLength = this.SelectionLength;
            int oldTextLength = this.Text.Length;

            int savedVpos = GetScrollPos(this.Handle, SB_VERT);
            base.AppendText(text);

            // Autoscroll if caret is at the end of the textbox
            if (oldSelectionStart == oldTextLength)
            {
                ScrollToBottom();
            }
            else
            {
                SetScrollPos(this.Handle, SB_VERT, savedVpos, true);
                PostMessageA(this.Handle, WM_VSCROLL, SB_THUMBPOSITION + 0x10000 * savedVpos, 0);

                this.SelectionStart = oldSelectionStart;
                this.SelectionLength = oldSelectionLength;
            }

            this.ResumeLayout();
        }

        public void ScrollToBottom()
        {
            PostMessageA(this.Handle, WM_VSCROLL, SB_BOTTOM, 0);
            this.SelectionStart = this.Text.Length;
        }
    }
}
