using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.scopefocus
{
    [ComVisible(false)]
    public partial class SetupDialogForm : Form
    {
        private static readonly Color BgColor = Color.FromArgb(17, 19, 24);
        private static readonly Color FieldBg = Color.FromArgb(18, 23, 32);
        private static readonly Color DisabledBg = Color.FromArgb(32, 39, 52);
        private static readonly Color TextColor = Color.FromArgb(242, 245, 248);
        private static readonly Color LabelColor = Color.FromArgb(174, 184, 198);
        private static readonly Color AccentColor = Color.FromArgb(77, 182, 172);
        private static readonly Color BorderColor = Color.FromArgb(56, 66, 82);

        private static void DebugLog(string method, string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ASCOM", "Logs", "ECAA-Rotator-Debug.log");
                string dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + method + "] " + message;
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }

        public SetupDialogForm()
        {
            InitializeComponent();
            LoadSettings();
            ApplyTheme();
            UpdateFieldStates();
            ValidateAllFields();

            foreach (Control c in Controls)
            {
                if (c is TextBox)
                    c.KeyDown += TextBox_KeyDown;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
                e.SuppressKeyPress = true;
            }
        }

        private void LoadSettings()
        {
            chkTrace.Checked = Rotator.traceState;

            cboTransport.Items.Clear();
            cboTransport.Items.AddRange(new object[] { "Serial", "TCP" });
            cboTransport.SelectedItem = string.Equals(Rotator.transport, "TCP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Rotator.transport, "WiFi TCP", StringComparison.OrdinalIgnoreCase)
                ? "TCP" : "Serial";

            cboComPort.Items.Clear();
            cboComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (cboComPort.Items.Contains(Rotator.comPort))
                cboComPort.SelectedItem = Rotator.comPort;
            else
                cboComPort.Text = Rotator.comPort;

            using (Profile p = new Profile())
            {
                p.DeviceType = "Rotator";
                txtTcpHost.Text = Rotator.GetProfileValue(p, Rotator.tcpHostProfileName, Rotator.tcpHostDefault);
                txtTcpPort.Text = Rotator.GetProfileValue(p, Rotator.tcpPortProfileName, Rotator.tcpPortDefault);
                txtTimeout.Text = Rotator.GetProfileValue(p, Rotator.commandTimeoutProfileName, Rotator.commandTimeoutDefault);
                txtTcpPassword.Text = Rotator.GetProfileValue(p, Rotator.tcpPasswordProfileName, Rotator.tcpPasswordDefault);
                txtStepsPerDegree.Text = Rotator.GetProfileValue(p, "StepsPerDegree", "100");
                txtMaxSpeed.Text = Rotator.GetProfileValue(p, "MaxSpeed", "800");
                txtAcceleration.Text = Rotator.GetProfileValue(p, "Acceleration", "1000");
                chkContHold.Checked = Rotator.GetProfileValue(p, "ContHold", "False").Equals("True", StringComparison.OrdinalIgnoreCase);
                chkSetPosition.Checked = Rotator.GetProfileValue(p, "SetPos", "False").Equals("True", StringComparison.OrdinalIgnoreCase);
                txtSetPosition.Text = Rotator.GetProfileValue(p, "Pos", "");
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (!ValidateAllFields())
            {
                DialogResult = DialogResult.None;
                return;
            }

            using (Profile p = new Profile())
            {
                p.DeviceType = "Rotator";
                p.WriteValue(Rotator.driverID, Rotator.transportProfileName, cboTransport.Text);
                p.WriteValue(Rotator.driverID, Rotator.comPortProfileName, cboComPort.Text);
                p.WriteValue(Rotator.driverID, Rotator.comPortLegacyProfileName, cboComPort.Text);
                p.WriteValue(Rotator.driverID, Rotator.tcpHostProfileName, txtTcpHost.Text.Trim());
                p.WriteValue(Rotator.driverID, Rotator.tcpPortProfileName, txtTcpPort.Text.Trim());
                p.WriteValue(Rotator.driverID, Rotator.commandTimeoutProfileName, txtTimeout.Text.Trim());
                p.WriteValue(Rotator.driverID, Rotator.tcpPasswordProfileName, txtTcpPassword.Text.Trim());
                p.WriteValue(Rotator.driverID, "StepsPerDegree", txtStepsPerDegree.Text.Trim());
                p.WriteValue(Rotator.driverID, "MaxSpeed", txtMaxSpeed.Text.Trim());
                p.WriteValue(Rotator.driverID, "Acceleration", txtAcceleration.Text.Trim());
                p.WriteValue(Rotator.driverID, "ContHold", chkContHold.Checked.ToString());
                p.WriteValue(Rotator.driverID, "SetPos", chkSetPosition.Checked.ToString());
                if (chkSetPosition.Checked)
                    p.WriteValue(Rotator.driverID, "Pos", txtSetPosition.Text.Trim());
                p.WriteValue(Rotator.driverID, Rotator.traceStateProfileName, chkTrace.Checked.ToString());
            }

            Rotator.transport = cboTransport.Text;
            Rotator.comPort = cboComPort.Text;
            Rotator.tcpHost = txtTcpHost.Text.Trim();
            Rotator.tcpPort = Rotator.ParseInt(txtTcpPort.Text.Trim(), 4030);
            Rotator.commandTimeoutMs = Rotator.ParseInt(txtTimeout.Text.Trim(), 3000);
            Rotator.tcpPassword = txtTcpPassword.Text.Trim();
            Rotator.stepsPerDegree = Rotator.ParseInt(txtStepsPerDegree.Text.Trim(), 100);
            Rotator.maxSpeed = Rotator.ParseInt(txtMaxSpeed.Text.Trim(), 800);
            Rotator.acceleration = Rotator.ParseInt(txtAcceleration.Text.Trim(), 1000);
            Rotator.traceState = chkTrace.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e)
        {
            try { System.Diagnostics.Process.Start("http://ascom-standards.org/"); }
            catch { }
        }

        private void chkTrace_CheckedChanged(object sender, EventArgs e)
        {
            Rotator.traceState = chkTrace.Checked;
        }

        private void cboTransport_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFieldStates();
            ValidateAllFields();
        }

        private void chkSetPosition_CheckedChanged(object sender, EventArgs e)
        {
            txtSetPosition.Enabled = chkSetPosition.Checked;
            StyleTextBox(txtSetPosition);
            ValidateAllFields();
        }

        private void ValidateFields(object sender, EventArgs e)
        {
            ValidateAllFields();
        }

        private bool ValidateAllFields()
        {
            int val;
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtStepsPerDegree.Text) || !int.TryParse(txtStepsPerDegree.Text.Trim(), out val) || val <= 0)
                valid = false;
            if (cboTransport.Text == "TCP" && string.IsNullOrWhiteSpace(txtTcpHost.Text))
                valid = false;
            if (!int.TryParse(txtTcpPort.Text.Trim(), out val) || val <= 0)
                valid = false;
            if (!int.TryParse(txtTimeout.Text.Trim(), out val) || val <= 0)
                valid = false;
            if (string.IsNullOrWhiteSpace(txtMaxSpeed.Text) || !int.TryParse(txtMaxSpeed.Text.Trim(), out val) || val <= 0)
                valid = false;
            if (string.IsNullOrWhiteSpace(txtAcceleration.Text) || !int.TryParse(txtAcceleration.Text.Trim(), out val) || val <= 0)
                valid = false;
            if (chkSetPosition.Checked && string.IsNullOrWhiteSpace(txtSetPosition.Text))
                valid = false;

            cmdOK.Enabled = valid;
            return valid;
        }

        private void UpdateFieldStates()
        {
            bool isTcp = cboTransport.Text == "TCP";
            cboComPort.Enabled = !isTcp;
            txtTcpHost.Enabled = isTcp;
            txtTcpPort.Enabled = isTcp;
            txtTcpPassword.Enabled = isTcp;

            StyleComboBox(cboComPort);
            StyleTextBox(txtTcpHost);
            StyleTextBox(txtTcpPort);
            StyleTextBox(txtTcpPassword);
        }

        private void ApplyTheme()
        {
            BackColor = BgColor;
            ForeColor = TextColor;

            lblTitle.ForeColor = TextColor;
            lblTitle.BackColor = Color.Transparent;

            foreach (Control c in Controls)
            {
                if (c is Label) { c.ForeColor = LabelColor; c.BackColor = Color.Transparent; }
                if (c is TextBox) StyleTextBox((TextBox)c);
                if (c is ComboBox) StyleComboBox((ComboBox)c);
                if (c is CheckBox) StyleCheckBox((CheckBox)c);
            }

            StyleButton(cmdOK, AccentColor, Color.Black);
            StyleButton(cmdCancel, DisabledBg, TextColor);
            StyleButton(btnTestConnection, DisabledBg, TextColor);
            picASCOM.BackColor = BgColor;
            lblTestResult.ForeColor = LabelColor;
            lblTestResult.BackColor = Color.Transparent;
        }

        private void StyleTextBox(TextBox tb)
        {
            tb.BackColor = tb.Enabled ? FieldBg : DisabledBg;
            tb.ForeColor = tb.Enabled ? TextColor : LabelColor;
            tb.BorderStyle = BorderStyle.FixedSingle;
        }

        private void StyleComboBox(ComboBox cb)
        {
            cb.BackColor = cb.Enabled ? FieldBg : DisabledBg;
            cb.ForeColor = cb.Enabled ? TextColor : LabelColor;
            cb.FlatStyle = FlatStyle.Flat;
        }

        private void StyleCheckBox(CheckBox cb)
        {
            cb.ForeColor = LabelColor;
            cb.BackColor = BgColor;
            cb.FlatStyle = FlatStyle.Flat;
            cb.FlatAppearance.BorderColor = BorderColor;
        }

        private void StyleButton(Button btn, Color bg, Color fg)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = bg;
            btn.ForeColor = fg;
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            lblTestResult.ForeColor = LabelColor;
            lblTestResult.Text = "Testing...";
            btnTestConnection.Enabled = false;
            Application.DoEvents();

            string host = txtTcpHost.Text.Trim();
            int port = Rotator.ParseInt(txtTcpPort.Text.Trim(), 4030);
            string password = txtTcpPassword.Text.Trim();
            int connectTimeout = 10000;
            int readTimeout = 10000;

            DebugLog("TestBtn", "Host=" + host + " Port=" + port + " HasPassword=" + !string.IsNullOrEmpty(password));

            if (cboTransport.Text != "TCP")
            {
                lblTestResult.ForeColor = Color.Orange;
                lblTestResult.Text = "Test only works for TCP transport";
                btnTestConnection.Enabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "TCP Host is empty";
                btnTestConnection.Enabled = true;
                return;
            }

            System.Net.Sockets.TcpClient client = null;
            try
            {
                DebugLog("TestBtn", "Connecting to " + host + ":" + port + " timeout=" + connectTimeout + "ms");
                lblTestResult.Text = "Connecting to " + host + ":" + port + "...";
                Application.DoEvents();

                client = new System.Net.Sockets.TcpClient();
                var result = client.BeginConnect(host, port, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(connectTimeout);
                
                DebugLog("TestBtn", "WaitOne returned: connected=" + connected + " client.Connected=" + client.Connected);
                
                if (!connected || !client.Connected)
                {
                    DebugLog("TestBtn", "TIMEOUT - connection failed");
                    lblTestResult.ForeColor = Color.Red;
                    lblTestResult.Text = "Timeout connecting to " + host + ":" + port;
                    btnTestConnection.Enabled = true;
                    return;
                }
                
                client.EndConnect(result);
                DebugLog("TestBtn", "TCP connected successfully");
                lblTestResult.Text = "Connected, sending command...";
                Application.DoEvents();

                var stream = client.GetStream();
                stream.ReadTimeout = readTimeout;
                stream.WriteTimeout = readTimeout;

                DebugLog("TestBtn", "Sending V# command");
                byte[] cmd = System.Text.Encoding.ASCII.GetBytes("V#");
                stream.Write(cmd, 0, cmd.Length);
                stream.Flush();
                
                System.Threading.Thread.Sleep(300);
                
                DebugLog("TestBtn", "Reading response...");
                byte[] buf = new byte[128];
                int len = stream.Read(buf, 0, buf.Length);
                string response = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                DebugLog("TestBtn", "Response (" + len + " bytes): " + response.Replace("\r", "\\r").Replace("\n", "\\n"));

                if (!response.Contains("V "))
                {
                    DebugLog("TestBtn", "Bad response - no 'V ' found");
                    lblTestResult.ForeColor = Color.Red;
                    lblTestResult.Text = "Bad response: " + response.Replace("\r", "").Replace("\n", "");
                    btnTestConnection.Enabled = true;
                    return;
                }

                string version = response.Trim();
                DebugLog("TestBtn", "Version OK: " + version);

                if (!string.IsNullOrEmpty(password))
                {
                    lblTestResult.Text = "Authenticating...";
                    Application.DoEvents();

                    DebugLog("TestBtn", "Sending auth command K <password>#");
                    cmd = System.Text.Encoding.ASCII.GetBytes("K " + password + "#");
                    stream.Write(cmd, 0, cmd.Length);
                    stream.Flush();
                    
                    System.Threading.Thread.Sleep(300);
                    
                    len = stream.Read(buf, 0, buf.Length);
                    response = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                    DebugLog("TestBtn", "Auth response: " + response);

                    if (!response.StartsWith("K ok"))
                    {
                        DebugLog("TestBtn", "Auth FAILED");
                        lblTestResult.ForeColor = Color.Orange;
                        lblTestResult.Text = "Auth failed: " + response.Trim();
                        btnTestConnection.Enabled = true;
                        return;
                    }
                    DebugLog("TestBtn", "Auth OK");
                }

                DebugLog("TestBtn", "SUCCESS");
                lblTestResult.ForeColor = Color.LimeGreen;
                lblTestResult.Text = "OK! " + version;
            }
            catch (Exception ex)
            {
                DebugLog("TestBtn", "EXCEPTION: " + ex.GetType().Name + " - " + ex.Message);
                if (ex.InnerException != null)
                    DebugLog("TestBtn", "Inner: " + ex.InnerException.Message);
                lblTestResult.ForeColor = Color.Red;
                string msg = ex.Message;
                if (ex.InnerException != null) msg = ex.InnerException.Message;
                lblTestResult.Text = "Error: " + msg;
            }
            finally
            {
                if (client != null)
                {
                    try { client.Close(); } catch { }
                }
                btnTestConnection.Enabled = true;
            }
        }
    }
}
