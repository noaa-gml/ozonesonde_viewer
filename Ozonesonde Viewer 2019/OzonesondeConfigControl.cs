using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ozonesonde_Viewer_2019
{
    public partial class OzonesondeConfigControl : UserControl
    {
        private uint dcIndex;

        public OzonesondeConfigControl(OzonesondeConfig ozoneConfig)
        {
            this.dcIndex = ozoneConfig.DCIndex;
            InitializeComponent();

            groupBox1.Text = "Ozonesonde DC Index " + ozoneConfig.DCIndex;

            //daisyChainIndexLabel.Text = string.Format("( Daisy Chain Index: {0:d} )", ozoneConfig.DCIndex);
            cellBackgroundTextBox.Text = string.Format("{0:0.000}", ozoneConfig.CellBackground);
            pumpFlowrateTextBox.Text = string.Format("{0:0.000}", ozoneConfig.PumpFlowrate);
            rhFlowrateCorrTextBox.Text = string.Format("{0:0.000}", ozoneConfig.RHFlowrateCorr);

            //populate the pump efficiency combo box with all available options
            pumpEffComboBox.Items.AddRange(PumpEfficiency.PumpEfficiencyParser.PumpEfficiencyList.ToArray());

            //find the config's pump efficiency and set the combobox to this index
            int pumpEffIndex = PumpEfficiency.PumpEfficiencyParser.PumpEfficiencyList.FindIndex(y => (y.Name == ozoneConfig.PumpEfficiencyName));
            if ((pumpEffIndex < 0) || (pumpEffIndex >= PumpEfficiency.PumpEfficiencyParser.PumpEfficiencyList.Count))
                throw new Exception("Invalid pump efficiency: " + ozoneConfig.PumpEfficiencyName);
            pumpEffComboBox.SelectedIndex = pumpEffIndex;
        }

        public OzonesondeConfig GetOzonesondeConfig()
        {
            //uint daisyChainIndex = uint.Parse(daisyChainIndexTextBox.Text);

            if (!double.TryParse(cellBackgroundTextBox.Text, out double cellBackground))
                throw new Exception("Invalid cell background");
            if ((cellBackground < -10) || (cellBackground > 100)) throw new Exception("Invalid cell background range");

            if (!double.TryParse(pumpFlowrateTextBox.Text, out double pumpFlowrate))
                throw new Exception("Invalid pump flowrate");
            if ((pumpFlowrate <= 0) || (pumpFlowrate > 100)) throw new Exception("Invalid pump flowrate range");

            if (!double.TryParse(rhFlowrateCorrTextBox.Text, out double rhFlowrateCorrection))
                throw new Exception("Invalid rh flowrate correction");
            if ((rhFlowrateCorrection < 0) || (rhFlowrateCorrection > 100)) throw new Exception("Invalid rh flowrate correction range");

            var pumpEff = (PumpEfficiency.PumpEfficiency)pumpEffComboBox.SelectedItem;

            return new OzonesondeConfig(dcIndex, cellBackground, pumpFlowrate, rhFlowrateCorrection, pumpEff.Name);
        }
    }
}
