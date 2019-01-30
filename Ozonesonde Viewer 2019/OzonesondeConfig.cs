using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ozonesonde_Viewer_2019
{
    public class OzonesondeConfig
    {
        public uint DCIndex { get; private set; }
        public double CellBackground { get; private set; }//[uA]
        public double PumpFlowrate { get; private set; }//[sec / 100 ml]
        public double RHFlowrateCorr { get; private set; }//[%]

        public OzonesondeConfig(uint dcIndex, double cellBackground, double pumpFlowrate, double rhFlowrateCorr)
        {
            DCIndex = dcIndex;
            CellBackground = cellBackground;
            PumpFlowrate = pumpFlowrate;
            RHFlowrateCorr = rhFlowrateCorr;
        }
    }
}
