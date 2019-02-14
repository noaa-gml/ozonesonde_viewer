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
using Ozonesonde_Viewer_2019.ExtensionMethods;

namespace Ozonesonde_Viewer_2019
{
    public partial class MainForm : Form
    {
        private class OzoneConfigAndData
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
            public System.Windows.Forms.Timer StatusClearingTimer { get; set; }

            public OzoneConfigAndData()
            {
                CellCurrent = double.NaN;
                PumpTemperature = double.NaN;
                PumpCurrent = double.NaN;
                BatteryVoltage = double.NaN;
                OzonePartialPressure = double.NaN;
                OzoneMixingRatio = double.NaN;

                IsReadyForOutput = false;
            }

            public void CalculatePartialPressureAndMixingRatio(double pressure)
            {
                //calculate partial pressure
                //NOTE: no pump efficiency correction applied since this program expects ground-level ozonesondes
                double correctedFlowrate = /*effCorr **/
                        OzoneConfig.PumpFlowrate * (1 + OzoneConfig.RHFlowrateCorr / 100);
                OzonePartialPressure =
                    4.3085E-4 * (CellCurrent - OzoneConfig.CellBackground) * (PumpTemperature + 273.15) * correctedFlowrate;

                //calculate mixing ratio if we have a good pressure
                if (!double.IsNaN(pressure) && (pressure > 0) && (pressure < 1200))
                {
                    OzoneMixingRatio = OzonePartialPressure / pressure * 10 * 1000;//the last * 1000 converts to ppb
                }
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
        private readonly Nito.AsyncEx.AsyncLock outputFileWriterAsyncLock = new Nito.AsyncEx.AsyncLock();
        private StreamWriter outputFileWriter = null;

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

                ConfigForm config = new ConfigForm();
                if (config.ShowDialog() != DialogResult.OK)
                    this.Close();
                var ozonesondeConfigList = config.ResultingOzonesondeConfigList;
                ozonesondeConfigAndDataList = new List<OzoneConfigAndData>();
                foreach (var ozoneConfig in ozonesondeConfigList)
                {
                    OzoneConfigAndData ocad = new OzoneConfigAndData();
                    ocad.OzoneConfig = ozoneConfig;

                    ocad.StatusClearingTimer = new System.Windows.Forms.Timer();
                    ocad.StatusClearingTimer.Tick += StatusClearingTimerTick;
                    ocad.StatusClearingTimer.Interval = 200;

                    ozonesondeConfigAndDataList.Add(ocad);
                }

                //setup the output data file
                using (await outputFileWriterAsyncLock.LockAsync())
                {
                    DateTime utcNow = DateTime.UtcNow;
                    string outputDataFilename = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Ozonesonde Viewer",
                    string.Format("ozonesondeViewerData_{0:d4}{1:d2}{2:d2}.csv", utcNow.Year, utcNow.Month, utcNow.Day));
                    outputFileWriter = new StreamWriter(new FileStream(outputDataFilename, FileMode.Append, FileAccess.Write));

                    await outputFileWriter.WriteAsync("Cutter Pressure [mb], Cutter Pressure Sensor Temperature [deg C], Cutter Board Temperature [deg C], Cutter Heater [PWM], Cutter Battery Voltage [V]");
                    foreach (var ozoneConfig in ozonesondeConfigList)
                    {
                        await outputFileWriter.WriteAsync(
                            ", DC Index, Ozone Mixing Ratio [ppbv], Ozone Partial Pressure [mPa], Cell Current [uA], Pump Temperature [deg C], Pump Current [mA], Battery Voltage [V], Pump Speed [RPM]");
                    }
                    await outputFileWriter.WriteLineAsync();
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
                                if ((charValue == '\r') || (charValue == '\n'))
                                {
                                    if (lineBuilder.Length > 0)
                                    {
                                        if (processLineTask != null) await processLineTask;

                                        try
                                        {
                                            var line = lineBuilder.ToString();
                                            processLineTask = ProcessSerialLineAsync(line, cancellationToken);
                                        }
                                        catch (Exception ex)
                                        {
                                            ShowError("Error processing serial line: " + ex.ToString());
                                        }

                                        lineBuilder.Clear();
                                    }
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
        private const byte INSTRUMENT_OZONESONDE_X1 = 0x03;//the X1 board's packet uses a different ID and includes pump motor RPM
        private const byte INSTRUMENT_CUTTER = 0x11;
        private const int CELL_CURRENT_OFFSET = 10;
        private const int CELL_CURRENT_SIZE = 4;
        private const int PUMP_TEMPERATURE_OFFSET = 14;
        private const int PUMP_TEMPERATURE_SIZE = 4;
        private const int PUMP_CURRENT_OFFSET = 18;
        private const int PUMP_CURRENT_SIZE = 2;
        private const int BATTERY_VOLTAGE_OFFSET = 20;
        private const int BATTERY_VOLTAGE_SIZE = 2;
        private const int PUMP_MOTOR_RPM_OFFSET = 22;
        private const int PUMP_MOTOR_RPM_SIZE = 4;

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
        //private System.Diagnostics.Stopwatch timeSinceLastOutput = null;

        private async Task ProcessSerialLineAsync(string line, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
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
                if ((instrumentID == INSTRUMENT_OZONESONDE) || (instrumentID == INSTRUMENT_OZONESONDE_X1))
                {
                    DateTime utcNow = DateTime.UtcNow;

                    Task fileWriterTask = null;
                    //output data (to UI and file) when the first DC index ozonesonde packet is received (assuming we've received at least one packet beforehand)
                    if ((dcIndex == 1) && (ozonesondeConfigAndDataList[0].IsReadyForOutput))
                    {
                        //create a string representation of the ozone data to later show on the UI
                        List<string> firstOutputLineList = new List<string>();
                        List<string> outputList = new List<string>();
                        foreach (var ocad in ozonesondeConfigAndDataList)
                        {
                            //calculate the o3pp and o3mr here to keep things synched on output
                            if (ocad.IsReadyForOutput) ocad.CalculatePartialPressureAndMixingRatio(latestReceivedPressure);

                            StringBuilder outputBuilder = new StringBuilder();

                            firstOutputLineList.Add("Ozonesonde " + ocad.OzoneConfig.DCIndex + " (" + ocad.ADBoardType + "):");
                            outputBuilder.AppendLine(string.Format("Ozone Mixing Ratio [ppbv]:\t{0:0.000}", ocad.OzoneMixingRatio));
                            outputBuilder.AppendLine(string.Format("Ozone Partial Pressure [mPa]:\t{0:0.000}", ocad.OzonePartialPressure));
                            outputBuilder.AppendLine(string.Format("Cell Current [uA]:\t\t{0:0.000}", ocad.CellCurrent));
                            outputBuilder.AppendLine(string.Format("Pump Temperature [deg C]:\t{0:0.00}", ocad.PumpTemperature));
                            outputBuilder.AppendLine(string.Format("Pump Current [mA]:\t\t{0:0.}", ocad.PumpCurrent));
                            outputBuilder.AppendLine(string.Format("Battery Voltage [V]:\t\t{0:0.0}", ocad.BatteryVoltage));
                            outputBuilder.AppendLine(string.Format("Pump Speed [RPM]:\t{0:0.0}", ocad.PumpMotorRPM));
                            //outputBuilder.AppendLine(string.Format("Date/Time [UTC]: {0:d4}/{1:d2}/{2:d2} {3:d2}:{4:d2}:{5:d2}",
                            //    ocad.DateTimeStamp.Year, ocad.DateTimeStamp.Month, ocad.DateTimeStamp.Day, ocad.DateTimeStamp.Hour, ocad.DateTimeStamp.Minute, ocad.DateTimeStamp.Second));
                            outputList.Add(outputBuilder.ToString());
                        }
                        //update the UI thread safely by posting to the sync context
                        sc.Post(o =>
                        {
                            dataRichTextBox.Suspend();
                            dataRichTextBox.Clear();
                            for (int i = 0; i < firstOutputLineList.Count; i++)
                            {
                                dataRichTextBox.SelectionColor = Color.Green;
                                dataRichTextBox.AppendText(firstOutputLineList[i] + Environment.NewLine);
                                dataRichTextBox.AppendText(outputList[i] + Environment.NewLine);
                            }
                            dataRichTextBox.SelectionStart = 0;
                            dataRichTextBox.Resume();
                        }, null);

                        //start the file output
                        fileWriterTask = OutputDataFileRow();
                    }

                    string adBoardType = "Unknown";
                    double cellCurrent = double.NaN;
                    double pumpTemperature = double.NaN;
                    double pumpCurrent = double.NaN;
                    double batteryVoltage = double.NaN;
                    double pumpMotorRPM = double.NaN;
                    //parse the standard (V7 and older X1) ozonesonde packet
                    if (instrumentID == INSTRUMENT_OZONESONDE)
                    {
                        cellCurrent = (Int16)IntFromMSBHexString(line.Substring(CELL_CURRENT_OFFSET, CELL_CURRENT_SIZE));
                        cellCurrent /= 1000;

                        pumpTemperature = (Int16)IntFromMSBHexString(line.Substring(PUMP_TEMPERATURE_OFFSET, PUMP_TEMPERATURE_SIZE));
                        pumpTemperature /= 100;

                        pumpCurrent = ((double)IntFromMSBHexString(line.Substring(PUMP_CURRENT_OFFSET, PUMP_CURRENT_SIZE)));

                        batteryVoltage = ((double)IntFromMSBHexString(line.Substring(BATTERY_VOLTAGE_OFFSET, BATTERY_VOLTAGE_SIZE))) / 10.0;

                        adBoardType = "V7";
                    }
                    //parse the new X1 ozonesonde packet that includes pump motor RPM
                    else if (instrumentID == INSTRUMENT_OZONESONDE_X1)
                    {
                        cellCurrent = (Int16)IntFromMSBHexString(line.Substring(CELL_CURRENT_OFFSET, CELL_CURRENT_SIZE));
                        cellCurrent /= 1000;

                        pumpTemperature = (Int16)IntFromMSBHexString(line.Substring(PUMP_TEMPERATURE_OFFSET, PUMP_TEMPERATURE_SIZE));
                        pumpTemperature /= 100;

                        pumpCurrent = ((double)IntFromMSBHexString(line.Substring(PUMP_CURRENT_OFFSET, PUMP_CURRENT_SIZE)));

                        batteryVoltage = ((double)IntFromMSBHexString(line.Substring(BATTERY_VOLTAGE_OFFSET, BATTERY_VOLTAGE_SIZE))) / 10.0;

                        pumpMotorRPM = ((double)IntFromMSBHexString(line.Substring(PUMP_MOTOR_RPM_OFFSET, PUMP_MOTOR_RPM_SIZE))) / 10.0;

                        adBoardType = "X1";
                    }

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
                    ozoneConfigAndData.PumpMotorRPM = pumpMotorRPM;
                    ozoneConfigAndData.ADBoardType = adBoardType;

                    //ozoneConfigAndData.CalculatePartialPressureAndMixingRatio(latestReceivedPressure);//calculated on UI/file output later

                    ozoneConfigAndData.DateTimeStamp = utcNow;
                    ozoneConfigAndData.IsReadyForOutput = true;

                    ShowOzoneStatusLight(dcIndex);
                    if (fileWriterTask != null) await fileWriterTask;
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

                    ShowCutterStatusLight();
                }

            }
            );//end of Task.Run
        }

        private async Task OutputDataFileRow()
        {
            StringBuilder fileOutputBuilder = new StringBuilder();
            //output the cutter data to file
            fileOutputBuilder.Append(string.Format("{0:0.00}, {1:0.00}, {2:0.00}, {3:0.}, {4:0.0}",
                latestReceivedPressure,
                latestCutterPressureSensorTemperature,
                latestCutterBoardTemperature,
                latestCutterHeaterPWM,
                latestCutterBatteryVoltage
                ));
            //output data from each ozonesonde to file
            foreach (var ocad in ozonesondeConfigAndDataList)
            {
                OzoneConfigAndData ocadToUse = ocad;

                //if the packet is not ready for output, make a new empty one to output NaN values
                if (!ocad.IsReadyForOutput) ocadToUse = new OzoneConfigAndData();

                fileOutputBuilder.Append(string.Format(", {0:d}, {1:0.000}, {2:0.000}, {3:0.000}, {4:0.00}, {5:0.}, {6:0.0}, {7:0.0}",
                    ocadToUse.OzoneConfig.DCIndex,
                    ocadToUse.OzoneMixingRatio,
                    ocadToUse.OzonePartialPressure,
                    ocadToUse.CellCurrent,
                    ocadToUse.PumpTemperature,
                    ocadToUse.PumpCurrent,
                    ocadToUse.BatteryVoltage,
                    ocadToUse.PumpMotorRPM
                    ));

                //indicate that the packet has already been output to file and shouldn't be output again
                ocad.IsReadyForOutput = false;
            }

            using (await outputFileWriterAsyncLock.LockAsync())
            {
                await outputFileWriter.WriteLineAsync(fileOutputBuilder.ToString());
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

                //close the serial port
                using (await serialAsyncLock.LockAsync())
                {
                    if ((serialPort != null) && (serialPort.IsOpen)) serialPort.Close();
                }

                //close the output data writer
                using (await outputFileWriterAsyncLock.LockAsync())
                {
                    outputFileWriter?.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Queue<(System.Windows.Forms.Timer timer, uint dcIndex)> timerQueue = new Queue<(System.Windows.Forms.Timer, uint dcIndex)>();
        private void ShowOzoneStatusLight(uint dcIndex)
        {
            sc.Post(o =>
            {
                DrawStatusCircle(true, dcIndex, "O3_" + dcIndex, Color.Purple);

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Tick += StatusClearingTimerTick;
                timer.Interval = 200;
                timerQueue.Enqueue((timer, dcIndex));
                timer.Start();
            }, null);
        }

        private void StatusClearingTimerTick(object sender, EventArgs e)
        {
            if (timerQueue.Count <= 0) throw new Exception("Timer queue empty");
            System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)sender;
            var queueTimerAndIndex = timerQueue.Dequeue();
            if (queueTimerAndIndex.timer != timer) throw new Exception("Timer mismatch");
            timer.Stop();

            DrawStatusCircle(false, queueTimerAndIndex.dcIndex, "O3_" + queueTimerAndIndex.dcIndex, Color.Purple);
        }

        private System.Windows.Forms.Timer cutterStatusClearingTimer;
        private void ShowCutterStatusLight()
        {
            sc.Post(o =>
            {
                DrawStatusCircle(true, 0, "CP", Color.Blue);

                cutterStatusClearingTimer = new System.Windows.Forms.Timer();
                cutterStatusClearingTimer.Tick += CutterStatusClearingTimerTick;
                cutterStatusClearingTimer.Interval = 200;
                cutterStatusClearingTimer.Start();
            }, null);
        }

        private void CutterStatusClearingTimerTick(object sender, EventArgs e)
        {
            cutterStatusClearingTimer.Stop();
            DrawStatusCircle(false, 0, "CP", Color.Blue);
        }

        private void DrawStatusCircle(bool isFilled, uint circleIndex, string text, Color filledColor)
        {
            var g = ozoneStatusPanel.CreateGraphics();
            var circleDia = ozoneStatusPanel.Height - 15 - 1;
            int xPos = ((int)circleIndex) * (circleDia + 3);
            g.DrawString(text, font, new SolidBrush(Color.Black), xPos + ((4 - text.Length) * 2), 0);
            if (isFilled) g.FillEllipse(new SolidBrush(filledColor), xPos, 15, circleDia, circleDia);
            else g.FillEllipse(new SolidBrush(ozoneStatusPanel.BackColor), xPos, 15, circleDia, circleDia);

            //todo: invalidate?
        }

        private Font font = new Font(FontFamily.GenericSansSerif, 7.0f, FontStyle.Regular);
    }
}
