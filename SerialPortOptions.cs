using Microsoft.VisualBasic;
using System;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class SerialPortOptions : Form
    {
        private Action<SerialPortOptions> OnSubmitAction = delegate (SerialPortOptions x) { };
        public string SerialPortName = "";
        public bool UsingEncryption = false;
        public bool AuthReqired = false;
        public string Login = "";
        public string Password = "";
        public int DataBitrate = 0;
        public bool UseNetwork = true;
        public bool OpenNetworkPortWithoutConnecting = false;
        public bool TetronetAddress = false;

        public SerialPortOptions()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                textBox3.Enabled = true;
                textBox4.Enabled = true;
            }
            else
            {
                textBox3.Enabled = false;
                textBox4.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SerialPortName = textBox1.Text;
            UsingEncryption = checkBox1.Checked;
            AuthReqired = checkBox2.Checked;
            Login = textBox3.Text;
            Password = textBox4.Text;
            DataBitrate = (int)numericUpDown1.Value;
            OpenNetworkPortWithoutConnecting = checkBox3.Checked;
            TetronetAddress = checkBox4.Checked;
            OnSubmitAction(this);
            Close();
        }
        public void OnSubmit(Action<SerialPortOptions> onSubmit)
        {
            OnSubmitAction = onSubmit;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UseNetwork = false;
            Task.Run(() => OnSubmitAction(this));
            Close();
        }

        private void SerialPortOptions_Load(object sender, EventArgs e)
        {

        }
        private bool wasUserWarnedOfLargePacketRate = false;

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value > 1000 && !wasUserWarnedOfLargePacketRate)
            {
                if (Interaction.MsgBox("WARNING: using packet rate value more than 1000 p/s is dangerous for the network. This can crash network and forcing the user to stop drawing. Try out values higher than 1000 p/s on our own risk. LPH developers are not responsilbe for the equipment damage and/or money loss due to packet rate being absurdly high. Are you willing to proceed?", MsgBoxStyle.OkCancel, "Tsu-k Interruptive Window") == MsgBoxResult.Ok)
                {
                    wasUserWarnedOfLargePacketRate = true;
                }
                else
                {
                    numericUpDown1.Value = 1000;
                }
            }
        }
    }
}
