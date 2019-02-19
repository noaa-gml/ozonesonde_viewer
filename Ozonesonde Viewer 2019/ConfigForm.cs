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
using Newtonsoft.Json;
using System.IO.Ports;
using System.Reflection;

namespace Ozonesonde_Viewer_2019
{
    public partial class ConfigForm : Form
    {
        public List<OzonesondeConfig> ResultingOzonesondeConfigList { get; private set; }

        private string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Ozonesonde Viewer");
        private string settingsFilename;

        public ConfigForm()
        {
            try
            {
                settingsFilename = Path.Combine(
                    settingsDir,
                    "OzonesondeViewerSettings.json");

                InitializeComponent();

                this.Text = "Ozonesonde Viewer 2019 Config " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

                ResultingOzonesondeConfigList = null;

                pictureBox1.Image = Image.FromFile("instrument selection graphic.png");

                //deserialize the ozone configuration settings
                List<OzonesondeConfig> ozoneConfigList = new List<OzonesondeConfig>();
                if (File.Exists(settingsFilename))
                {
                    StreamReader reader = new StreamReader(settingsFilename);
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.Length > 0)
                        {
                            ozoneConfigList.Add(JsonConvert.DeserializeObject<OzonesondeConfig>(line));
                        }
                    }
                    reader.Close();
                }
                else
                {
                    //default to a single ozonesonde at the first daisy chain index
                    ozoneConfigList.Add(new OzonesondeConfig(1, 0.01, 28, 3.1));
                }

                //build the tab pages based on the ozone configs
                for (int configIndex = 0; configIndex < ozoneConfigList.Count; configIndex++)
                {
                    uint dcIndex = 1;
                    if (configIndex > 0)
                    {
                        dcIndex = (uint)(tabControl1.TabPages.Count + 1);
                        tabControl1.TabPages.Add("Sonde " + dcIndex);
                    }

                    var oc = ozoneConfigList[configIndex];
                    if (oc.DCIndex != dcIndex) throw new Exception("dc index mismatch when restoring settings");

                    OzonesondeConfigControl occ = new OzonesondeConfigControl(oc);
                    tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(occ);
                    tabControl1.TabPages[tabControl1.TabPages.Count - 1].BackColor = Color.White;
                }

                //get the existing serial port names
                List<string> portNames = new List<string>(SerialPort.GetPortNames());
                portNames.Sort();
                portComboBox.Items.AddRange(portNames.ToArray());
                portComboBox.Text = Properties.Settings.Default.Port;

                OnResize(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void addNewOzonesondeButton_Click(object sender, EventArgs e)
        {
            uint dcIndex = (uint)(tabControl1.TabPages.Count + 1);
            tabControl1.TabPages.Add("Sonde " + dcIndex);

            OzonesondeConfig oc = new OzonesondeConfig(dcIndex, 0.01, 28, 3.1);
            OzonesondeConfigControl occ = new OzonesondeConfigControl(oc);
            tabControl1.TabPages[tabControl1.TabPages.Count-1].Controls.Add(occ);
            tabControl1.TabPages[tabControl1.TabPages.Count - 1].BackColor = Color.White;
            //occ.Location = new Point(tabControl1.TabPages[tabControl1.TabPages.Count - 1].Width / 2 - occ.Width / 2, occ.Location.Y);
            OnResize(EventArgs.Empty);
        }

        private void removeOzonesondeButton_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count > 1)
            {
                tabControl1.TabPages.RemoveAt(tabControl1.TabPages.Count - 1);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                var occ = (OzonesondeConfigControl)tabPage.Controls[0];
                occ.Location = new Point(tabControl1.TabPages[0].Width / 2 - occ.Width / 2, occ.Location.Y);
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                ResultingOzonesondeConfigList = (from TabPage t in tabControl1.TabPages select ((OzonesondeConfigControl)t.Controls[0]).GetOzonesondeConfig()).ToList();

                if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

                StreamWriter writer = new StreamWriter(new FileStream(settingsFilename, FileMode.Create, FileAccess.Write));
                foreach (var ozoneConfig in ResultingOzonesondeConfigList)
                {
                    string json = JsonConvert.SerializeObject(ozoneConfig);
                    writer.WriteLine(json);
                }
                writer.Close();

                string port = portComboBox.Text;
                if ((port == "") || (!port.StartsWith("COM"))) throw new Exception("Invalid port name");
                Properties.Settings.Default.Port = port;
                Properties.Settings.Default.Save();

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
