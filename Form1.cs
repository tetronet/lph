using System;
using System.IO;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UpdateAssociationButton();
        }

        private void UpdateAssociationButton()
        {
            button9.Text = FileAssociation.IsRegistered()
                ? "Stop opening .lph files with LPH by double click"
                : "Open .lph files with LPH by double click";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                if (FileAssociation.IsRegistered())
                {
                    if (MessageBox.Show(".lph files will no longer open in LPH by double click. Continue?", "Tsu-k Executive", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        FileAssociation.Unregister();
                    }
                }
                else
                {
                    FileAssociation.Register();
                    MessageBox.Show("Done: double click opens .lph files in the Viewer, \"Edit in LPH Editor\" is in the right click menu.\n\n" +
                        "If a double click still opens another program, Windows remembers your earlier choice: right click a .lph file -> Open with -> Choose another app -> LPH, and check \"Always use this app\".\n\n" +
                        "If you move LPH to another folder, press this button again.", "Tsu-k Executive");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to change the .lph file association: " + ex.Message, "Tsu-k Executive", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            UpdateAssociationButton();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Recent(File.ReadAllText("Recent.text").Split('\n')).Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string filename = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                File.AppendAllText("Recent.text", filename + "\n");
                new Viewer(filename).Show();
            }
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
         
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string filename = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                File.AppendAllText("Recent.text", filename + "\n");
                new Edit(filename, true).Show();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            new Edit("", true).Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new Converter().Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            new Viewer("").Show();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            new ExperimentsMainMenu().Show();
        }
    }
}
