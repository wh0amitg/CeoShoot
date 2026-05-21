using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CeoShootMain
{
    internal static class Program
    {
        private const string AppName = "CEOSHOOT";
        public static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CeoShoot");
        public static readonly string ConfigPath = Path.Combine(ConfigFolder, "config.ini");

        public static bool Autostart = true;
        public static string SaveFormat = "PNG";
        public static string AccentColor = "#00F0FF";
        public static string SavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!Directory.Exists(ConfigFolder))
            {
                try { Directory.CreateDirectory(ConfigFolder); } catch { }
            }

            if (!File.Exists(ConfigPath))
            {
                using (WelcomeForm welcome = new WelcomeForm())
                {
                    if (welcome.ShowDialog() == DialogResult.OK)
                    {
                        Autostart = welcome.IsAutostartEnabled;
                        SaveFormat = welcome.SelectedFormat;
                        AccentColor = welcome.SelectedColorHex;
                        SetAutostart(Autostart);
                        SaveSettings();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                LoadSettings();
            }

            Application.Run(new BackgroundControllerForm());
        }

        public static void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }
                string content = $"Autostart={Autostart}\nFormat={SaveFormat}\nColor={AccentColor}\nPath={SavePath}";
                File.WriteAllText(ConfigPath, content);
            }
            catch { }
        }

        public static void LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var lines = File.ReadAllLines(ConfigPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("=")) continue;
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length < 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key.Equals("Autostart", StringComparison.OrdinalIgnoreCase)) Autostart = bool.Parse(val);
                    if (key.Equals("Format", StringComparison.OrdinalIgnoreCase)) SaveFormat = val;
                    if (key.Equals("Color", StringComparison.OrdinalIgnoreCase)) AccentColor = val;
                    if (key.Equals("Path", StringComparison.OrdinalIgnoreCase)) SavePath = val;
                }
            }
            catch { }
        }

        public static void SetAutostart(bool enable)
        {
            try
            {
                const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (key == null) return;
                    if (enable) key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
                    else key.DeleteValue(AppName, false);
                }
            }
            catch { }
        }
    }
}