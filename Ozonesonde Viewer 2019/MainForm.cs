using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace Ozonesonde_Viewer_2019
{
    public partial class MainForm : Form
    {
        private class OzoneConfigAndData
        {
            public OzonesondeConfig OzoneConfig { get; set; }

            //measurements
            public double CellCurrent { get; set; }
            public double PumpTemperature { get; set; }
            public double PumpCurrent { get; set; }
            public double BatteryVoltage { get; set; }

            //calculations
            public double OzonePartialPressure { get; set; }
            public double OzoneMixingRatio { get; set; }

            public OzoneConfigAndData()
            {
                CellCurrent = double.NaN;
                PumpTemperature = double.NaN;
                PumpCurrent = double.NaN;
                BatteryVoltage = double.NaN;
                OzonePartialPressure = double.NaN;
                OzoneMixingRatio = double.NaN;
            }
        }

        //ozonesonde and port information received from the config dialog
        private List<OzoneConfigAndData> ozonesondeConfigAndDataList;
        private SerialPort serialPort = null;

        //synchronization and task storage for the serial port
        private static CancellationTokenSource serialCancellationTokenSource;
        private readonly Nito.AsyncEx.AsyncLock serialAsyncLock = new Nito.AsyncEx.AsyncLock();
        private SynchronizationContext sc;
        private Task processSerialTask;

        //synchronization and writer class for the log file
        private readonly Nito.AsyncEx.AsyncLock logWriterAsyncLock = new Nito.AsyncEx.AsyncLock();
        private StreamWriter logWriter = null;

        public MainForm()
        {
            //setup the invariant culture
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                sc = SynchronizationContext.Current;

                this.DoubleBuffered = true;

                ConfigForm config = new ConfigForm();
                if (config.ShowDialog() != DialogResult.OK)
                    this.Close();
                var ozonesondeConfigList = config.ResultingOzonesondeConfigList;
                ozonesondeConfigAndDataList = new List<OzoneConfigAndData>();
                foreach (var ozoneConfig in ozonesondeConfigList)
                {
                    OzoneConfigAndData ocad = new OzoneConfigAndData();
                    ocad.OzoneConfig = ozoneConfig;
                    ozonesondeConfigAndDataList.Add(ocad);
                }

                //a cancellation token for the serial port processing task, used to exit gracefully
                serialCancellationTokenSource = new CancellationTokenSource();

                await SerialConnectAsync(Properties.Settings.Default.Port, serialCancellationTokenSource.Token);
                ShowStatus("Opened serial port " + Properties.Settings.Default.Port);

                //start the seiral port processing background task
                processSerialTask = ProcessSerialPortAsync(serialCancellationTokenSource.Token);
                ShowStatus("Started serial processing task");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private async Task SerialConnectAsync(string port, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                using (await serialAsyncLock.LockAsync(cancellationToken))
                {
                    if (serialPort == null)
                    {
                        serialPort = new SerialPort(Properties.Settings.Default.Port, 9600, Parity.None, 8, StopBits.One);
                        serialPort.Open();
                    }
                    else
                    {
                        if (serialPort.IsOpen) serialPort.Close();
                        serialPort.PortName = port;
                        serialPort.Open();
                    }
                }
            });
        }

        private void ShowError(string message)
        {
            sc.Post(o =>
            {
                if (!statusRichTextBox.IsDisposed)
                {
                    //if we've gone over some threshold number of lines, remove the older lines
                    int linesToCheck = 1000;
                    if (statusRichTextBox.Lines.Length > linesToCheck)
                    {
                        //leave enough lines on the screen to match the height of the control * 2 (to not make the vert scroll bar jump too much)
                        int linesToRemove = statusRichTextBox.Lines.Length - (statusRichTextBox.Height / statusRichTextBox.Font.Height) * 2;
                        statusRichTextBox.Lines = statusRichTextBox.Lines.Skip(linesToRemove).ToArray();
                    }
                    statusRichTextBox.SelectionColor = Color.Red;
                    statusRichTextBox.AppendText(((string)o).Trim() + Environment.NewLine);
                }
            }, message);
        }

        private void ShowStatus(string message)
        {
            sc.Post(o =>
            {
                if (!statusRichTextBox.IsDisposed)
                {
                    //if we've gone over some threshold number of lines, remove the older lines
                    int linesToCheck = 1000;
                    if (statusRichTextBox.Lines.Length > linesToCheck)
                    {
                        //leave enough lines on the screen to match the height of the control * 2 (to not make the vert scroll bar jump too much)
                        int linesToRemove = statusRichTextBox.Lines.Length - (statusRichTextBox.Height / statusRichTextBox.Font.Height) * 2;
                        statusRichTextBox.Lines = statusRichTextBox.Lines.Skip(linesToRemove).ToArray();
                    }
                    statusRichTextBox.SelectionColor = Color.Black;
                    statusRichTextBox.AppendText(((string)o).Trim() + Environment.NewLine);
                }
            }, message);
        }



        private async Task ProcessSerialPortAsync(CancellationToken cancellationToken)
        {
            //note: using this to run the serial processing loop on a thread really improves UI responsiveness compared to just doing await on the IO operations
            await Task.Run(async () =>
            {
                StringBuilder lineBuilder = new StringBuilder();

                //keep looping until outside cancellation is requested
                while (!cancellationToken.IsCancellationRequested)
                {
                    //get the serial port lock
                    using (await serialAsyncLock.LockAsync(cancellationToken))
                    {
                        if (!serialPort.IsOpen) throw new Exception("Serial port no longer open");

                        //Task writeLogTask = null;
                        Task processLineTask = null;
                        while ((serialPort.BytesToRead > 0) && (!cancellationToken.IsCancellationRequested))
                        {
                            int bytesToRead = serialPort.BytesToRead;
                            byte[] buffer = new byte[bytesToRead];
                            await serialPort.BaseStream.ReadAsync(buffer, 0, bytesToRead, cancellationToken);
                            char[] charBuffer = Array.ConvertAll(buffer, a => (char)a);

                            //process incoming characters into separate lines
                            foreach (var charValue in charBuffer)
                            {
                                if (((charValue == '\r') || (charValue == '\n')) && (lineBuilder.Length > 0))
                                {
                                    if (processLineTask != null) await processLineTask;

                                    try
                                    {
                                        processLineTask = ProcessSerialLineTask(lineBuilder.ToString(), cancellationToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        ShowError("Error processing serial line: " + ex.ToString());
                                    }

                                    lineBuilder.Clear();
                                }
                                else
                                {
                                    lineBuilder.Append(charValue);
                                    if (lineBuilder.Length > 100)
                                    {
                                        ShowError("No serial line ending found, continuing to search");
                                        lineBuilder.Clear();
                                    }
                                }
                            }
                        }
                    }//end of using serial lock
                }//end of while !cancelled loop
            });//end of await Task.Run

            ShowStatus("Closing serial processing task");
        }

        //offsets and sizes are for ascii hexadecimal characters (2 characters per byte)
        private const int INSTRUMENT_ID_OFFSET = 6;
        private const int INSTRUMENT_ID_SIZE = 2;
        private const int DC_INDEX_OFFSET = 8;
        private const int DC_INDEX_SIZE = 2;
        private const byte INSTRUMENT_OZONESONDE = 0x01;
        private const byte INSTRUMENT_CUTTER = 0x11;
        private const int CELL_CURRENT_OFFSET = 10;
        private const int CELL_CURRENT_SIZE = 4;
        private const int PUMP_TEMPERATURE_OFFSET = 14;
        private const int PUMP_TEMPERATURE_SIZE = 4;
        private const int PUMP_CURRENT_OFFSET = 18;
        private const int PUMP_CURRENT_SIZE = 2;
        private const int BATTERY_VOLTAGE_OFFSET = 20;
        private const int BATTERY_VOLTAGE_SIZE = 2;

        private const int CUTTER_PRESSURE_OFFSET = 10;
        private const int CUTTER_PRESSURE_SIZE = 8;
        private const int CUTTER_PTEMP_OFFSET = 18;
        private const int CUTTER_PTEMP_SIZE = 4;
        private const int CUTTER_BTEMP_OFFSET = 22;
        private const int CUTTER_BTEMP_SIZE = 4;
        private const int CUTTER_HEATER_OFFSET = 26;
        private const int CUTTER_HEATER_SIZE = 4;
        private const int CUTTER_BATTERY_OFFSET = 30;
        private const int CUTTER_BATTERY_SIZE = 2;


        private double latestReceivedPressure = double.NaN;
        private double latestCutterPressureSensorTemperature = double.NaN;
        private double latestCutterBoardTemperature = double.NaN;
        private double latestCutterHeaterPWM = double.NaN;
        private double latestCutterBatteryVoltage = double.NaN;

        private bool isFirstLine = true;

        private async Task ProcessSerialLineTask(string line, CancellationToken cancellationToken)
        {
            if (!line.StartsWith("xdata=") && (!isFirstLine))
            {
                ShowError("Serial line not in xdata format");
                return;
            }
            isFirstLine = false;

            byte instrumentID = Byte.Parse(line.Substring(INSTRUMENT_ID_OFFSET, INSTRUMENT_ID_SIZE), System.Globalization.NumberStyles.HexNumber);
            byte dcIndex = Byte.Parse(line.Substring(DC_INDEX_OFFSET, DC_INDEX_SIZE), System.Globalization.NumberStyles.HexNumber);
            if (dcIndex < 1) throw new Exception("Invalid daisy chain index");
            if (instrumentID == INSTRUMENT_OZONESONDE)
            {
                //parse raw ozonesonde fields
                double cellCurrent = (Int16)IntFromMSBHexString(line.Substring(CELL_CURRENT_OFFSET, CELL_CURRENT_SIZE));
                cellCurrent /= 1000;
                double pumpTemperature = (Int16)IntFromMSBHexString(line.Substring(PUMP_TEMPERATURE_OFFSET, PUMP_TEMPERATURE_SIZE));
                pumpTemperature /= 100;
                double pumpCurrent = ((double)IntFromMSBHexString(line.Substring(PUMP_CURRENT_OFFSET, PUMP_CURRENT_SIZE)));
                double batteryVoltage = ((double)IntFromMSBHexString(line.Substring(BATTERY_VOLTAGE_OFFSET, BATTERY_VOLTAGE_SIZE))) / 10.0;

                //select the dataset to update based on the daisy chain index
                if (dcIndex > ozonesondeConfigAndDataList.Count)
                {
                    ShowError("Ozonesonde packet received for unconfigured dc index " + dcIndex + ".  Restart program and reconfigure.");
                    return;
                }
                var ozoneConfigAndData = ozonesondeConfigAndDataList[dcIndex - 1];
                if (ozoneConfigAndData.OzoneConfig.DCIndex != dcIndex) throw new Exception("dc index mismatch");
                ozoneConfigAndData.CellCurrent = cellCurrent;
                ozoneConfigAndData.PumpTemperature = pumpTemperature;
                ozoneConfigAndData.PumpCurrent = pumpCurrent;
                ozoneConfigAndData.BatteryVoltage = batteryVoltage;

                //calculate partial pressure
                //NOTE: no pump efficiency correction applied since this program expects ground-level ozonesondes
                double correctedFlowrate = /*effCorr **/ ozoneConfigAndData.OzoneConfig.PumpFlowrate * (1 + ozoneConfigAndData.OzoneConfig.RHFlowrateCorr / 100);
                ozoneConfigAndData.OzonePartialPressure =
                    4.3085E-4 * (cellCurrent - ozoneConfigAndData.OzoneConfig.CellBackground) * (pumpTemperature + 273.15) * correctedFlowrate;

                //calculate mixing ratio if we have a good pressure
                if (!double.IsNaN(latestReceivedPressure) && (latestReceivedPressure > 0) && (latestReceivedPressure < 1200))
                {
                    ozoneConfigAndData.OzoneMixingRatio = ozoneConfigAndData.OzonePartialPressure / latestReceivedPressure * 10.0;
                }


                //create a string representation of the ozone data to later show on the UI
                List<string> firstOutputLineList = new List<string>();
                List<string> outputList = new List<string>();
                foreach (var ocad in ozonesondeConfigAndDataList)
                {
                    StringBuilder outputBuilder = new StringBuilder();

                    firstOutputLineList.Add("Ozonesonde " + ocad.OzoneConfig.DCIndex + ":");
                    outputBuilder.AppendLine(string.Format("Ozone Mixing Ratio [ppbv]:\t{0:0.000}", ocad.OzoneMixingRatio));
                    outputBuilder.AppendLine(string.Format("Ozone Partial Pressure [mPa]:\t{0:0.000}", ocad.OzonePartialPressure));
                    outputBuilder.AppendLine(string.Format("Cell Current [uA]:\t\t{0:0.000}", ocad.CellCurrent));
                    outputBuilder.AppendLine(string.Format("Pump Temperature [deg C]:\t{0:0.00}", ocad.PumpTemperature));
                    outputBuilder.AppendLine(string.Format("Pump Current [mA]:\t\t{0:0.}", ocad.PumpCurrent));
                    outputBuilder.AppendLine(string.Format("Battery Voltage [V]:\t\t{0:0.0}", ocad.BatteryVoltage));
                    outputList.Add(outputBuilder.ToString());
                }
                //update the UI thread safely by posting to the sync context
                sc.Post(o =>
                {
                    dataRichTextBox.Clear();
                    for (int i = 0; i < firstOutputLineList.Count; i++)
                    {
                        dataRichTextBox.SelectionColor = Color.Green;
                        dataRichTextBox.AppendText(firstOutputLineList[i] + Environment.NewLine);
                        dataRichTextBox.AppendText(outputList[i] + Environment.NewLine);
                    }
                    dataRichTextBox.SelectionStart = 0;
                }, null);

            }
            else if (instrumentID == INSTRUMENT_CUTTER)
            {
                //xdata=110100013BEA0A41014B00001F
                string cutterPressOffsetStr = line.Substring(CUTTER_PRESSURE_OFFSET, CUTTER_PRESSURE_SIZE);
                latestReceivedPressure = (Int32)IntFromMSBHexString(cutterPressOffsetStr);
                latestReceivedPressure /= 100;//mb
                latestCutterPressureSensorTemperature = (Int16)IntFromMSBHexString(line.Substring(CUTTER_PTEMP_OFFSET, CUTTER_PTEMP_SIZE));
                latestCutterPressureSensorTemperature /= 100;//deg C
                int btempADC = (Int16)IntFromMSBHexString(line.Substring(CUTTER_BTEMP_OFFSET, CUTTER_BTEMP_SIZE));
                latestCutterHeaterPWM = (UInt16)IntFromMSBHexString(line.Substring(CUTTER_HEATER_OFFSET, CUTTER_HEATER_SIZE));
                latestCutterBatteryVoltage = (byte)IntFromMSBHexString(line.Substring(CUTTER_BATTERY_OFFSET, CUTTER_BATTERY_SIZE));
                latestCutterBatteryVoltage /= 10;

                double therm_res = (20000.0 * btempADC) / (1023.0 - btempADC);
                latestCutterBoardTemperature = 1.0 / (.0007 + .00028 * Math.Log(therm_res) + (9.93007E-8) * (Math.Log(therm_res) * Math.Log(therm_res) * Math.Log(therm_res))) - 273.16;//deg C, approx

                sc.Post(o =>
                {
                    cutterPressureLabel.Text = string.Format("{0:0.00}", latestReceivedPressure);
                    cutterPressureSensorTemperatureLabel.Text = string.Format("{0:0.00}", latestCutterPressureSensorTemperature);
                    cutterBoardTemperatureLabel.Text = string.Format("{0:0.00}", latestCutterBoardTemperature);
                    cutterHeaterLabel.Text = string.Format("{0:0.}", latestCutterHeaterPWM);
                    cutterBatteryVoltageLabel.Text = string.Format("{0:0.0}", latestCutterBatteryVoltage);
                }, null);
            }

            

            using (await logWriterAsyncLock.LockAsync(cancellationToken))
            {
                await logWriter.WriteLineAsync(line);
            }
        }

        /**
         * Helper method to parse an integer from a MSB-first ascii hex string.  
         * @param str       The MSB-first ascii hex string needing to be parsed into an integer.  
         * @result          The resulting integer value from the parser.  
         */
        protected int IntFromMSBHexString(string str)
        {
            return Int32.Parse(str, System.Globalization.NumberStyles.HexNumber);
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //cancel the serial port processing task and wait on its completion
                serialCancellationTokenSource?.Cancel();
                if (processSerialTask != null) await processSerialTask;

                using (await serialAsyncLock.LockAsync())
                {
                    if ((serialPort != null) && (serialPort.IsOpen)) serialPort.Close();
                }

                using (await logWriterAsyncLock.LockAsync())
                {
                    logWriter?.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
