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
using System.Reflection;

namespace Ozonesonde_Viewer_2019
{
    public partial class MainForm : Form
    {
        //ozonesonde data and config list
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

        //font for the text above the blinking status lights
        private Font statusFont = new Font(FontFamily.GenericSansSerif, 7.0f, FontStyle.Regular);

        private OzonePlot ozonePlotForm;

        //constructor, mostly ignored in favor of the async load handler below
        public MainForm()
        {
            //setup the invariant culture
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeComponent();

            this.Text = "Ozonesonde Viewer 2019 " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        //do most of the form init here to keep things async
        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                //get the sync context for safe GUI updates from threads (tasks) later
                sc = SynchronizationContext.Current;

                //show the config form as a dialog, receiving ozonesonde metadata, setial port info, etc
                ConfigForm config = new ConfigForm();
                if ((config == null) || (config.IsDisposed)) this.Close();
                if (config.ShowDialog() != DialogResult.OK)
                    this.Close();
                var ozonesondeConfigList = config.ResultingOzonesondeConfigList;

                //rearrange the ozone config info into a list of OzoneConfigAndData that can also store data packets
                ozonesondeConfigAndDataList = new List<OzoneConfigAndData>();
                foreach (var ozoneConfig in ozonesondeConfigList)
                {
                    OzoneConfigAndData ocad = new OzoneConfigAndData
                    {
                        OzoneConfig = ozoneConfig
                    };
                    ozonesondeConfigAndDataList.Add(ocad);
                }

                //setup the output data file
                using (await outputFileWriterAsyncLock.LockAsync())
                {
                    //the filename has year/month/day and is written to "<AppData>\Ozonesonde Viewer"
                    DateTime utcNow = DateTime.UtcNow;

                    //loop until we get a filename that doesn't already exist (to allow multiple instances of this program to run without writing to the same file simultaneously)
                    int count = 1;
                    string outputDataFilename = "";
                    do
                    {
                        outputDataFilename = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Ozonesonde Viewer",
                            string.Format("ozonesondeViewerData_{0:d4}{1:d2}{2:d2}_{3:d2}{4:d2}{5:d2}_{6:d}.csv", utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second, count));
                        count++;
                    } while (File.Exists(outputDataFilename));
                    //open the file for writing (will append instead of overwrite)
                    outputFileWriter = new StreamWriter(new FileStream(outputDataFilename, FileMode.Append, FileAccess.Write));//append isn't necessary here thanks to the existence check above, but I'm including it for safety

                    //write out the header information for the cutter and each ozonesonde
                    await outputFileWriter.WriteAsync("Date/Time [UTC], Cutter Pressure [mb], Cutter Pressure Sensor Temperature [deg C], Cutter Board Temperature [deg C], Cutter Heater [PWM], Cutter Battery Voltage [V]");
                    foreach (var ozoneConfig in ozonesondeConfigList);
                    {
                        await outputFileWriter.WriteAsync(
                            ", DC Index, Ozone Mixing Ratio [ppbv], Ozone Partial Pressure [mPa], Cell Current [uA], Pump Temperature [deg C], Pump Current [mA], Battery Voltage [V], Pump Speed [RPM]");
                    }
                    await outputFileWriter.WriteLineAsync();
                }

                //a cancellation token for the serial port processing task, used to exit gracefully
                serialCancellationTokenSource = new CancellationTokenSource();

                //connect to the serial port
                await SerialConnectAsync(Properties.Settings.Default.Port, serialCancellationTokenSource.Token);
                ShowStatus("Opened serial port " + Properties.Settings.Default.Port);

                //initialize the plot window
                ozonePlotForm = new OzonePlot(ozonesondeConfigAndDataList);
                ozonePlotForm.Show();

                //start the seiral port processing background task
                processSerialTask = ProcessSerialPortAsync(serialCancellationTokenSource.Token);
                ShowStatus("Started serial processing task");
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed)
                {
                    ShowError(ex.Message);
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.Close();
            }
        }

        private async Task SerialConnectAsync(string port, CancellationToken cancellationToken)
        {
            //await Task.Run(async () =>//not really necessary, this is not very CPU bound
            //{
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
            //});
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

                    //scroll to the end
                    statusRichTextBox.SelectionStart = statusRichTextBox.Text.Length;
                    statusRichTextBox.ScrollToCaret();
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
                    
                    //scroll to the end
                    statusRichTextBox.SelectionStart = statusRichTextBox.Text.Length;
                    statusRichTextBox.ScrollToCaret();
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
                                        try
                                        {
                                            var line = lineBuilder.ToString();
                                            await ProcessSerialLineAsync(line, cancellationToken);
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
        private const int OZONE_SIZE = 11*2;
        private const int OZONE_X1_SIZE = 13 * 2;//15*2;
        private const int CUTTER_PACKET_SIZE = 32;
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


        private DateTime dateTimeOfLatestPressure = DateTime.MinValue;
        private double latestReceivedPressure = double.NaN;
        private double latestCutterPressureSensorTemperature = double.NaN;
        private double latestCutterBoardTemperature = double.NaN;
        private double latestCutterHeaterPWM = double.NaN;
        private double latestCutterBatteryVoltage = double.NaN;
        private int cutterDCIndex = -1;

        private bool isFirstLine = true;

        private async Task ProcessSerialLineAsync(string line, CancellationToken cancellationToken)
        {
            try
            {
                //if the line length is not recognized, just return
                if ((line.Length != OZONE_SIZE) && (line.Length != OZONE_X1_SIZE) && (line.Length != CUTTER_PACKET_SIZE))
                {
                    //throw new SerialLineFormatException("Invalid packet line length, skipping it");
                    return;
                }

                //check for unrecognized serial data lines (ignoring the first line, as it's usually garbage)
                if (!isFirstLine)
                {
                    if (!line.StartsWith("xdata=")) throw new SerialLineFormatException("Serial line not in xdata format");
                    if (line.Length <= 12) throw new SerialLineFormatException("XDATA line too short: " + line);
                }
                isFirstLine = false;

                byte instrumentID = Byte.Parse(line.Substring(INSTRUMENT_ID_OFFSET, INSTRUMENT_ID_SIZE), System.Globalization.NumberStyles.HexNumber);
                byte dcIndex = Byte.Parse(line.Substring(DC_INDEX_OFFSET, DC_INDEX_SIZE), System.Globalization.NumberStyles.HexNumber);
                if (dcIndex < 1) throw new Exception("Invalid daisy chain index");
                if ((instrumentID == INSTRUMENT_OZONESONDE) || (instrumentID == INSTRUMENT_OZONESONDE_X1))
                {
                    //line length check
                    if ((line.Length != OZONE_SIZE) && (line.Length != OZONE_X1_SIZE))
                        throw new SerialLineFormatException("Invalid ozonesonde packet line length, skipping it");

                    //make sure that the pressure cutter is at the end of the daisy chain, otherwise it shifts the ozonesonde dc indices
                    if ((cutterDCIndex > 0) && (dcIndex > cutterDCIndex)) throw new SerialLineFormatException("The pressure cutter needs to be at the end of the chain");

                    //output data (to UI and file) when the first DC index ozonesonde packet is received (assuming we've received at least one packet beforehand)
                    if ((dcIndex == 1) && (ozonesondeConfigAndDataList[0].IsReadyForOutput))
                    {
                        DateTime utcNow = DateTime.UtcNow;

                        //create a string representation of the ozone data to later show on the UI
                        List<string> firstOutputLineList = new List<string>();
                        List<string> outputList = new List<string>();
                        foreach (var ocad in ozonesondeConfigAndDataList)
                        {
                            //set all the timestamps at output so it's the same for each ozonesonde
                            ocad.DateTimeStamp = utcNow;

                            //calculate the o3pp and o3mr here to keep things synched on output
                            if (ocad.IsReadyForOutput)
                            {
                                //check the "age" of the latest received pressure
                                if (!double.IsNaN(latestReceivedPressure))
                                {
                                    var latestPressureAgeMinutes = (DateTime.UtcNow - dateTimeOfLatestPressure).TotalMinutes;
                                    if (latestPressureAgeMinutes > 30)
                                    {
                                        ShowStatus("The manually-entered pressure is " + string.Format("{0:0.0}", latestPressureAgeMinutes) + " minutes old and should be re-entered.");
                                    }
                                }
                                ocad.CalculatePartialPressureAndMixingRatio(latestReceivedPressure);
                            }

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
                            if (!dataRichTextBox.IsDisposed)
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
                            }
                        }, null);

                        //start the file output
                        //if (fileWriterTask != null) await fileWriterTask;
                        //fileWriterTask = OutputDataFileRow();
                        await OutputDataFileAndPlotAsync(utcNow);
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
                        if (line.Length < (BATTERY_VOLTAGE_OFFSET + BATTERY_VOLTAGE_SIZE)) throw new SerialLineFormatException("Ozonesonde (V7) serial data line too short: " + line);

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
                        if (line.Length < (PUMP_MOTOR_RPM_OFFSET + PUMP_MOTOR_RPM_SIZE)) throw new SerialLineFormatException("Ozonesonde (X1) serial data line too short: " + line);

                        cellCurrent = (Int16)IntFromMSBHexString(line.Substring(CELL_CURRENT_OFFSET, CELL_CURRENT_SIZE));
                        cellCurrent /= 1000;

                        pumpTemperature = (Int16)IntFromMSBHexString(line.Substring(PUMP_TEMPERATURE_OFFSET, PUMP_TEMPERATURE_SIZE));
                        pumpTemperature /= 100;

                        pumpCurrent = ((double)IntFromMSBHexString(line.Substring(PUMP_CURRENT_OFFSET, PUMP_CURRENT_SIZE)));

                        batteryVoltage = ((double)IntFromMSBHexString(line.Substring(BATTERY_VOLTAGE_OFFSET, BATTERY_VOLTAGE_SIZE))) / 10.0;

                        pumpMotorRPM = ((double)IntFromMSBHexString(line.Substring(PUMP_MOTOR_RPM_OFFSET, PUMP_MOTOR_RPM_SIZE))) / 10.0;

                        adBoardType = "X1";
                    }
                    //if we still don't have good ozonesonde data, just return
                    if (double.IsNaN(cellCurrent))
                    {
                        ShowError("Ozone packet received, but parsing failed");
                        return;
                    }

                    //select the dataset to update based on the daisy chain index
                    if (dcIndex > ozonesondeConfigAndDataList.Count)
                    {
                        //don't throw an exception here (since this message is easier to read without the call heirarchy)
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

                    //ozoneConfigAndData.DateTimeStamp = utcNow;//note: better to do this on output so the times are synched
                    ozoneConfigAndData.IsReadyForOutput = true;

                    HelperMethods.RunAsync(ShowOzoneStatusLightAsync(dcIndex), ShowError);
                }
                else if (instrumentID == INSTRUMENT_CUTTER)
                {
                    if (line.Length < (CUTTER_BATTERY_OFFSET + CUTTER_BATTERY_SIZE)) throw new SerialLineFormatException("Cutter serial data line too short: " + line);

                    //xdata=110100013BEA0A41014B00001F
                    string cutterPressOffsetStr = line.Substring(CUTTER_PRESSURE_OFFSET, CUTTER_PRESSURE_SIZE);
                    latestReceivedPressure = (Int32)IntFromMSBHexString(cutterPressOffsetStr);
                    latestReceivedPressure /= 100;//mb
                    dateTimeOfLatestPressure = DateTime.UtcNow;
                    latestCutterPressureSensorTemperature = (Int16)IntFromMSBHexString(line.Substring(CUTTER_PTEMP_OFFSET, CUTTER_PTEMP_SIZE));
                    latestCutterPressureSensorTemperature /= 100;//deg C
                    int btempADC = (Int16)IntFromMSBHexString(line.Substring(CUTTER_BTEMP_OFFSET, CUTTER_BTEMP_SIZE));
                    latestCutterHeaterPWM = (UInt16)IntFromMSBHexString(line.Substring(CUTTER_HEATER_OFFSET, CUTTER_HEATER_SIZE));
                    latestCutterBatteryVoltage = (byte)IntFromMSBHexString(line.Substring(CUTTER_BATTERY_OFFSET, CUTTER_BATTERY_SIZE));
                    latestCutterBatteryVoltage /= 10;
                    cutterDCIndex = dcIndex;

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

                    HelperMethods.RunAsync(ShowCutterStatusLightAsync(), ShowError);
                }
            }
            catch (SerialLineFormatException serialLineException)
            {
                ShowError("Serial line format error: " + serialLineException.ToString());
            }
            //todo: other exception types?
        }

        private async Task OutputDataFileAndPlotAsync(DateTime dateTimeToUseUTC)
        {
            string dateTimeStrUTC = string.Format("{0:d4}/{1:d2}/{2:d2} {3:d2}:{4:d2}:{5:d2}",
                dateTimeToUseUTC.Year,
                dateTimeToUseUTC.Month,
                dateTimeToUseUTC.Day,
                dateTimeToUseUTC.Hour,
                dateTimeToUseUTC.Minute,
                dateTimeToUseUTC.Second
                );
            StringBuilder fileOutputBuilder = new StringBuilder();
            //output the cutter data to file
            fileOutputBuilder.Append(string.Format("{0}, {1:0.00}, {2:0.00}, {3:0.00}, {4:0.}, {5:0.0}",
                dateTimeStrUTC,
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
                    (ocadToUse.OzoneConfig != null) ? (int)ocadToUse.OzoneConfig.DCIndex : -999,
                    ocadToUse.OzoneMixingRatio,
                    ocadToUse.OzonePartialPressure,
                    ocadToUse.CellCurrent,
                    ocadToUse.PumpTemperature,
                    ocadToUse.PumpCurrent,
                    ocadToUse.BatteryVoltage,
                    ocadToUse.PumpMotorRPM
                    ));

                //update the plot data
                ozonePlotForm.AddOzoneDataPoint(ocadToUse);

                //indicate that the packet has already been output to file and shouldn't be output again
                ocad.IsReadyForOutput = false;
            }

            using (await outputFileWriterAsyncLock.LockAsync())
            {
                //outputFileWriter.WriteLine(fileOutputBuilder.ToString());
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

        private async Task ShowOzoneStatusLightAsync(uint dcIndex)
        {
            sc.Post(o =>
            {
                DrawStatusCircle(true, dcIndex, "O3_" + dcIndex, Color.Purple);
            }, null);

            await Task.Delay(200);

            sc.Post(o =>
            {
                DrawStatusCircle(false, dcIndex, "O3_" + dcIndex, Color.Purple);
            }, null);
        }

        private async Task ShowCutterStatusLightAsync()
        {
            sc.Post(o =>
            {
                DrawStatusCircle(true, 0, "CP", Color.Blue);
            }, null);

            await Task.Delay(200);

            sc.Post(o =>
            {
                DrawStatusCircle(false, 0, "CP", Color.Blue);
            }, null);
        }

        private void DrawStatusCircle(bool isFilled, uint circleIndex, string text, Color filledColor)
        {
            if (ozoneStatusPanel.IsDisposed) return;

            var g = ozoneStatusPanel.CreateGraphics();
            var circleDia = ozoneStatusPanel.Height - 15 - 1;
            int xPos = ((int)circleIndex) * (circleDia + 3);
            g.DrawString(text, statusFont, new SolidBrush(Color.Black), xPos + ((4 - text.Length) * 2), 0);
            if (isFilled) g.FillEllipse(new SolidBrush(filledColor), xPos, 15, circleDia, circleDia);
            else g.FillEllipse(new SolidBrush(ozoneStatusPanel.BackColor), xPos, 15, circleDia, circleDia);
            //todo: invalidate?  doesn't seem to be necessary
        }

        private void pressureForCalculationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PressureForCalculationsForm form = new PressureForCalculationsForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                dateTimeOfLatestPressure = DateTime.UtcNow;
                latestReceivedPressure = form.Pressure;
                cutterPressureLabel.Text = string.Format("{0:0.00}", latestReceivedPressure);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder textBuilder = new StringBuilder();
            textBuilder.AppendLine("Ozonesonde Viewer 2019");
            textBuilder.AppendLine("Allen Jordan, NOAA GMD/OZWV");
            textBuilder.AppendLine("allen.jordan@noaa.gov");
            textBuilder.Append("https://www.esrl.noaa.gov/gmd/ozwv/");
            MessageBox.Show(this, textBuilder.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void showPlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ozonePlotForm.Show();
        }
    }
}
