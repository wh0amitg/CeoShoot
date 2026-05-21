using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CeoShootMain
{
    public class BackgroundControllerForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HotkeyId = 1;
        private const uint ModNone = 0x0000;
        private const uint VkSnapshot = 0x2C;

        private NotifyIcon _trayIcon;
        private Icon _appIcon;
        private bool _isCapturing = false;

        public BackgroundControllerForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Size = new Size(0, 0);
            this.Opacity = 0;
            this.Visible = false;

            LoadApplicationIcon();
            InitTrayIcon();

            RegisterHotKey(this.Handle, HotkeyId, ModNone, VkSnapshot);
        }

        private void LoadApplicationIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    _appIcon = new Icon(iconPath);
                    this.Icon = _appIcon;
                    return;
                }
                catch { }
            }
            _appIcon = SystemIcons.Application;
            this.Icon = _appIcon;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Text = "CEOSHOOT",
                Visible = true
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("Settings...", null, (s, e) => OpenSettings());
            menu.Items.Add("About", null, (s, e) => ShowAbout());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                this.Close();
            });

            _trayIcon.ContextMenuStrip = menu;
        }

        private void OpenSettings()
        {
            using (SetupForm setup = new SetupForm())
            {
                if (setup.ShowDialog() == DialogResult.OK)
                {
                    Program.Autostart = setup.IsAutostartEnabled;
                    Program.SaveFormat = setup.SelectedFormat;
                    Program.AccentColor = setup.SelectedColorHex;
                    Program.SetAutostart(Program.Autostart);
                    Program.SaveSettings();
                }
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "CEOSHOOT - Advanced Screenshot Tool\nVersion: v1.0\nCreator: wh0ami",
                "CEOSHOOT",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void WndProc(ref Message m)
        {
            const int WmHotkey = 0x0312;
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                TriggerScreenshot();
            }
            base.WndProc(ref m);
        }

        private void TriggerScreenshot()
        {
            if (_isCapturing) return;
            _isCapturing = true;

            try
            {
                Bitmap backgroundScreen = CaptureScreen();
                using (SelectionForm selForm = new SelectionForm(backgroundScreen))
                {
                    selForm.Icon = _appIcon;
                    selForm.ShowDialog();
                }
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            UnregisterHotKey(this.Handle, HotkeyId);
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            _appIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}