using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public partial class Splash : Form
    {
        public Splash()
        {
            InitializeComponent();
        }

        private void Splash_Load( object sender, EventArgs e )
        {
            // show the version
            lblVersion.Text = "ver. " + ProductVersion;
        }
    }
}
