using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LPH_Edit_Viewer
{
    /// <summary>
    /// Associates *.lph files with this program for the current user only (HKCU\Software\Classes, no administrator rights needed):
    /// double click opens a file in the Viewer, "Edit" in the context menu opens it in the Editor.
    /// </summary>
    internal static class FileAssociation
    {
        private const string EXTENSION = ".lph";
        private const string PROG_ID = "LPH.Drawing";
        private const string CLASSES = @"Software\Classes\";

        public static bool IsRegistered()
        {
            using RegistryKey? command = Registry.CurrentUser.OpenSubKey($@"{CLASSES}{PROG_ID}\shell\open\command");
            using RegistryKey? extension = Registry.CurrentUser.OpenSubKey(CLASSES + EXTENSION);
            return command?.GetValue("") as string == Command("")
                && extension?.GetValue("") as string == PROG_ID;
        }

        public static void Register()
        {
            using (RegistryKey extension = Registry.CurrentUser.CreateSubKey(CLASSES + EXTENSION))
            {
                extension.SetValue("", PROG_ID);
            }
            using (RegistryKey openWith = Registry.CurrentUser.CreateSubKey($@"{CLASSES}{EXTENSION}\OpenWithProgids"))
            {
                openWith.SetValue(PROG_ID, "");
            }
            using (RegistryKey progId = Registry.CurrentUser.CreateSubKey(CLASSES + PROG_ID))
            {
                progId.SetValue("", "LPH drawing");
            }
            using (RegistryKey icon = Registry.CurrentUser.CreateSubKey($@"{CLASSES}{PROG_ID}\DefaultIcon"))
            {
                icon.SetValue("", $"\"{ExePath()}\",0");
            }
            SetVerb("open", "Open in LPH Viewer", Command(""));
            SetVerb("edit", "Edit in LPH Editor", Command("--edit "));
            NotifyShell();
        }

        public static void Unregister()
        {
            Registry.CurrentUser.DeleteSubKeyTree(CLASSES + PROG_ID, false);
            using (RegistryKey? extension = Registry.CurrentUser.OpenSubKey(CLASSES + EXTENSION, true))
            {
                // don't touch the extension if it was given to another program meanwhile
                if (extension?.GetValue("") as string == PROG_ID)
                {
                    extension.DeleteValue("");
                }
            }
            using (RegistryKey? openWith = Registry.CurrentUser.OpenSubKey($@"{CLASSES}{EXTENSION}\OpenWithProgids", true))
            {
                openWith?.DeleteValue(PROG_ID, false);
            }
            NotifyShell();
        }

        private static void SetVerb(string verb, string title, string command)
        {
            using (RegistryKey verbKey = Registry.CurrentUser.CreateSubKey($@"{CLASSES}{PROG_ID}\shell\{verb}"))
            {
                verbKey.SetValue("", title);
            }
            using (RegistryKey commandKey = Registry.CurrentUser.CreateSubKey($@"{CLASSES}{PROG_ID}\shell\{verb}\command"))
            {
                commandKey.SetValue("", command);
            }
        }

        private static string Command(string arguments)
        {
            return $"\"{ExePath()}\" {arguments}\"%1\"";
        }

        private static string ExePath()
        {
            return Environment.ProcessPath ?? Application.ExecutablePath;
        }

        // tells Explorer that associations changed, so icons and double click work without logging off
        private static void NotifyShell()
        {
            const int SHCNE_ASSOCCHANGED = 0x08000000;
            const int SHCNF_IDLIST = 0;
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
    }
}
