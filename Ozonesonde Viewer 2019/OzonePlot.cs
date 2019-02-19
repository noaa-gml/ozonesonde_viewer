using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Ozonesonde_Viewer_2019
{
    public partial class OzonePlot : Form
    {
        private List<ZedGraph.RollingPointPairList> o3mrListList;
        private List<ZedGraph.RollingPointPairList> o3ppListList;
        private List<ZedGraph.RollingPointPairList> cellIListList;
        private List<ZedGraph.RollingPointPairList> pumpTempListList;
        private List<ZedGraph.RollingPointPairList> pumpIListList;
        private List<ZedGraph.RollingPointPairList> batVListList;
        private List<ZedGraph.RollingPointPairList> pumpMotorRPMListList;

        private List<Color> colorList;

        public OzonePlot(List<OzoneConfigAndData> ocadList)
        {
            InitializeComponent();

            //read in the color list
            colorList = new List<Color>();
            StreamReader colorReader = new StreamReader("bright_rainbow.csv");
            while (!colorReader.EndOfStream)
            {
                var split = colorReader.ReadLine().Split(new char[] { ',' });
                if (split.Count() == 3)
                    colorList.Add(Color.FromArgb(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2])));
            }
            colorReader.Close();

            //setup the basic plot elements
            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.Legend.Position = ZedGraph.LegendPos.InsideTopLeft;
            zedGraphControl1.GraphPane.XAxis.Type = ZedGraph.AxisType.Date;
            zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;

            //setup the x and y axes and curves
            SetXRange(500, ocadList.Count);//todo: remember in properties.settings?
            yAxisComboBox.SelectedIndex = 1;
            //SetYAxisName("Ozone Partial Pressure");
        }

        private delegate void AddOzoneDataPointDelegate(OzoneConfigAndData ocad);
        public void AddOzoneDataPoint(OzoneConfigAndData ocad)
        {
            if (zedGraphControl1.InvokeRequired)
            {
                zedGraphControl1.BeginInvoke(new AddOzoneDataPointDelegate(AddOzoneDataPoint), new object[] { ocad });
            }
            else
            {
                if (ocad.OzoneConfig == null) return;
                uint dcIndex = ocad.OzoneConfig.DCIndex;
                int ozoneIndex = ((int)dcIndex) - 1;
                if (ozoneIndex >= o3mrListList.Count) throw new Exception("Ozonesonde index greater than plot data storage setup initially");

                UpdateOzoneRollingList(o3mrListList[ozoneIndex], ocad.DateTimeStamp, ocad.OzoneMixingRatio);
                UpdateOzoneRollingList(o3ppListList[ozoneIndex], ocad.DateTimeStamp, ocad.OzonePartialPressure);
                UpdateOzoneRollingList(cellIListList[ozoneIndex], ocad.DateTimeStamp, ocad.CellCurrent);
                UpdateOzoneRollingList(pumpTempListList[ozoneIndex], ocad.DateTimeStamp, ocad.PumpTemperature);
                UpdateOzoneRollingList(pumpIListList[ozoneIndex], ocad.DateTimeStamp, ocad.PumpCurrent);
                UpdateOzoneRollingList(batVListList[ozoneIndex], ocad.DateTimeStamp, ocad.BatteryVoltage);
                UpdateOzoneRollingList(pumpMotorRPMListList[ozoneIndex], ocad.DateTimeStamp, ocad.PumpMotorRPM);

                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
            }
        }
        private void UpdateOzoneRollingList(ZedGraph.RollingPointPairList rollingList, DateTime dateTime, double yValue)
        {
            if ((dateTime == DateTime.MinValue) || (dateTime == DateTime.MaxValue)) return;
            if (double.IsNaN(yValue) || double.IsInfinity(yValue)) return;

            rollingList.Add(dateTime.ToOADate(), yValue);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            for (int i=0; i<o3mrListList.Count; i++)
            {
                o3mrListList[i].Clear();
                o3ppListList[i].Clear();
                cellIListList[i].Clear();
                pumpTempListList[i].Clear();
                pumpIListList[i].Clear();
                batVListList[i].Clear();
                pumpMotorRPMListList[i].Clear();
            }
            zedGraphControl1.Refresh();
        }

        private void yAxisComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetYAxisName(yAxisComboBox.Text);
        }
        private void SetYAxisName(string yAxisName)
        {
            //clear previous curves
            zedGraphControl1.GraphPane.CurveList.Clear();

            //select the set of rolling data lists that matches this y axis name
            List<ZedGraph.RollingPointPairList> dataListList = null;
            switch (yAxisName)
            {
                case "Ozone Mixing Ratio":
                    dataListList = o3mrListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Ozone Mixing Ratio [ppbv]";
                    break;
                case "Ozone Partial Pressure":
                    dataListList = o3ppListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Ozone Partial Pressure [mPa]";
                    break;
                case "Cell Current":
                    dataListList = cellIListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Cell Current [uA]";
                    break;
                case "Pump Temperature":
                    dataListList = pumpTempListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Pump Temperature [deg C]";
                    break;
                case "Pump Current":
                    dataListList = pumpIListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Pump Current [mA]";
                    break;
                case "Battery Voltage":
                    dataListList = batVListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Battery Voltage [V]";
                    break;
                case "Pump Motor RPM":
                    dataListList = pumpMotorRPMListList;
                    zedGraphControl1.GraphPane.YAxis.Title.Text = "Pump Motor Speed [RPM]";
                    break;
                default:
                    throw new Exception("Unrecognized y axis type");
            }

            //build the graph curves
            for (int dataIndex = 0; dataIndex < dataListList.Count; dataIndex++)
            {
                var colorIndex = (int)Math.Round((((float)dataIndex) / (dataListList.Count - 1)) * (colorList.Count - 1), 0);
                var color = colorList[colorIndex];

                var dataList = dataListList[dataIndex];
                var curve = zedGraphControl1.GraphPane.AddCurve(
                    "Ozonesonde " + (dataIndex + 1),
                    dataList,
                    color);
                curve.Symbol.IsVisible = true;
                curve.Symbol.Type = ZedGraph.SymbolType.Circle;
                curve.Symbol.Fill = new ZedGraph.Fill(curve.Color);
                curve.Symbol.Size = 5;
                curve.Line.IsVisible = true;
                curve.Line.Width = 2;
            }



            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void xRangeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (o3mrListList != null)
            {
                SetXRange((int)xRangeNumericUpDown.Value, o3mrListList.Count);
                SetYAxisName(yAxisComboBox.Text);
            }
        }
        public void SetXRange(int xCapacity, int numOzonesondes)
        {
            o3mrListList = new List<ZedGraph.RollingPointPairList>();
            o3ppListList = new List<ZedGraph.RollingPointPairList>();
            cellIListList = new List<ZedGraph.RollingPointPairList>();
            pumpTempListList = new List<ZedGraph.RollingPointPairList>();
            pumpIListList = new List<ZedGraph.RollingPointPairList>();
            batVListList = new List<ZedGraph.RollingPointPairList>();
            pumpMotorRPMListList = new List<ZedGraph.RollingPointPairList>();
            //build the rollingpointpairlist collection, and add them to the graph
            for (int ozonesondeIndex = 0; ozonesondeIndex < numOzonesondes; ozonesondeIndex++)
            {
                o3mrListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                o3ppListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                cellIListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                pumpTempListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                pumpIListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                batVListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
                pumpMotorRPMListList.Add(new ZedGraph.RollingPointPairList(xCapacity));
            }

            //this is always done after this method is called
            //zedGraphControl1.AxisChange();
            //zedGraphControl1.Refresh();
        }

        //keep the form open but invisible if the user closes the window
        private void OzonePlot_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
