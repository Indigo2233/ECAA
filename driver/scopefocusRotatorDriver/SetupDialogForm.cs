using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.scopefocus;

namespace ASCOM.scopefocus
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {

            if (!ValidateInputs())
            {
                this.DialogResult = DialogResult.None;
                return;
            }
            else
            {
                using (ASCOM.Utilities.Profile p = new Utilities.Profile())
                {
                    p.DeviceType = "Rotator";
                    p.WriteValue(Rotator.driverID, Rotator.transportProfileName, comboBoxTransport.Text);
                    p.WriteValue(Rotator.driverID, Rotator.comPortLegacyProfileName, comboBoxComPort.Text);
                    p.WriteValue(Rotator.driverID, Rotator.comPortProfileName, comboBoxComPort.Text);
                    p.WriteValue(Rotator.driverID, Rotator.tcpHostProfileName, textBoxTcpHost.Text.Trim());
                    p.WriteValue(Rotator.driverID, Rotator.tcpPortProfileName, textBoxTcpPort.Text.Trim());
                    p.WriteValue(Rotator.driverID, Rotator.commandTimeoutProfileName, textBoxTimeout.Text.Trim());
                    p.WriteValue(Rotator.driverID, "SetPos", checkBox1.Checked.ToString());
                    // 6-16-16 added 2 lines below
                    //   p.WriteValue(Rotator.driverID, "Reverse", reverseCheckBox1.Checked.ToString());  // motor sitting shaft up turns clockwise with increasing numbers if NOT reversed
                    p.WriteValue(Rotator.driverID, "ContHold", checkBox2.Checked.ToString());


                    p.WriteValue(Rotator.driverID, "StepsPerDegree", textBox2.Text.ToString());
                    //   p.WriteValue(Focuser.driverID, "RPM", textBoxRpm.Text);
                    if (checkBox1.Checked)
                    {
                        p.WriteValue(Rotator.driverID, "Pos", textBox1.Text.ToString());
                    }
                    //    p.WriteValue(Focuser.driverID, "TempDisp", radioCelcius.Checked ? "C" : "F");
                }
                Dispose();




                // Place any validation constraint checks here
                // Update the state variables with results from the dialogue
                Rotator.transport = comboBoxTransport.Text;
                Rotator.comPort = comboBoxComPort.Text;
                Rotator.tcpHost = textBoxTcpHost.Text.Trim();
                Rotator.tcpPort = Rotator.ParseInt(textBoxTcpPort.Text.Trim(), 4030);
                Rotator.commandTimeoutMs = Rotator.ParseInt(textBoxTimeout.Text.Trim(), 3000);
                Rotator.traceState = chkTrace.Checked;
            }
        }
        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private bool ValidateInputs()
        {
            int numericValue;
            if (string.IsNullOrWhiteSpace(textBox2.Text) || !int.TryParse(textBox2.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a positive value for Steps per Degree");
                return false;
            }
            if ((string.IsNullOrWhiteSpace(textBox1.Text) && checkBox1.Checked))
            {
                MessageBox.Show("You must specify a position when Set Position is checked");
                return false;
            }
            if (comboBoxTransport.Text == "TCP" && string.IsNullOrWhiteSpace(textBoxTcpHost.Text))
            {
                MessageBox.Show("You must specify a TCP host");
                return false;
            }
            if (!int.TryParse(textBoxTcpPort.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a valid TCP port");
                return false;
            }
            if (!int.TryParse(textBoxTimeout.Text.Trim(), out numericValue) || numericValue <= 0)
            {
                MessageBox.Show("You must specify a valid command timeout");
                return false;
            }
            return true;
        }

        private void checkTextBox() //don't allow close with steps/dgree blank or set pos checked and blank position
        {
            int numericValue;
            if (string.IsNullOrWhiteSpace(textBox2.Text) || !int.TryParse(textBox2.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false; 
            else if ((string.IsNullOrWhiteSpace(textBox1.Text) && checkBox1.Checked))
                cmdOK.Enabled = false;
            else if (comboBoxTransport.Text == "TCP" && string.IsNullOrWhiteSpace(textBoxTcpHost.Text))
                cmdOK.Enabled = false;
            else if (!int.TryParse(textBoxTcpPort.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false;
            else if (!int.TryParse(textBoxTimeout.Text.Trim(), out numericValue) || numericValue <= 0)
                cmdOK.Enabled = false;
            else 
                cmdOK.Enabled = true;
        }


        private void InitUI()
        {
           
            chkTrace.Checked = Rotator.traceState;
            comboBoxTransport.Items.Clear();
            comboBoxTransport.Items.AddRange(new object[] { "Serial", "TCP" });
            comboBoxTransport.SelectedItem = string.Equals(Rotator.transport, "TCP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Rotator.transport, "WiFi TCP", StringComparison.OrdinalIgnoreCase)
                ? "TCP"
                : "Serial";

            // set the list of com ports to those that are currently available
            comboBoxComPort.Items.Clear();
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());      // use System.IO because it's static
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(Rotator.comPort))
            {
                comboBoxComPort.SelectedItem = Rotator.comPort;
            }
            else
            {
                comboBoxComPort.Text = Rotator.comPort;
            }
            using (ASCOM.Utilities.Profile p = new Utilities.Profile())
            {
                p.DeviceType = "Rotator";
                textBoxTcpHost.Text = Rotator.GetProfileValue(p, Rotator.tcpHostProfileName, Rotator.tcpHostDefault);
                textBoxTcpPort.Text = Rotator.GetProfileValue(p, Rotator.tcpPortProfileName, Rotator.tcpPortDefault);
                textBoxTimeout.Text = Rotator.GetProfileValue(p, Rotator.commandTimeoutProfileName, Rotator.commandTimeoutDefault);
                textBox2.Text = Rotator.GetProfileValue(p, "StepsPerDegree", "100");
                if (p.GetValue(Rotator.driverID, "ContHold") == "True")
                    checkBox2.Checked = true;
                else
                    checkBox2.Checked = false;
            }
            if (!checkBox1.Checked)
                textBox1.Enabled = false;
            UpdateTransportFields();
            checkTextBox();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = false;
            if (checkBox1.Checked)
                enable = true;


            //  label2.Enabled = enable;
            textBox1.Enabled = enable;
            checkTextBox();
        }

        private void chkTrace_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTrace.Checked)
                Rotator.traceState = true;
            else
                Rotator.traceState = false;
        }



        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            checkTextBox();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            checkTextBox();
        }

        private void comboBoxTransport_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTransportFields();
            checkTextBox();
        }

        private void connectionTextBox_TextChanged(object sender, EventArgs e)
        {
            checkTextBox();
        }

        private void UpdateTransportFields()
        {
            bool serialSelected = comboBoxTransport.Text != "TCP";
            comboBoxComPort.Enabled = serialSelected;
            textBoxTcpHost.Enabled = !serialSelected;
            textBoxTcpPort.Enabled = !serialSelected;
        }
    }
}
