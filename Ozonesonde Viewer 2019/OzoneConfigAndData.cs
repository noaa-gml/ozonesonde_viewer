using System;
using System.Linq;

namespace Ozonesonde_Viewer_2019
{
    //ozonesonde configuration information from the setup GUI and json file
    public class OzonesondeConfig
    {
        public uint DCIndex { get; private set; }
        public double CellBackground { get; private set; }//[uA]
        public double PumpFlowrate { get; private set; }//[sec / 100 ml]
        public double RHFlowrateCorr { get; private set; }//[%]
        public string PumpEfficiencyName { get; private set; }

        public OzonesondeConfig(uint dcIndex, double cellBackground, double pumpFlowrate, double rhFlowrateCorr, string pumpEfficiencyName)
        {
            DCIndex = dcIndex;
            CellBackground = cellBackground;
            PumpFlowrate = pumpFlowrate;
            RHFlowrateCorr = rhFlowrateCorr;
            PumpEfficiencyName = pumpEfficiencyName;
        }
    }

    //storage for an ozonesonde data packet (after parsing), along with configuration info
    public class OzoneConfigAndData
    {
        //the configuration parameters for this ozonesonde, set at program startup in the ConfigForm dialog
        public OzonesondeConfig OzoneConfig { get; set; }

        public DateTime DateTimeStamp { get; set; }//utc

        public string ADBoardType { get; set; }//V7 or X1 for now

        //measurements
        public double CellCurrent { get; set; }//uA
        public double PumpTemperature { get; set; }//deg C
        public double PumpCurrent { get; set; }//mA
        public double BatteryVoltage { get; set; }//V
        public double PumpMotorRPM { get; set; }//RPM

        //calculations
        public double OzonePartialPressure { get; set; }//mPa
        public double OzoneMixingRatio { get; set; }//ppbv

        //set true after all the fields are filled out to indicate that file output should happen, set false when file output is complete
        public bool IsReadyForOutput { get; set; }

        public OzoneConfigAndData()
        {
            CellCurrent = double.NaN;
            PumpTemperature = double.NaN;
            PumpCurrent = double.NaN;
            BatteryVoltage = double.NaN;
            PumpMotorRPM = double.NaN;

            OzonePartialPressure = double.NaN;
            OzoneMixingRatio = double.NaN;

            IsReadyForOutput = false;
        }

        //calculate the ozone partial pressure and mixing ratio from the already-populated ozone fields stored in this class
        public void CalculatePartialPressureAndMixingRatio(double pressure)
        {
            bool pressureGood = !double.IsNaN(pressure) && (pressure > 0) && (pressure < 1200);

            //if the pressure is bad/missing, default to a pump efficiency of 1.0 (perfectly effecient)
            double effCorr = 1.0;
            if (pressureGood)
            {
                //get the pump efficiency class matching the config name (TODO: not very efficient to do this every time)
                var matchingPumpEffs = from p in PumpEfficiency.PumpEfficiencyParser.PumpEfficiencyList where (p.Name == OzoneConfig.PumpEfficiencyName) select p;
                if (matchingPumpEffs.Count() != 1) throw new Exception("Could not find matching pump efficiency correction");
                var pumpEff = matchingPumpEffs.First();
                effCorr = pumpEff.GetPumpEfficiencyCorrection(pressure);

                if (double.IsNaN(effCorr) || (effCorr < 0.5) || (effCorr > 3)) throw new Exception("Invalid pump efficiency correction value: " + effCorr);
            }

            //calculate partial pressure
            //NOTE: no pump efficiency correction applied since this program expects ground-level ozonesondes
            double correctedFlowrate = effCorr * OzoneConfig.PumpFlowrate * (1 + OzoneConfig.RHFlowrateCorr / 100);

            OzonePartialPressure =
                4.3085E-4 * (CellCurrent - OzoneConfig.CellBackground) * (PumpTemperature + 273.15) * correctedFlowrate;

            //calculate mixing ratio if we have a good pressure
            if (pressureGood)
                OzoneMixingRatio = OzonePartialPressure / pressure * 10 * 1000;//the last * 1000 converts to ppb
        }
    }

}
