﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace imember
{
    public partial class MainForm : Form
    {
        // Maximum monitor arrangements.
        private const int MAX_SUPPORTED_MONITORS = 5;

        // Interval to save current window arrangement, in milliseconds.
        private const int SAVE_INTERVAL = 60000;

        private const string REG_ENTRY = "imember";

        private const int CONSOLE_MAX_LINES = 100;

        private Queue<string> consoleLines;

        private int indexToSave = 0;
        private Timer saveTimer;

        // An array of dictionaries of window arrangements. Each index in the array represents
        // an arragement for a monitor configuration where number of monitors = index. The dictionary
        // is a collection of window position/size rectangles with window processes as the keys.
        private Dictionary<int, Rect>[] arrangements;

        public delegate bool WindowEnumCallback(int hWnd, int lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(WindowEnumCallback lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern void GetWindowText(int hWnd, StringBuilder s, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(int hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(int hWnd, out Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(int hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        private struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
            public int Width
            {
                private set { }
                get { return Right - Left; }
            }
            public int Height
            {
                private set { }
                get { return Bottom - Top; }
            }
            public override string ToString()
            {
                return string.Format("Left: {0}, Right: {1}, Top: {2}, Bottom: {3}, Width: {4}, Height: {5}", Left, Right, Top, Bottom, Width, Height);
            }
        }

        public MainForm()
        {
            arrangements = new Dictionary<int, Rect>[MAX_SUPPORTED_MONITORS];
            consoleLines = new Queue<string>(CONSOLE_MAX_LINES);

            InitializeComponent();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            if (AreAllDisconnected())
            {
                Log("All monitors are now disconnected.");
            }
            else
            {
                int totalScreens = Screen.AllScreens.Length;
                Log("Restoring last known configuration for {0} monitors...", totalScreens);
                RestoreWindows(totalScreens - 1);
            }
        }

        private void SaveWindows()
        {
            indexToSave = Screen.AllScreens.Length - 1;
            Log("Saving current configuration for {0} monitors...", indexToSave);
            arrangements[indexToSave] = new Dictionary<int, Rect>();
            EnumWindows(new WindowEnumCallback(this.GetVisibleWindows), 0);
        }

        private void RestoreWindows(int indexToRestore)
        {
            Dictionary<int, Rect> arrangement = arrangements[indexToRestore];

            const short SWP_NOZORDER = 0X4;
            const int SWP_NOACTIVATE = 0x0010;

            foreach (KeyValuePair<int, Rect> entry in arrangement)
            {
                StringBuilder sb = new StringBuilder(1024);
                GetWindowText(entry.Key, sb, sb.Capacity);
                if (sb.Length > 0)
                {
                    Log("-> Restoring {0} to {1}", sb.ToString(), entry.Value.ToString());
                }
                SetWindowPos(entry.Key, 0, entry.Value.Left, entry.Value.Top, entry.Value.Width, entry.Value.Height, SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        private bool GetVisibleWindows(int hWnd, int lparam)
        {
            if (IsWindowVisible(hWnd))
            {
                Rect rect = new Rect();
                GetWindowRect(hWnd, out rect);
                arrangements[indexToSave][hWnd] = rect;
            }
            return true;
        }

        private bool AreAllDisconnected()
        {           
            // If there is only one screen and it is 640x480, this means all monitors were disconnected.
            return 
                Screen.AllScreens.Length == 1
                && Screen.AllScreens[0].Bounds.Width == 640
                && Screen.AllScreens[0].Bounds.Height == 480;
        }

        private void Log(string message, params object[] args)
        {
            Log(string.Format(message, args));
        }

        private void Log(string message)
        {
            consoleLines.Enqueue(string.Format("{0} {1}", DateTime.Now.ToString("G"), message));
            if (consoleLines.Count > CONSOLE_MAX_LINES - 1)
            {
                consoleLines.Dequeue();
            }
            txtConsole.Lines = consoleLines.ToArray();
            txtConsole.ScrollToCaret();
        }

        private void OnSaveTimer(object sender, EventArgs e)
        {
            if (!AreAllDisconnected())
            {                
                SaveWindows();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Hide();

            lblAbout.Text = "Windows may forget the position and" + Environment.NewLine + "size of all your windows, but I member!";

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk.GetValue(REG_ENTRY) != null)
            {
                checkBox1.Checked = true;
            }

            // Watch event for when displays have changed.
            //
            // This event trigger when signal is lost or restored due to a KVM switch change
            // (if monitor emulation is not provided by the switch) or the monitor power cord is
            // removed or plugged back in. Also, a monitor that is turned on via the monitor
            // controls also triggers these events.
            //
            // This event does NOT trigger when a monitor is turned off via the monitor controls
            // for some monitors most likely due to the monitors providing emulation on power off. 
            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);

            // Saves the current window arrangement on an interval. This is a lazy solution to a problem
            // with the display events. The SystemEvents.DisplaySettingsChanging event fires after user
            // display scaling (such as 150% typically used on 4K monitors) resets and so positions saved
            // at that time would not be correct when applied after the SystemEvents.DisplaySettingsChanged
            // event fires which is after scaling is reapplied. We could try to figure out the DPI and
            // adjust all positions and sizes, but this works good enough.
            saveTimer = new Timer();
            saveTimer.Tick += new EventHandler(OnSaveTimer);
            saveTimer.Interval = SAVE_INTERVAL;
            saveTimer.Start();

            OnSaveTimer(null, null);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                this.Hide();
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Show();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBox1.Checked)
            {
                rk.SetValue(REG_ENTRY, Application.ExecutablePath);
            }
            else
            {
                rk.DeleteValue(REG_ENTRY, false);
            }
        }

        private void lblLink_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.breadweb.net/");
        }

        private void picLogo_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.breadweb.net/");
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void btnConsole_Click(object sender, EventArgs e)
        {
            txtConsole.Visible = !txtConsole.Visible;
        }

        private void btnSaveNow_Click(object sender, EventArgs e)
        {
            SaveWindows();
        }
    }
}
