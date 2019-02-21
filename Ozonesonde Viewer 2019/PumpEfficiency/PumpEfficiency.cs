using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ozonesonde_Viewer_2019.PumpEfficiency
{
    public abstract class PumpEfficiency
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }

        public abstract List<double> GetIndepValues();
        public abstract List<double> GetDepValues();

        public abstract double GetPumpEfficiencyCorrection(double pressure);

        /**
         * Get a string (formatted for de1/fle output) describing the pump efficiency correction equations/coefficients used.  
         * 
         * @param description       The resulting description string.  
         * @return                  The number of lines the description will occupy in the header.  
         */
        public abstract int GetHeaderDescriptionOutput(out string description);

        public override string ToString()
        {
            return Name;
        }
    }
}
