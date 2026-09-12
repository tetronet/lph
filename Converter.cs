using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class Converter : Form
    {
        private string filename = "";
        private string resultFilename = "";
        private int objectsConverted = 0;
        private int lastObjectsConverted = 0;
        private int erroredObjects = 0;
        public Converter()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // 775; 396
            numericUpDown1.Value = 775;
            numericUpDown2.Value = 396;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                label1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (filename == "")
                {
                    Interaction.MsgBox("Empty file name", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
                StringBuilder outputSVG = new StringBuilder();
                outputSVG.Append($"<svg width=\"{(int)numericUpDown1.Value}\" height=\"{(int)numericUpDown2.Value}\" viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\">\n");
                string toParse = File.ReadAllText(filename);
                string[] shapes = toParse.Split('\n');
                progressBar1.Maximum = shapes.Length;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    resultFilename = saveFileDialog1.FileName;
                }
                Task.Run(delegate ()
                {
                    foreach (string shape in shapes)
                    {
                        objectsConverted++;
                        string[] shapeData = shape.Split(' ');
                        switch (shapeData[0])
                        {
                            case "point":
                                try
                                {
                                    float x_percent_point = float.Parse(shapeData[1]);
                                    float y_percent_point = float.Parse(shapeData[2]);
                                    float r_pt = float.Parse(shapeData[3]);
                                    float g_pt = float.Parse(shapeData[4]);
                                    float b_pt = float.Parse(shapeData[5]);
                                    string color_pt = $"#{(int)(r_pt * 255):X2}{(int)(g_pt * 255):X2}{(int)(b_pt * 255):X2}";
                                    outputSVG.Append($"<circle cx=\"{x_percent_point}\" cy=\"{y_percent_point}\" r=\"1\" fill=\"{color_pt}\"/>\n");
                                }
                                catch
                                {
                                    erroredObjects++;
                                }
                                break;
                            case "line":
                                try
                                {
                                    float x_percent_line = float.Parse(shapeData[1]);
                                    float y_percent_line = float.Parse(shapeData[2]);
                                    float x_end_percent_line = float.Parse(shapeData[3]);
                                    float y_end_percent_line = float.Parse(shapeData[4]);
                                    float r_ln = float.Parse(shapeData[5]);
                                    float g_ln = float.Parse(shapeData[6]);
                                    float b_ln = float.Parse(shapeData[7]);
                                    string color_ln = $"#{(int)(r_ln * 255):X2}{(int)(g_ln * 255):X2}{(int)(b_ln * 255):X2}";
                                    outputSVG.Append($"<line x1=\"{x_percent_line}\" y1=\"{y_percent_line}\" x2=\"{x_end_percent_line}\" y2=\"{y_end_percent_line}\" stroke=\"{color_ln}\"/>\n");
                                }
                                catch
                                {
                                    erroredObjects++;
                                }
                                break;
                        }
                    }
                    outputSVG.Append("</svg>");
                    objectsConverted = 0;
                    lastObjectsConverted = 0;
                    erroredObjects = 0;
                    while (resultFilename == "")
                    {
                        Thread.Sleep(10);
                    }
                    File.WriteAllText(resultFilename, outputSVG.ToString().Replace(',', '.'));
                    Task.Run(() => Interaction.MsgBox($"Successfully converted {shapes.Length} objects from Line Picture Host to Scalable Vector Graphics", MsgBoxStyle.Information, "Tsu-k Interruptive Window"));
                    Array.Clear(shapes, 0, shapes.Length);
                    outputSVG.Clear();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void Converter_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value = objectsConverted;
            label4.Text = $"Object Rate: {objectsConverted - lastObjectsConverted} O/s";
            lastObjectsConverted = objectsConverted;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label5.Text = $"Failed to convert: {erroredObjects} objects";
        }
    }
}
