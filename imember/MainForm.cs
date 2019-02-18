using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SHDocVw;

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
        private bool isEnabled = true;

        // An array of dictionaries of window arrangements. Each index in the array represents
        // an arragement for a monitor configuration where number of monitors = index. The dictionary
        // is a collection of window position/size rectangles with window processes as the keys.
        private Dictionary<IntPtr, Rect>[] arrangements;

        [DllImport("user32.dll")]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder s, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

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
            arrangements = new Dictionary<IntPtr, Rect>[MAX_SUPPORTED_MONITORS];
            consoleLines = new Queue<string>(CONSOLE_MAX_LINES);

            InitializeComponent();
        }

        private void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            if (AreAllDisconnected())
            {
                Log("All monitors are now disconnected.");
            }
            else
            {
                if (isEnabled)
                {
                    int totalScreens = Screen.AllScreens.Length;
                    int indexToRestore = totalScreens - 1;
                    Log("Restoring last known arrangement for {0} monitors...", totalScreens);
                    RestoreWindows(indexToRestore);
                }
            }
        }

        private void SaveWindows(bool shouldLogArrangement)
        {
            int totalMonitors = Screen.AllScreens.Length;
            indexToSave = totalMonitors - 1;
            Log("Saving current configuration for {0} monitors...", totalMonitors);
            arrangements[indexToSave] = new Dictionary<IntPtr, Rect>();

            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                // If a main window title is missing, it is usually a background process.
                if (string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    continue;
                }

                // Windows Store applications have a main window title but may be suspended and not visible.
                ProcessThread thread = process.Threads[0];
                if (thread.ThreadState == ThreadState.Wait && thread.WaitReason == ThreadWaitReason.Suspended)
                {
                    continue;
                }

                SaveWindow(indexToSave, GetWindowRect(process.MainWindowHandle), process.MainWindowHandle, process.ProcessName, shouldLogArrangement);
            }

            // Save this application's location.
            Rect rect = new Rect();
            rect.Left = this.Bounds.Left;
            rect.Top = this.Bounds.Top;
            rect.Right = this.Bounds.Right;
            rect.Bottom = this.Bounds.Bottom;

            SaveWindow(indexToSave, rect, Process.GetCurrentProcess().MainWindowHandle, this.Name, shouldLogArrangement);

            // Save all open file explorer windows.
            ShellWindows shellWindows = new SHDocVw.ShellWindows();
            foreach (InternetExplorer window in shellWindows)
            {
                SaveWindow(indexToSave, GetWindowRect((IntPtr)window.HWND), (IntPtr)window.HWND, window.Name, shouldLogArrangement);
            }

        }

        private Rect GetWindowRect(IntPtr hWnd)
        {
            Rect rect = new Rect();
            GetWindowRect(hWnd, out rect);
            return rect;
        }

        private void SaveWindow(int indexToSave, Rect rect, IntPtr hWnd, string windowName, bool shouldLogArrangement)
        {
            arrangements[indexToSave][hWnd] = rect;
            if (shouldLogArrangement)
            {
                Log("Saving {0} at {1}:", windowName, rect.ToString());
            }
        }

        private void RestoreWindows(int indexToRestore)
        {
            Dictionary<IntPtr, Rect> arrangement = arrangements[indexToRestore];
            if (arrangement == null)
            {
                Log("We don't have an arrangement for {0} monitors yet!", indexToRestore + 1);
                return;
            }

            const short SWP_NOZORDER = 0X4;
            const int SWP_NOACTIVATE = 0x0010;

            foreach (KeyValuePair<IntPtr, Rect> entry in arrangement)
            {
                StringBuilder sb = new StringBuilder(1024);
                GetWindowText(entry.Key, sb, sb.Capacity);
                Log("-> Restoring {0} to {1}", sb.ToString(), entry.Value.ToString());
                SetWindowPos(entry.Key, 0, entry.Value.Left, entry.Value.Top, entry.Value.Width, entry.Value.Height, SWP_NOZORDER | SWP_NOACTIVATE);
            }
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
            message = string.Format("{0} {1}", DateTime.Now.ToString("G"), message);
            consoleLines.Enqueue(message);
            if (consoleLines.Count > CONSOLE_MAX_LINES - 1)
            {
                consoleLines.Dequeue();
            }
            txtConsole.Lines = consoleLines.ToArray();
            txtConsole.ScrollToCaret();
            Console.WriteLine(message);
        }

        private void OnSaveTimer(object sender, EventArgs e)
        {
            if (!AreAllDisconnected() && isEnabled)
            {                
                SaveWindows(true);
            }
        }

        private void ToggleEnabled()
        {
            isEnabled = !isEnabled;
            UpdateEnabledDisplay();
        }

        private void UpdateEnabledDisplay()
        {
            toolStripMenuItem2.Text = isEnabled ? "Disable" : "Enable";
            checkBox2.Checked = isEnabled;
            btnSaveNow.Enabled = isEnabled;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            UpdateEnabledDisplay();

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
            SystemEvents.DisplaySettingsChanging += new EventHandler(SystemEvents_DisplaySettingsChanging);

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
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.WindowState = FormWindowState.Normal;
            }
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
            if (isEnabled)
            {
                SaveWindows(true);
            }            
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ToggleEnabled();
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            ToggleEnabled();
        }
    }
}
