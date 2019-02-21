using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ozonesonde_Viewer_2019.PumpEfficiency
{
    public static class PumpEfficiencyParser
    {
        public static List<PumpEfficiency> PumpEfficiencyList { get; private set; }

        /**
             * Read in the ozonesonde pump efficiency corrections from pumpEff.xml, and store them in a list for later use.  
             */
        public static void LoadXMLPumpEfficiencyCorrections()
        {
            PumpEfficiencyList = new List<PumpEfficiency>();
            char[] commaSplitter = new char[] { ',' };

            var filename = "pumpEff.xml";
            XElement pumpEffXML = XElement.Load(filename);

            var pumpEffs = pumpEffXML.Descendants("pumpEffCorrection"); //from p in pumpEffXML.Descendants("pumpEffCorrection") select p;
            foreach (var pumpEff in pumpEffs)
            {
                var name = pumpEff.Descendants("name").First().Value;
                var description = pumpEff.Descendants("description").First().Value;
                var type = pumpEff.Descendants("type").First().Value;

                PumpEfficiency pumpEffObj = null;
                if (type == "Cubic Polynomial Terms")
                {
                    double pc0 = double.Parse(pumpEff.Descendants("PC0").First().Value);
                    double pc1 = double.Parse(pumpEff.Descendants("PC1").First().Value);
                    double pc2 = double.Parse(pumpEff.Descendants("PC2").First().Value);
                    double pc3 = double.Parse(pumpEff.Descendants("PC3").First().Value);

                    pumpEffObj = new CubicCoeffPumpEfficiency(name, description, pc0, pc1, pc2, pc3);
                }
                else if (type == "Pressure-Correction Pairs")
                {
                    var pressArr = pumpEff.Descendants("pressures").First().Value.Split(commaSplitter).Select(s => double.Parse(s)).ToList();
                    var corrArr = pumpEff.Descendants("corrections").First().Value.Split(commaSplitter).Select(s => double.Parse(s)).ToList();

                    pumpEffObj = new PressureCorrPairsPumpEfficiency(name, description, pressArr, corrArr);
                }
                else throw new Exception("Invalid pump eff type: " + type);

                if (pumpEffObj != null) PumpEfficiencyList.Add(pumpEffObj);
            }
        }
    }
}
