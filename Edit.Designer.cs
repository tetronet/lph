namespace LPH_Edit_Viewer
{
    partial class Edit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Edit));
            panel1 = new Panel();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            button6 = new Button();
            button7 = new Button();
            saveFileDialog1 = new SaveFileDialog();
            button8 = new Button();
            BrushTimer = new System.Windows.Forms.Timer(components);
            button5 = new Button();
            button9 = new Button();
            button10 = new Button();
            button11 = new Button();
            button12 = new Button();
            button13 = new Button();
            button14 = new Button();
            colorDialog1 = new ColorDialog();
            button15 = new Button();
            CursorUpdater = new System.Windows.Forms.Timer(components);
            ServerPingTimer = new System.Windows.Forms.Timer(components);
            button16 = new Button();
            button17 = new Button();
            button18 = new Button();
            groupBox1 = new GroupBox();
            button21 = new Button();
            button19 = new Button();
            groupBox2 = new GroupBox();
            groupBox3 = new GroupBox();
            button20 = new Button();
            groupBox4 = new GroupBox();
            groupBox5 = new GroupBox();
            button23 = new Button();
            ScreenUpdater = new System.Windows.Forms.Timer(components);
            groupBox6 = new GroupBox();
            button22 = new Button();
            button24 = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox5.SuspendLayout();
            groupBox6.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(16, 18);
            panel1.Margin = new Padding(4, 5, 4, 5);
            panel1.Name = "panel1";
            panel1.Size = new Size(1033, 608);
            panel1.TabIndex = 3;
            panel1.Paint += panel1_Paint;
            panel1.MouseClick += panel1_MouseClick;
            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseUp += panel1_MouseUp;
            // 
            // button1
            // 
            button1.Location = new Point(8, 29);
            button1.Margin = new Padding(4, 5, 4, 5);
            button1.Name = "button1";
            button1.Size = new Size(100, 35);
            button1.TabIndex = 4;
            button1.Text = "Clear";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(8, 29);
            button2.Margin = new Padding(4, 5, 4, 5);
            button2.Name = "button2";
            button2.Size = new Size(100, 35);
            button2.TabIndex = 5;
            button2.Text = "Draw line";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(116, 29);
            button3.Margin = new Padding(4, 5, 4, 5);
            button3.Name = "button3";
            button3.Size = new Size(100, 35);
            button3.TabIndex = 6;
            button3.Text = "Draw point";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new Point(224, 29);
            button4.Margin = new Padding(4, 5, 4, 5);
            button4.Name = "button4";
            button4.Size = new Size(135, 35);
            button4.TabIndex = 7;
            button4.Text = "Draw rectangle";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // button6
            // 
            button6.Location = new Point(367, 29);
            button6.Margin = new Padding(4, 5, 4, 5);
            button6.Name = "button6";
            button6.Size = new Size(100, 35);
            button6.TabIndex = 9;
            button6.Text = "Brush";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // button7
            // 
            button7.Location = new Point(951, 1057);
            button7.Margin = new Padding(4, 5, 4, 5);
            button7.Name = "button7";
            button7.Size = new Size(100, 35);
            button7.TabIndex = 10;
            button7.Text = "Save...";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.FileName = "Save your LPH";
            saveFileDialog1.Filter = "Line Picture Host|*.lph";
            // 
            // button8
            // 
            button8.Location = new Point(475, 29);
            button8.Margin = new Padding(4, 5, 4, 5);
            button8.Name = "button8";
            button8.Size = new Size(124, 35);
            button8.TabIndex = 11;
            button8.Text = "Ended Line";
            button8.UseVisualStyleBackColor = true;
            button8.Click += button8_Click;
            // 
            // BrushTimer
            // 
            BrushTimer.Interval = 20;
            BrushTimer.Tick += BrushTimer_Tick;
            // 
            // button5
            // 
            button5.Location = new Point(116, 29);
            button5.Margin = new Padding(4, 5, 4, 5);
            button5.Name = "button5";
            button5.Size = new Size(148, 35);
            button5.TabIndex = 12;
            button5.Text = "Cancel last";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click_1;
            // 
            // button9
            // 
            button9.Location = new Point(8, 29);
            button9.Margin = new Padding(4, 5, 4, 5);
            button9.Name = "button9";
            button9.Size = new Size(140, 35);
            button9.TabIndex = 13;
            button9.Text = "Force Redraw";
            button9.UseVisualStyleBackColor = true;
            button9.Click += button9_Click;
            // 
            // button10
            // 
            button10.Location = new Point(8, 29);
            button10.Margin = new Padding(4, 5, 4, 5);
            button10.Name = "button10";
            button10.Size = new Size(143, 35);
            button10.TabIndex = 14;
            button10.Text = "Serial Port Options";
            button10.UseVisualStyleBackColor = true;
            button10.Click += button10_Click;
            // 
            // button11
            // 
            button11.Location = new Point(8, 74);
            button11.Margin = new Padding(4, 5, 4, 5);
            button11.Name = "button11";
            button11.Size = new Size(225, 35);
            button11.TabIndex = 15;
            button11.Text = "Line sample rate (brush only)";
            button11.UseVisualStyleBackColor = true;
            button11.Click += button11_Click;
            // 
            // button12
            // 
            button12.Location = new Point(411, 29);
            button12.Margin = new Padding(4, 5, 4, 5);
            button12.Name = "button12";
            button12.Size = new Size(169, 35);
            button12.TabIndex = 16;
            button12.Text = "Send Data To The Port";
            button12.UseVisualStyleBackColor = true;
            button12.Click += button12_Click;
            // 
            // button13
            // 
            button13.Enabled = false;
            button13.Location = new Point(588, 29);
            button13.Margin = new Padding(4, 5, 4, 5);
            button13.Name = "button13";
            button13.Size = new Size(89, 35);
            button13.TabIndex = 17;
            button13.Text = "Connect";
            button13.UseVisualStyleBackColor = true;
            button13.Click += button13_Click;
            // 
            // button14
            // 
            button14.Location = new Point(607, 29);
            button14.Margin = new Padding(4, 5, 4, 5);
            button14.Name = "button14";
            button14.Size = new Size(60, 35);
            button14.TabIndex = 18;
            button14.Text = "Color";
            button14.UseVisualStyleBackColor = true;
            button14.Click += button14_Click;
            // 
            // button15
            // 
            button15.Location = new Point(843, 1057);
            button15.Margin = new Padding(4, 5, 4, 5);
            button15.Name = "button15";
            button15.Size = new Size(100, 35);
            button15.TabIndex = 19;
            button15.Text = "Exit";
            button15.UseVisualStyleBackColor = true;
            button15.Click += button15_Click;
            // 
            // CursorUpdater
            // 
            CursorUpdater.Enabled = true;
            CursorUpdater.Interval = 20;
            CursorUpdater.Tick += CursorUpdater_Tick;
            // 
            // ServerPingTimer
            // 
            ServerPingTimer.Interval = 2000;
            ServerPingTimer.Tick += ServerPingTimer_Tick;
            // 
            // button16
            // 
            button16.Location = new Point(8, 74);
            button16.Margin = new Padding(4, 5, 4, 5);
            button16.Name = "button16";
            button16.Size = new Size(141, 35);
            button16.TabIndex = 20;
            button16.Text = "Ping The Server";
            button16.UseVisualStyleBackColor = true;
            button16.Click += button16_Click;
            // 
            // button17
            // 
            button17.Location = new Point(159, 29);
            button17.Margin = new Padding(4, 5, 4, 5);
            button17.Name = "button17";
            button17.Size = new Size(244, 35);
            button17.TabIndex = 21;
            button17.Text = "Remote Buffer --> Local Buffer";
            button17.UseVisualStyleBackColor = true;
            button17.Click += button17_Click;
            // 
            // button18
            // 
            button18.Location = new Point(8, 29);
            button18.Margin = new Padding(4, 5, 4, 5);
            button18.Name = "button18";
            button18.Size = new Size(100, 35);
            button18.TabIndex = 22;
            button18.Text = "See buffer";
            button18.UseVisualStyleBackColor = true;
            button18.Click += button18_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(button24);
            groupBox1.Controls.Add(button21);
            groupBox1.Controls.Add(button19);
            groupBox1.Controls.Add(button2);
            groupBox1.Controls.Add(button3);
            groupBox1.Controls.Add(button4);
            groupBox1.Controls.Add(button6);
            groupBox1.Controls.Add(button8);
            groupBox1.Controls.Add(button14);
            groupBox1.Location = new Point(17, 637);
            groupBox1.Margin = new Padding(4, 5, 4, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 5, 4, 5);
            groupBox1.Size = new Size(1033, 85);
            groupBox1.TabIndex = 23;
            groupBox1.TabStop = false;
            groupBox1.Text = "Drawing";
            // 
            // button21
            // 
            button21.Location = new Point(804, 29);
            button21.Margin = new Padding(4, 5, 4, 5);
            button21.Name = "button21";
            button21.Size = new Size(75, 35);
            button21.TabIndex = 20;
            button21.Text = "Ruler";
            button21.UseVisualStyleBackColor = true;
            button21.Click += button21_Click;
            // 
            // button19
            // 
            button19.Location = new Point(675, 29);
            button19.Margin = new Padding(4, 5, 4, 5);
            button19.Name = "button19";
            button19.Size = new Size(121, 35);
            button19.TabIndex = 19;
            button19.Text = "Fill with Lines";
            button19.UseVisualStyleBackColor = true;
            button19.Click += button19_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(button5);
            groupBox2.Location = new Point(17, 731);
            groupBox2.Margin = new Padding(4, 5, 4, 5);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 5, 4, 5);
            groupBox2.Size = new Size(275, 83);
            groupBox2.TabIndex = 24;
            groupBox2.TabStop = false;
            groupBox2.Text = "Massive Changes";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(button20);
            groupBox3.Controls.Add(button10);
            groupBox3.Controls.Add(button17);
            groupBox3.Controls.Add(button12);
            groupBox3.Controls.Add(button13);
            groupBox3.Controls.Add(button16);
            groupBox3.Location = new Point(300, 731);
            groupBox3.Margin = new Padding(4, 5, 4, 5);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(4, 5, 4, 5);
            groupBox3.Size = new Size(751, 128);
            groupBox3.TabIndex = 25;
            groupBox3.TabStop = false;
            groupBox3.Text = "Network";
            // 
            // button20
            // 
            button20.Location = new Point(157, 74);
            button20.Margin = new Padding(4, 5, 4, 5);
            button20.Name = "button20";
            button20.Size = new Size(100, 35);
            button20.TabIndex = 22;
            button20.Text = "Disconnect";
            button20.UseVisualStyleBackColor = true;
            button20.Click += button20_Click;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(button9);
            groupBox4.Controls.Add(button11);
            groupBox4.Location = new Point(17, 823);
            groupBox4.Margin = new Padding(4, 5, 4, 5);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(4, 5, 4, 5);
            groupBox4.Size = new Size(275, 125);
            groupBox4.TabIndex = 26;
            groupBox4.TabStop = false;
            groupBox4.Text = "Graphics";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(button23);
            groupBox5.Controls.Add(button18);
            groupBox5.Location = new Point(300, 868);
            groupBox5.Margin = new Padding(4, 5, 4, 5);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(4, 5, 4, 5);
            groupBox5.Size = new Size(751, 80);
            groupBox5.TabIndex = 27;
            groupBox5.TabStop = false;
            groupBox5.Text = "Debug";
            // 
            // button23
            // 
            button23.Location = new Point(116, 29);
            button23.Margin = new Padding(4, 5, 4, 5);
            button23.Name = "button23";
            button23.Size = new Size(141, 35);
            button23.TabIndex = 23;
            button23.Text = "Overlay debug";
            button23.UseVisualStyleBackColor = true;
            button23.Click += button23_Click;
            // 
            // ScreenUpdater
            // 
            ScreenUpdater.Enabled = true;
            ScreenUpdater.Interval = 20;
            ScreenUpdater.Tick += ScreenUpdater_Tick;
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(button22);
            groupBox6.Location = new Point(17, 957);
            groupBox6.Margin = new Padding(4, 5, 4, 5);
            groupBox6.Name = "groupBox6";
            groupBox6.Padding = new Padding(4, 5, 4, 5);
            groupBox6.Size = new Size(1032, 91);
            groupBox6.TabIndex = 28;
            groupBox6.TabStop = false;
            groupBox6.Text = "Display";
            // 
            // button22
            // 
            button22.Location = new Point(8, 29);
            button22.Margin = new Padding(4, 5, 4, 5);
            button22.Name = "button22";
            button22.Size = new Size(225, 35);
            button22.TabIndex = 0;
            button22.Text = "Show overlay/cursors: True";
            button22.UseVisualStyleBackColor = true;
            button22.Click += button22_Click;
            // 
            // button24
            // 
            button24.Location = new Point(886, 28);
            button24.Name = "button24";
            button24.Size = new Size(84, 36);
            button24.TabIndex = 21;
            button24.Text = "Triangle";
            button24.UseVisualStyleBackColor = true;
            button24.Click += button24_Click;
            // 
            // Edit
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1067, 1111);
            Controls.Add(groupBox6);
            Controls.Add(groupBox5);
            Controls.Add(groupBox4);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(button15);
            Controls.Add(button7);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "Edit";
            Text = "LPH Editor";
            FormClosing += Edit_FormClosing;
            Load += Edit_Load;
            Shown += Edit_Shown;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            groupBox6.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Timer BrushTimer;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button button15;
        private System.Windows.Forms.Timer CursorUpdater;
        private System.Windows.Forms.Timer ServerPingTimer;
        private System.Windows.Forms.Button button16;
        private System.Windows.Forms.Button button17;
        private System.Windows.Forms.Button button18;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button19;
        private System.Windows.Forms.Timer ScreenUpdater;
        private System.Windows.Forms.Button button20;
        private System.Windows.Forms.Button button21;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Button button22;
        private System.Windows.Forms.Button button23;
        private Button button24;
    }
}