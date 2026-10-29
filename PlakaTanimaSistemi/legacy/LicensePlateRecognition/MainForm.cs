using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace akıllıgecissistemleri
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        public void setImage(Image receivedImage)
        {
            pictureBox1.Image = receivedImage;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
