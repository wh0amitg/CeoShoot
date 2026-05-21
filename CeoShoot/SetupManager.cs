using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CeoShootMain
{
    public static class SetupManager
    {
        private static readonly string ConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        public static bool IsFirstLaunch() => !File.Exists(ConfigFile);

        public static void MarkAsLaunched()
        {
            if (!File.Exists(ConfigFile))
            {
                File.Create(ConfigFile).Dispose();
            }
        }

        public static void SetAutostart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                            key.SetValue("CEOSHOOT", $"\"{Application.ExecutablePath}\"");
                        else
                            key.DeleteValue("CEOSHOOT", false);
                    }
                }
            }
            catch { }
        }
    }
}