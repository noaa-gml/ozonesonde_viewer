using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ozonesonde_Viewer_2019.PumpEfficiency
{
    /**
     * Calculate pump efficiency corrections from the strato-style cubic polynomial fit interpolation.  
     * This follows the equation corr = pc0 + pc1*x + pc2*x^2 + pc3*x^3        (where x = 1 + 1/ln(p))
     */
    class CubicCoeffPumpEfficiency : PumpEfficiency
    {
        protected double PC0, PC1, PC2, PC3;

        /**
         * Default constructor assigns the standard pump efficiency correction factors from strato.  
         * todo: remove or change to 2znoaa
         */
        public CubicCoeffPumpEfficiency()
        {
            Name = "Strato Default";
            this.PC0 = 5.475537;
            this.PC1 = -10.432013;
            this.PC2 = 7.774825;
            this.PC3 = -1.818522;
        }

        /**
         * Initialize with custom cubic polynomial pump efficiency coefficients.  
         * 
         * @param   PC0     The x^0 term.  
         * @param   PC1     The x^1 term.  
         * @param   PC2     The x^2 term.  
         * @param   PC3     The x^3 term.  
         */
        public CubicCoeffPumpEfficiency(string name, string description, double PC0, double PC1, double PC2, double PC3)
        {
            Name = name;
            Description = description;
            this.PC0 = PC0;
            this.PC1 = PC1;
            this.PC2 = PC2;
            this.PC3 = PC3;
        }

        public override List<double> GetIndepValues()
        {
            return new List<double> { 0, 1, 2, 3 };
        }
        public override List<double> GetDepValues()
        {
            return new List<double> { PC0, PC1, PC2, PC3 };
        }

        public override double GetPumpEfficiencyCorrection(double pressure)
        {
            //this threshold keeps some of the polynomial corrections from dropping low at high pressures
            //note that this is a historical correction and needs to be kept in place for cell current back-calculation in older files
            if ((pressure <= 0) || (pressure >= 300)) return 1;

            double x = 1.0 + 1.0 / Math.Log(pressure);
            return PC0 + PC1 * x + PC2 * Math.Pow(x, 2) + PC3 * Math.Pow(x, 3);
        }

        public override int GetHeaderDescriptionOutput(out string description)
        {
            description = string.Format(
                "               Coefficients = {0}" + Environment.NewLine + 
                "       Pump coefficient pc0 = {1:0.0000}" + Environment.NewLine +
                "                        pc1 = {2:0.0000}" + Environment.NewLine +
                "                        pc2 = {3:0.0000}" + Environment.NewLine +
                "                        pc3 = {4:0.0000}", Name, PC0, PC1, PC2, PC3);
            return 5;
        }
    }
}
