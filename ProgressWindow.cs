using System;
using System.Drawing;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class ProgressWindow : Form
    {
        private int taskAmount = 0;
        private string progress = "";
        private int progressValue = 0;

        public ProgressWindow(string title, int taskAmount)
        {
            InitializeComponent();
            label1.Text = title;
            label2.Text = $"Progress: 0 out of {taskAmount}, [value]:";
            this.taskAmount = taskAmount;
            progressBar1.Maximum = taskAmount;
        }

        public void Print(string text)
        {
            richTextBox1.Text += text;
        }

        public void SetProgressBarValue(int newValue)
        {
            if (newValue > taskAmount || newValue < 0)
            {
                MessageBox.Show($"IllegalValue: Error, \"{newValue} is not in 0..100 range!\"", "Tsu-k Interruptive Window");
            }
            progressValue = newValue;
        }
        public void IncrementProgressBarValue()
        {
            if (progressValue + 1 > taskAmount)
            {
                return;
            }
            progressValue++;
        }

        private void SetProgress(string progress)
        {
            this.progress = progress;
        }

        public void SetStage(int state)
        {
            label2.Text = $"Progress: {state} out of {taskAmount}, {progress}:";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                richTextBox1.Visible = true;
                Size = new Size(483, 216);
            }
            else
            {
                richTextBox1.Visible = false;
                Size = new Size(483, 162);
            }
        }

        private void ProgressWindow_Load(object sender, EventArgs e)
        {

        }

        private void ProgressUpdater_Tick(object sender, EventArgs e)
        {
            progressBar1.Value = progressValue;
        }
    }
}
