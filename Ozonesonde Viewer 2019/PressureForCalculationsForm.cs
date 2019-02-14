using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ozonesonde_Viewer_2019
{
    public partial class PressureForCalculationsForm : Form
    {
        public double Pressure { get; private set; }

        public PressureForCalculationsForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            string pressureStr = pressureTextBox.Text;

            if (!double.TryParse(pressureStr, out double pressure))
            {
                MessageBox.Show(this, "Could not parse pressure", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if ((pressure <= 0) || (pressure > 2000))
            {
                MessageBox.Show(this, "Invalid pressure range", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Pressure = pressure;

            this.DialogResult = DialogResult.OK;
        }
    }
}
