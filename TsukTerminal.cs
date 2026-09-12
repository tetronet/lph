using System;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class TsukTerminal : Form
    {
        private string OutputText = "";
        public TsukTerminal(string text)
        {
            InitializeComponent();
            OutputText = text;
        }

        private void TsukTerminal_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = OutputText;
        }
    }
}
