using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class ExperimentsMainMenu : Form
    {
        public ExperimentsMainMenu()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Experiment_FromBitmapToVector().Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            new Experiment_DestroyPresicion().Show();
        }
    }
}
