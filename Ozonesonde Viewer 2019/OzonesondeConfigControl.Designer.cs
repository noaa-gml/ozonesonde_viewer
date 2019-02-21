namespace Ozonesonde_Viewer_2019
{
    partial class OzonesondeConfigControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cellBackgroundTextBox = new System.Windows.Forms.TextBox();
            this.pumpFlowrateTextBox = new System.Windows.Forms.TextBox();
            this.rhFlowrateCorrTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pumpEffComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cellBackgroundTextBox
            // 
            this.cellBackgroundTextBox.Location = new System.Drawing.Point(9, 36);
            this.cellBackgroundTextBox.Name = "cellBackgroundTextBox";
            this.cellBackgroundTextBox.Size = new System.Drawing.Size(100, 20);
            this.cellBackgroundTextBox.TabIndex = 2;
            // 
            // pumpFlowrateTextBox
            // 
            this.pumpFlowrateTextBox.Location = new System.Drawing.Point(9, 79);
            this.pumpFlowrateTextBox.Name = "pumpFlowrateTextBox";
            this.pumpFlowrateTextBox.Size = new System.Drawing.Size(100, 20);
            this.pumpFlowrateTextBox.TabIndex = 3;
            // 
            // rhFlowrateCorrTextBox
            // 
            this.rhFlowrateCorrTextBox.Location = new System.Drawing.Point(9, 122);
            this.rhFlowrateCorrTextBox.Name = "rhFlowrateCorrTextBox";
            this.rhFlowrateCorrTextBox.Size = new System.Drawing.Size(100, 20);
            this.rhFlowrateCorrTextBox.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Cell Background [uA]:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 63);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(148, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Pump Flowrate [sec / 100 ml]:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 106);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "RH Flowrate Correction [%]:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.pumpEffComboBox);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.rhFlowrateCorrTextBox);
            this.groupBox1.Controls.Add(this.pumpFlowrateTextBox);
            this.groupBox1.Controls.Add(this.cellBackgroundTextBox);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(251, 196);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ozonesonde Configuration";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 149);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Pump Efficiency Correction:";
            // 
            // pumpEffComboBox
            // 
            this.pumpEffComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pumpEffComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pumpEffComboBox.FormattingEnabled = true;
            this.pumpEffComboBox.Location = new System.Drawing.Point(9, 165);
            this.pumpEffComboBox.Name = "pumpEffComboBox";
            this.pumpEffComboBox.Size = new System.Drawing.Size(236, 21);
            this.pumpEffComboBox.TabIndex = 10;
            // 
            // OzonesondeConfigControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "OzonesondeConfigControl";
            this.Size = new System.Drawing.Size(257, 202);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox cellBackgroundTextBox;
        private System.Windows.Forms.TextBox pumpFlowrateTextBox;
        private System.Windows.Forms.TextBox rhFlowrateCorrTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox pumpEffComboBox;
    }
}
