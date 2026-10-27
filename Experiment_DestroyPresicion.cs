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
    public partial class Experiment_DestroyPresicion : Form
    {
        private StringBuilder output = new StringBuilder();
        private string inputFilename = string.Empty;
        private string outputFilename = string.Empty;
        public Experiment_DestroyPresicion()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                outputFilename = saveFileDialog1.FileName;
                if (inputFilename == "")
                {
                    Interaction.MsgBox("Failed due to file path being empty, most likely caused by not selecting an input file.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                    return;
                }
                Task.Run(delegate ()
                {
                    string[] data = File.ReadAllText(inputFilename).Split('\n');
                    int errors = 0;
                    foreach (string s in data)
                    {
                        try
                        {
                            string[] parts = s.Split(' ');
                            string toAppend = "";
                            if (parts[0] == "line")
                            {
                                decimal x1 = Helpers.ParseDecimal(parts[1]);
                                decimal y1 = Helpers.ParseDecimal(parts[2]);
                                decimal x2 = Helpers.ParseDecimal(parts[3]);
                                decimal y2 = Helpers.ParseDecimal(parts[4]);
                                decimal r = Helpers.ParseDecimal(parts[5]);
                                decimal g = Helpers.ParseDecimal(parts[6]);
                                decimal b = Helpers.ParseDecimal(parts[7]);
                                toAppend = $"line " +
                                    $"{x1.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{y1.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{x2.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{y2.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{Helpers.ColorChannelToLph((int)(r * 255))} " +
                                    $"{Helpers.ColorChannelToLph((int)(g * 255))} " +
                                    $"{Helpers.ColorChannelToLph((int)(b * 255))}\n";
                            }
                            else if (parts[0] == "point")
                            {
                                decimal x = Helpers.ParseDecimal(parts[1]);
                                decimal y = Helpers.ParseDecimal(parts[2]);
                                decimal r = Helpers.ParseDecimal(parts[3]);
                                decimal g = Helpers.ParseDecimal(parts[4]);
                                decimal b = Helpers.ParseDecimal(parts[5]);
                                toAppend = $"point " +
                                    $"{x.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{y.ToString($"f{numericUpDown1.Value}")} " +
                                    $"{Helpers.ColorChannelToLph((int)(r * 255))} " +
                                    $"{Helpers.ColorChannelToLph((int)(g * 255))} " +
                                    $"{Helpers.ColorChannelToLph((int)(b * 255))}\n";
                            }
                            output.Append(toAppend);
                        }
                        catch
                        {
                            errors++;
                            Console.WriteLine($"skip invalid object: {s}");
                        }
                    }
                    File.WriteAllText(outputFilename, output.ToString());
                    Interaction.MsgBox($"Successfully changed presicion for {data.Length} with {errors} objects failed to convert.", MsgBoxStyle.Information, "Tsu-k Interruptive Window");
                });
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                inputFilename = openFileDialog1.FileName;
            }
        }
    }
}
