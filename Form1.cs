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
