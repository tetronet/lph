using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// <param name="args">"file.lph" opens the file in the Viewer, "--edit file.lph" in the Editor (used by the .lph file association)</param>
        [STAThread]
        static void Main(string[] args)
        {
            // LPH data (files and network) must look the same on every machine: always write numbers with '.'
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string? fileToOpen = null;
            bool openInEditor = false;
            foreach (string arg in args)
            {
                if (arg == "--edit")
                {
                    openInEditor = true;
                }
                else
                {
                    // resolve before changing the current directory
                    fileToOpen = Path.GetFullPath(arg);
                }
            }
            // started by a double click on a file, the current directory may be anything (e.g. the file's folder),
            // but tetronet.txt, Recent.text and temp/ live next to the program
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            Directory.CreateDirectory("temp");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (fileToOpen == null)
            {
                Application.Run(new Form1());
                return;
            }
            try
            {
                File.AppendAllText("Recent.text", fileToOpen + "\n");
            }
            catch (Exception e)
            {
                DebugWriter.ReceiveException("adding the opened file to Recent.text", e);
            }
            Form first = openInEditor ? new Edit(fileToOpen, true) : new Viewer(fileToOpen);
            Application.Run(new LastFormContext(first));
        }
    }

    /// <summary>
    /// Keeps the program running while any of its windows is open, so closing the Viewer doesn't kill an Editor opened from it.
    /// </summary>
    internal sealed class LastFormContext : ApplicationContext
    {
        public LastFormContext(Form first)
        {
            first.Show();
            Application.Idle += OnIdle;
        }

        private void OnIdle(object? sender, EventArgs e)
        {
            if (Application.OpenForms.Count == 0)
            {
                Application.Idle -= OnIdle;
                ExitThread();
            }
        }
    }
}
