using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ozonesonde_Viewer_2019.PumpEfficiency
{
    /**
     * Calculate pump efficiency corrections using the SkySonde technique: linear interpolation between points of Bryan Johnson's latest 2Z.EFF correction set.  
     */
    class PressureCorrPairsPumpEfficiency : PumpEfficiency
    {
        protected List<double> pumpEffPressures;
        protected List<double> pumpEffCorrections;

        protected MathNet.Numerics.Interpolation.IInterpolation interpolationScheme;

        public PressureCorrPairsPumpEfficiency(string name, string description, List<double> pressures, List<double> corrections)
        {
            Name = name;
            Description = description;
            if (pressures.Count <= 0) throw new Exception("No pump efficiency pressures provided");
            if (pressures.Count != corrections.Count) throw new Exception("Number of pressures not equal to corrections");

            pumpEffPressures = pressures;
            pumpEffCorrections = corrections;

            //make sure the lists are ascending on the independent data
            if (pumpEffPressures[0] > pumpEffPressures[pumpEffPressures.Count - 1])
            {
                pumpEffPressures.Reverse();
                pumpEffCorrections.Reverse();
            }

            interpolationScheme = MathNet.Numerics.Interpolate.Linear(
                pumpEffPressures, pumpEffCorrections);
        }

        public override List<double> GetIndepValues()
        {
            return pumpEffPressures;
        }
        public override List<double> GetDepValues()
        {
            return pumpEffCorrections;
        }

        public override double GetPumpEfficiencyCorrection(double pressure)
        {
            return interpolationScheme.Interpolate(pressure);
        }

        public override int GetHeaderDescriptionOutput(out string description)
        {
            StringBuilder pressuresBuilder = new StringBuilder();
            StringBuilder correctionsBuilder = new StringBuilder();
            for (int i = 0; i < pumpEffPressures.Count; i++)
            {
                pressuresBuilder.Append(string.Format("{0:0.}", pumpEffPressures[i]));
                correctionsBuilder.Append(string.Format("{0:0.000}", pumpEffCorrections[i]));
                if (i < (pumpEffPressures.Count - 1))
                {
                    pressuresBuilder.Append(", ");
                    correctionsBuilder.Append(", ");
                }
            }

            description = string.Format(
                "               Coefficients = {0}" + Environment.NewLine + 
                "    Pump eff corr pressures = {1}" + Environment.NewLine +
                "       Pump eff corr values = {2}" + Environment.NewLine +
                "     Pump eff interpolation = linear", Name, pressuresBuilder.ToString(), correctionsBuilder.ToString());

            return 4;
        }
    }
}
