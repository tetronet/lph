using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class Experiment_FromBitmapToVector : Form
    {
        private Bitmap bitmap = null;
        bool didUserSelectAFile = false;
        string saveOutputTo = "";
        int pixelsConverted = 0;
        int lastPixelsConverted = 0;
        StringBuilder output = new StringBuilder();
        public Experiment_FromBitmapToVector()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    bitmap = new Bitmap(openFileDialog1.FileName);
                    label1.Text = openFileDialog1.FileName;
                    didUserSelectAFile = true;
                }
                else
                {
                    Interaction.MsgBox("This file doesn't exist.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (didUserSelectAFile)
            {
                timer1.Start();
                progressBar1.Maximum = bitmap.Width * bitmap.Height;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    saveOutputTo = saveFileDialog1.FileName;
                }
                Task.Run(delegate ()
                {
                    for (int i = 0; i < bitmap.Height-1; i++)
                    {
                        for (int j = 0; j < bitmap.Width-1; j++)
                        {
                            try
                            {
                                decimal x_percent = (decimal)j / bitmap.Width * 100;
                                decimal y_percent = (decimal)i / bitmap.Height * 100;
                                decimal r_div = (decimal)bitmap.GetPixel(j, i).R / 255;
                                decimal g_div = (decimal)bitmap.GetPixel(j, i).G / 255;
                                decimal b_div = (decimal)bitmap.GetPixel(j, i).B / 255;
                                output.Append($"point {x_percent} {y_percent} {r_div} {g_div} {b_div}\n");
                                pixelsConverted++;
                            }
                            catch
                            {
                                
                            }
                        }
                    }
                    File.WriteAllText(saveOutputTo, output.ToString());
                    Interaction.MsgBox($"Successfully converted {bitmap.Width * bitmap.Height} to LPH", MsgBoxStyle.Information, "Tsu-k Interruptive Window");
                    pixelsConverted = 0;
                });
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                label2.Text = $"Pixels converted: {pixelsConverted} from {bitmap.Width * bitmap.Height} at rate of {pixelsConverted - lastPixelsConverted}";
                progressBar1.Value = pixelsConverted;
            }
            catch
            {
                
            }
            lastPixelsConverted = pixelsConverted;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
