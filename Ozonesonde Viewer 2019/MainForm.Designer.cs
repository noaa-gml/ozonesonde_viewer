namespace Ozonesonde_Viewer_2019
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataRichTextBox = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cutterBatteryVoltageLabel = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.cutterHeaterLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cutterBoardTemperatureLabel = new System.Windows.Forms.Label();
            this.cutterPressureSensorTemperatureLabel = new System.Windows.Forms.Label();
            this.cutterPressureLabel = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.statusRichTextBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ozoneStatusPanel = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pressureForCalculationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showPlotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataRichTextBox
            // 
            this.dataRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dataRichTextBox.Location = new System.Drawing.Point(12, 27);
            this.dataRichTextBox.Name = "dataRichTextBox";
            this.dataRichTextBox.Size = new System.Drawing.Size(232, 342);
            this.dataRichTextBox.TabIndex = 0;
            this.dataRichTextBox.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.cutterBatteryVoltageLabel);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.cutterHeaterLabel);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cutterBoardTemperatureLabel);
            this.groupBox1.Controls.Add(this.cutterPressureSensorTemperatureLabel);
            this.groupBox1.Controls.Add(this.cutterPressureLabel);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(250, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(225, 87);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pressure Cutter Data";
            // 
            // cutterBatteryVoltageLabel
            // 
            this.cutterBatteryVoltageLabel.AutoSize = true;
            this.cutterBatteryVoltageLabel.Location = new System.Drawing.Point(155, 68);
            this.cutterBatteryVoltageLabel.Name = "cutterBatteryVoltageLabel";
            this.cutterBatteryVoltageLabel.Size = new System.Drawing.Size(24, 13);
            this.cutterBatteryVoltageLabel.TabIndex = 13;
            this.cutterBatteryVoltageLabel.Text = "n/a";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 68);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(98, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "Battery Voltage [V]:";
            // 
            // cutterHeaterLabel
            // 
            this.cutterHeaterLabel.AutoSize = true;
            this.cutterHeaterLabel.Location = new System.Drawing.Point(155, 55);
            this.cutterHeaterLabel.Name = "cutterHeaterLabel";
            this.cutterHeaterLabel.Size = new System.Drawing.Size(24, 13);
            this.cutterHeaterLabel.TabIndex = 11;
            this.cutterHeaterLabel.Text = "n/a";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 55);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Heater [PWM]:";
            // 
            // cutterBoardTemperatureLabel
            // 
            this.cutterBoardTemperatureLabel.AutoSize = true;
            this.cutterBoardTemperatureLabel.Location = new System.Drawing.Point(155, 42);
            this.cutterBoardTemperatureLabel.Name = "cutterBoardTemperatureLabel";
            this.cutterBoardTemperatureLabel.Size = new System.Drawing.Size(24, 13);
            this.cutterBoardTemperatureLabel.TabIndex = 9;
            this.cutterBoardTemperatureLabel.Text = "n/a";
            // 
            // cutterPressureSensorTemperatureLabel
            // 
            this.cutterPressureSensorTemperatureLabel.AutoSize = true;
            this.cutterPressureSensorTemperatureLabel.Location = new System.Drawing.Point(155, 29);
            this.cutterPressureSensorTemperatureLabel.Name = "cutterPressureSensorTemperatureLabel";
            this.cutterPressureSensorTemperatureLabel.Size = new System.Drawing.Size(24, 13);
            this.cutterPressureSensorTemperatureLabel.TabIndex = 8;
            this.cutterPressureSensorTemperatureLabel.Text = "n/a";
            // 
            // cutterPressureLabel
            // 
            this.cutterPressureLabel.AutoSize = true;
            this.cutterPressureLabel.Location = new System.Drawing.Point(155, 16);
            this.cutterPressureLabel.Name = "cutterPressureLabel";
            this.cutterPressureLabel.Size = new System.Drawing.Size(24, 13);
            this.cutterPressureLabel.TabIndex = 7;
            this.cutterPressureLabel.Text = "n/a";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 42);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(138, 13);
            this.label7.TabIndex = 6;
            this.label7.Text = "Board Temperature [deg C]:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 29);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(143, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Sensor Temperature [deg C]:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Pressure [mb]:";
            // 
            // statusRichTextBox
            // 
            this.statusRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusRichTextBox.Location = new System.Drawing.Point(250, 133);
            this.statusRichTextBox.Name = "statusRichTextBox";
            this.statusRichTextBox.Size = new System.Drawing.Size(225, 190);
            this.statusRichTextBox.TabIndex = 2;
            this.statusRichTextBox.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(247, 117);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Status:";
            // 
            // ozoneStatusPanel
            // 
            this.ozoneStatusPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ozoneStatusPanel.Location = new System.Drawing.Point(250, 329);
            this.ozoneStatusPanel.Name = "ozoneStatusPanel";
            this.ozoneStatusPanel.Size = new System.Drawing.Size(225, 40);
            this.ozoneStatusPanel.TabIndex = 4;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(487, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pressureForCalculationsToolStripMenuItem,
            this.showPlotToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // pressureForCalculationsToolStripMenuItem
            // 
            this.pressureForCalculationsToolStripMenuItem.Name = "pressureForCalculationsToolStripMenuItem";
            this.pressureForCalculationsToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.pressureForCalculationsToolStripMenuItem.Text = "&Pressure for Calculations";
            this.pressureForCalculationsToolStripMenuItem.Click += new System.EventHandler(this.pressureForCalculationsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // showPlotToolStripMenuItem
            // 
            this.showPlotToolStripMenuItem.Name = "showPlotToolStripMenuItem";
            this.showPlotToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.showPlotToolStripMenuItem.Text = "&Show Plot";
            this.showPlotToolStripMenuItem.Click += new System.EventHandler(this.showPlotToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 381);
            this.Controls.Add(this.ozoneStatusPanel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.statusRichTextBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dataRichTextBox);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ozonesonde Viewer 2019";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox dataRichTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label cutterBoardTemperatureLabel;
        private System.Windows.Forms.Label cutterPressureSensorTemperatureLabel;
        private System.Windows.Forms.Label cutterPressureLabel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox statusRichTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label cutterBatteryVoltageLabel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label cutterHeaterLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel ozoneStatusPanel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pressureForCalculationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showPlotToolStripMenuItem;
    }
}