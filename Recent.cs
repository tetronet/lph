using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class Recent : Form
    {
        private string[] filelist;
        public Recent(string[] filelist)
        {
            InitializeComponent();
            this.filelist = filelist;
        }
        private void Recent_Load(object sender, EventArgs e)
        {
            bool trueIfNullOrEmpty(string s)
            {
                return s == null || s.Length == 0 || s.Trim().Length == s.Length;
            }
            listBox1.Items.AddRange(filelist.Reverse().Where(trueIfNullOrEmpty).ToArray());
        }

        // don't delete, this feature is for future!
        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                listBox1.Items.Remove(listBox1.SelectedItem);
                string oldRecentList = File.ReadAllText("Recent.text");
                string newRecentList = oldRecentList.Replace((string)listBox1.SelectedItem, "");
                Console.WriteLine(oldRecentList);
                Console.WriteLine(newRecentList);
                Console.WriteLine(listBox1.SelectedItem + "\n");
                File.WriteAllText("Recent.text", newRecentList);
            }
            else
            {
                MessageBox.Show("ElementNotSelected: Error, \"There are no selected elems, try to select elems, that you wanna remove from recent.\"", "Tsu-k Interruptive Window");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("NullValue: Error, \"listBox1.SelectedValue is null!!!!\"", "Tsu-k Interruptive Window");
            }
            else
            {
                new Viewer(listBox1.SelectedItem.ToString()).Show();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("NullValue: Error, \"listBox1.SelectedValue is null!!!!\"", "Tsu-k Interruptive Window");
            }
            else
            {
                new Edit(listBox1.SelectedItem.ToString(), true).Show();
            }
        }
    }
}
