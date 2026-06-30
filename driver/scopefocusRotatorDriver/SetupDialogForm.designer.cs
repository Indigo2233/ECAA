namespace ASCOM.scopefocus
{
    partial class SetupDialogForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.lblTransport = new System.Windows.Forms.Label();
            this.cboTransport = new System.Windows.Forms.ComboBox();
            this.lblComPort = new System.Windows.Forms.Label();
            this.cboComPort = new System.Windows.Forms.ComboBox();
            this.lblTcpHost = new System.Windows.Forms.Label();
            this.txtTcpHost = new System.Windows.Forms.TextBox();
            this.lblTcpPort = new System.Windows.Forms.Label();
            this.txtTcpPort = new System.Windows.Forms.TextBox();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.lblTcpPassword = new System.Windows.Forms.Label();
            this.txtTcpPassword = new System.Windows.Forms.TextBox();
            this.chkSetPosition = new System.Windows.Forms.CheckBox();
            this.txtSetPosition = new System.Windows.Forms.TextBox();
            this.lblStepsPerDegree = new System.Windows.Forms.Label();
            this.txtStepsPerDegree = new System.Windows.Forms.TextBox();
            this.lblMaxSpeed = new System.Windows.Forms.Label();
            this.txtMaxSpeed = new System.Windows.Forms.TextBox();
            this.lblAcceleration = new System.Windows.Forms.Label();
            this.txtAcceleration = new System.Windows.Forms.TextBox();
            this.chkContHold = new System.Windows.Forms.CheckBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.lblTestResult = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(175, 19);
            this.lblTitle.Text = "ECAA rotator connection";
            // 
            // picASCOM
            // 
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = global::ASCOM.scopefocus.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(270, 9);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            // 
            // chkTrace
            // 
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(16, 50);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(69, 17);
            this.chkTrace.Text = "Trace on";
            this.chkTrace.CheckedChanged += new System.EventHandler(this.chkTrace_CheckedChanged);
            // 
            // lblTransport
            // 
            this.lblTransport.AutoSize = true;
            this.lblTransport.Location = new System.Drawing.Point(24, 81);
            this.lblTransport.Name = "lblTransport";
            this.lblTransport.Size = new System.Drawing.Size(52, 13);
            this.lblTransport.Text = "Transport";
            // 
            // cboTransport
            // 
            this.cboTransport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTransport.Location = new System.Drawing.Point(120, 78);
            this.cboTransport.Name = "cboTransport";
            this.cboTransport.Size = new System.Drawing.Size(140, 21);
            this.cboTransport.SelectedIndexChanged += new System.EventHandler(this.cboTransport_SelectedIndexChanged);
            // 
            // lblComPort
            // 
            this.lblComPort.AutoSize = true;
            this.lblComPort.Location = new System.Drawing.Point(24, 109);
            this.lblComPort.Name = "lblComPort";
            this.lblComPort.Size = new System.Drawing.Size(58, 13);
            this.lblComPort.Text = "Comm Port";
            // 
            // cboComPort
            // 
            this.cboComPort.Location = new System.Drawing.Point(120, 106);
            this.cboComPort.Name = "cboComPort";
            this.cboComPort.Size = new System.Drawing.Size(140, 21);
            // 
            // lblTcpHost
            // 
            this.lblTcpHost.AutoSize = true;
            this.lblTcpHost.Location = new System.Drawing.Point(24, 137);
            this.lblTcpHost.Name = "lblTcpHost";
            this.lblTcpHost.Size = new System.Drawing.Size(51, 13);
            this.lblTcpHost.Text = "TCP Host";
            // 
            // txtTcpHost
            // 
            this.txtTcpHost.Location = new System.Drawing.Point(120, 134);
            this.txtTcpHost.Name = "txtTcpHost";
            this.txtTcpHost.Size = new System.Drawing.Size(140, 20);
            this.txtTcpHost.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblTcpPort
            // 
            this.lblTcpPort.AutoSize = true;
            this.lblTcpPort.Location = new System.Drawing.Point(24, 165);
            this.lblTcpPort.Name = "lblTcpPort";
            this.lblTcpPort.Size = new System.Drawing.Size(49, 13);
            this.lblTcpPort.Text = "TCP Port";
            // 
            // txtTcpPort
            // 
            this.txtTcpPort.Location = new System.Drawing.Point(120, 162);
            this.txtTcpPort.Name = "txtTcpPort";
            this.txtTcpPort.Size = new System.Drawing.Size(140, 20);
            this.txtTcpPort.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Location = new System.Drawing.Point(24, 193);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(68, 13);
            this.lblTimeout.Text = "Timeout (ms)";
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(120, 190);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(140, 20);
            this.txtTimeout.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblTcpPassword
            // 
            this.lblTcpPassword.AutoSize = true;
            this.lblTcpPassword.Location = new System.Drawing.Point(24, 221);
            this.lblTcpPassword.Name = "lblTcpPassword";
            this.lblTcpPassword.Size = new System.Drawing.Size(78, 13);
            this.lblTcpPassword.Text = "TCP Password";
            // 
            // txtTcpPassword
            // 
            this.txtTcpPassword.Location = new System.Drawing.Point(120, 218);
            this.txtTcpPassword.MaxLength = 15;
            this.txtTcpPassword.Name = "txtTcpPassword";
            this.txtTcpPassword.PasswordChar = '*';
            this.txtTcpPassword.Size = new System.Drawing.Size(140, 20);
            // 
            // chkSetPosition
            // 
            this.chkSetPosition.AutoSize = true;
            this.chkSetPosition.Location = new System.Drawing.Point(27, 250);
            this.chkSetPosition.Name = "chkSetPosition";
            this.chkSetPosition.Size = new System.Drawing.Size(82, 17);
            this.chkSetPosition.Text = "Set Position";
            this.chkSetPosition.CheckedChanged += new System.EventHandler(this.chkSetPosition_CheckedChanged);
            // 
            // txtSetPosition
            // 
            this.txtSetPosition.Enabled = false;
            this.txtSetPosition.Location = new System.Drawing.Point(120, 248);
            this.txtSetPosition.Name = "txtSetPosition";
            this.txtSetPosition.Size = new System.Drawing.Size(140, 20);
            this.txtSetPosition.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblStepsPerDegree
            // 
            this.lblStepsPerDegree.AutoSize = true;
            this.lblStepsPerDegree.Location = new System.Drawing.Point(24, 279);
            this.lblStepsPerDegree.Name = "lblStepsPerDegree";
            this.lblStepsPerDegree.Size = new System.Drawing.Size(72, 13);
            this.lblStepsPerDegree.Text = "Steps/degree";
            // 
            // txtStepsPerDegree
            // 
            this.txtStepsPerDegree.Location = new System.Drawing.Point(120, 276);
            this.txtStepsPerDegree.Name = "txtStepsPerDegree";
            this.txtStepsPerDegree.Size = new System.Drawing.Size(140, 20);
            this.txtStepsPerDegree.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblMaxSpeed
            // 
            this.lblMaxSpeed.AutoSize = true;
            this.lblMaxSpeed.Location = new System.Drawing.Point(24, 307);
            this.lblMaxSpeed.Name = "lblMaxSpeed";
            this.lblMaxSpeed.Size = new System.Drawing.Size(58, 13);
            this.lblMaxSpeed.Text = "Max Speed";
            // 
            // txtMaxSpeed
            // 
            this.txtMaxSpeed.Location = new System.Drawing.Point(120, 304);
            this.txtMaxSpeed.Name = "txtMaxSpeed";
            this.txtMaxSpeed.Size = new System.Drawing.Size(140, 20);
            this.txtMaxSpeed.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // lblAcceleration
            // 
            this.lblAcceleration.AutoSize = true;
            this.lblAcceleration.Location = new System.Drawing.Point(24, 335);
            this.lblAcceleration.Name = "lblAcceleration";
            this.lblAcceleration.Size = new System.Drawing.Size(66, 13);
            this.lblAcceleration.Text = "Acceleration";
            // 
            // txtAcceleration
            // 
            this.txtAcceleration.Location = new System.Drawing.Point(120, 332);
            this.txtAcceleration.Name = "txtAcceleration";
            this.txtAcceleration.Size = new System.Drawing.Size(140, 20);
            this.txtAcceleration.TextChanged += new System.EventHandler(this.ValidateFields);
            // 
            // chkContHold
            // 
            this.chkContHold.AutoSize = true;
            this.chkContHold.Location = new System.Drawing.Point(120, 362);
            this.chkContHold.Name = "chkContHold";
            this.chkContHold.Size = new System.Drawing.Size(104, 17);
            this.chkContHold.Text = "Continuous Hold";
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(120, 390);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(140, 25);
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // lblTestResult
            // 
            this.lblTestResult.Location = new System.Drawing.Point(24, 418);
            this.lblTestResult.Name = "lblTestResult";
            this.lblTestResult.Size = new System.Drawing.Size(236, 20);
            this.lblTestResult.Text = "";
            // 
            // cmdOK
            // 
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(140, 445);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 28);
            this.cmdOK.Text = "OK";
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(225, 445);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 28);
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // SetupDialogForm
            // 
            this.AcceptButton = this.cmdOK;
            this.CancelButton = this.cmdCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 485);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.lblTestResult);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.chkContHold);
            this.Controls.Add(this.txtAcceleration);
            this.Controls.Add(this.lblAcceleration);
            this.Controls.Add(this.txtMaxSpeed);
            this.Controls.Add(this.lblMaxSpeed);
            this.Controls.Add(this.txtStepsPerDegree);
            this.Controls.Add(this.lblStepsPerDegree);
            this.Controls.Add(this.txtSetPosition);
            this.Controls.Add(this.chkSetPosition);
            this.Controls.Add(this.txtTcpPassword);
            this.Controls.Add(this.lblTcpPassword);
            this.Controls.Add(this.txtTimeout);
            this.Controls.Add(this.lblTimeout);
            this.Controls.Add(this.txtTcpPort);
            this.Controls.Add(this.lblTcpPort);
            this.Controls.Add(this.txtTcpHost);
            this.Controls.Add(this.lblTcpHost);
            this.Controls.Add(this.cboComPort);
            this.Controls.Add(this.lblComPort);
            this.Controls.Add(this.cboTransport);
            this.Controls.Add(this.lblTransport);
            this.Controls.Add(this.chkTrace);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ECAA Rotator Setup";
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.CheckBox chkTrace;
        private System.Windows.Forms.Label lblTransport;
        private System.Windows.Forms.ComboBox cboTransport;
        private System.Windows.Forms.Label lblComPort;
        private System.Windows.Forms.ComboBox cboComPort;
        private System.Windows.Forms.Label lblTcpHost;
        private System.Windows.Forms.TextBox txtTcpHost;
        private System.Windows.Forms.Label lblTcpPort;
        private System.Windows.Forms.TextBox txtTcpPort;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.Label lblTcpPassword;
        private System.Windows.Forms.TextBox txtTcpPassword;
        private System.Windows.Forms.CheckBox chkSetPosition;
        private System.Windows.Forms.TextBox txtSetPosition;
        private System.Windows.Forms.Label lblStepsPerDegree;
        private System.Windows.Forms.TextBox txtStepsPerDegree;
        private System.Windows.Forms.Label lblMaxSpeed;
        private System.Windows.Forms.TextBox txtMaxSpeed;
        private System.Windows.Forms.Label lblAcceleration;
        private System.Windows.Forms.TextBox txtAcceleration;
        private System.Windows.Forms.CheckBox chkContHold;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Label lblTestResult;
    }
}
