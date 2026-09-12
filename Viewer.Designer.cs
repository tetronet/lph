namespace LPH_Edit_Viewer
{
    partial class Viewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Viewer));
            button1 = new Button();
            button2 = new Button();
            panel1 = new Panel();
            button3 = new Button();
            button4 = new Button();
            label1 = new Label();
            RenderedObjectCountUpdater = new System.Windows.Forms.Timer(components);
            ObjectRateUpdater = new System.Windows.Forms.Timer(components);
            ScreenUpdater = new System.Windows.Forms.Timer(components);
            progressBar1 = new ProgressBar();
            button5 = new Button();
            button6 = new Button();
            saveFileDialog1 = new SaveFileDialog();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(16, 638);
            button1.Margin = new Padding(4, 5, 4, 5);
            button1.Name = "button1";
            button1.Size = new Size(66, 35);
            button1.TabIndex = 0;
            button1.Text = "Edit...";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(90, 638);
            button2.Margin = new Padding(4, 5, 4, 5);
            button2.Name = "button2";
            button2.Size = new Size(61, 35);
            button2.TabIndex = 1;
            button2.Text = "Quit";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(17, 20);
            panel1.Margin = new Padding(4, 5, 4, 5);
            panel1.Name = "panel1";
            panel1.Size = new Size(1033, 608);
            panel1.TabIndex = 2;
            panel1.Paint += panel1_Paint;
            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseMove += panel1_MouseMove;
            panel1.MouseUp += panel1_MouseUp;
            // 
            // button3
            // 
            button3.Location = new Point(159, 638);
            button3.Margin = new Padding(4, 5, 4, 5);
            button3.Name = "button3";
            button3.Size = new Size(244, 35);
            button3.TabIndex = 3;
            button3.Text = "Open this host to the network";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new Point(411, 638);
            button4.Margin = new Padding(4, 5, 4, 5);
            button4.Name = "button4";
            button4.Size = new Size(131, 35);
            button4.TabIndex = 4;
            button4.Text = "Force Redraw";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(732, 645);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(224, 20);
            label1.TabIndex = 5;
            label1.Text = "Objects Rendered: 0 (rate: 0 O/s)";
            // 
            // RenderedObjectCountUpdater
            // 
            RenderedObjectCountUpdater.Enabled = true;
            RenderedObjectCountUpdater.Interval = 10;
            RenderedObjectCountUpdater.Tick += RenderedObjectCountUpdater_Tick;
            // 
            // ObjectRateUpdater
            // 
            ObjectRateUpdater.Enabled = true;
            ObjectRateUpdater.Interval = 1000;
            ObjectRateUpdater.Tick += ObjectRateUpdater_Tick;
            // 
            // ScreenUpdater
            // 
            ScreenUpdater.Enabled = true;
            ScreenUpdater.Interval = 50;
            ScreenUpdater.Tick += ScreenUpdater_Tick;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(17, 681);
            progressBar1.Margin = new Padding(4, 5, 4, 5);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1033, 35);
            progressBar1.TabIndex = 6;
            // 
            // button5
            // 
            button5.Location = new Point(550, 638);
            button5.Margin = new Padding(4, 5, 4, 5);
            button5.Name = "button5";
            button5.Size = new Size(100, 35);
            button5.TabIndex = 7;
            button5.Text = "See buffer";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // button6
            // 
            button6.Location = new Point(657, 638);
            button6.Name = "button6";
            button6.Size = new Size(68, 35);
            button6.TabIndex = 8;
            button6.Text = "Save";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.Filter = "LIne Picture Host|*.lph|All files|*.*";
            // 
            // Viewer
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1067, 734);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(progressBar1);
            Controls.Add(label1);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(panel1);
            Controls.Add(button2);
            Controls.Add(button1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "Viewer";
            Text = "LPH Viewer";
            FormClosing += Viewer_FormClosing;
            Load += Viewer_Load;
            Shown += Viewer_Shown;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer RenderedObjectCountUpdater;
        private System.Windows.Forms.Timer ObjectRateUpdater;
        private System.Windows.Forms.Timer ScreenUpdater;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button button5;
        private Button button6;
        private SaveFileDialog saveFileDialog1;
    }
}