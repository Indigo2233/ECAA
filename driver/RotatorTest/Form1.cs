using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ASCOM.scopefocus
{
    public partial class Form1 : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(17, 19, 24);
        private static readonly Color PanelColor = Color.FromArgb(26, 31, 40);
        private static readonly Color PanelAltColor = Color.FromArgb(32, 39, 52);
        private static readonly Color LineColor = Color.FromArgb(56, 66, 82);
        private static readonly Color TextColor = Color.FromArgb(242, 245, 248);
        private static readonly Color MutedColor = Color.FromArgb(174, 184, 198);
        private static readonly Color AccentColor = Color.FromArgb(77, 182, 172);
        private static readonly Color DangerColor = Color.FromArgb(238, 107, 99);
        private static readonly Color AccentTextColor = Color.FromArgb(7, 18, 17);
        private static readonly Color DangerTextColor = Color.FromArgb(33, 7, 6);

        private ASCOM.DriverAccess.Rotator driver;
        private float stepsize = 1.0f;

        public Form1()
        {
            InitializeComponent();
            ApplyTheme();
            UpdateStepSize();
            SetUIState();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsConnected)
                driver.Connected = false;

            Properties.Settings.Default.Save();
        }

        private void buttonChoose_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DriverId = ASCOM.DriverAccess.Rotator.Choose(Properties.Settings.Default.DriverId);
            SetUIState();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (IsConnected)
            {
                driver.Connected = false;
                timer1.Stop();
                SetUIState();
                return;
            }

            driver = new ASCOM.DriverAccess.Rotator(Properties.Settings.Default.DriverId);
            driver.Connected = true;
            SetUIState();
            timer1.Start();
        }

        private void SetUIState()
        {
            bool isConnected = IsConnected;
            buttonConnect.Enabled = !string.IsNullOrEmpty(Properties.Settings.Default.DriverId);
            buttonChoose.Enabled = !isConnected;
            buttonConnect.Text = isConnected ? "Disconnect" : "Connect";
            button1.Enabled = isConnected;
            button2.Enabled = isConnected;
            button3.Enabled = isConnected;
            button4.Enabled = isConnected;
            button5.Enabled = isConnected;
            textBox2.Enabled = isConnected;
            textBox4.Enabled = isConnected;
            StyleButton(buttonConnect, isConnected ? DangerColor : AccentColor, isConnected ? DangerTextColor : AccentTextColor);
        }

        private bool IsConnected
        {
            get
            {
                return ((driver != null) && (driver.Connected == true));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!EnsureConnected() || !UpdateStepSize())
                return;

            driver.Move(stepsize);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!EnsureConnected() || !UpdateStepSize())
                return;

            driver.Move(-stepsize);
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            UpdateStepSize();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!IsConnected)
                return;

            textBox1.Text = driver.Position.ToString(CultureInfo.CurrentCulture);
            textBox3.Text = driver.IsMoving.ToString();
            textBox5.Text = driver.TargetPosition.ToString(CultureInfo.CurrentCulture);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!EnsureConnected())
                return;

            float target;
            if (!TryReadAngle(textBox4, "Target angle", out target))
                return;

            driver.MoveAbsolute(target);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!EnsureConnected())
                return;

            driver.Halt();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!EnsureConnected())
                return;

            driver.Action("Home", "");
        }

        private bool EnsureConnected()
        {
            if (IsConnected)
                return true;

            MessageBox.Show("Connect to an ASCOM rotator driver first.", "ECAA Rotator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private bool UpdateStepSize()
        {
            float value;
            if (!TryReadAngle(textBox2, "Relative step", out value))
                return false;

            stepsize = value;
            return true;
        }

        private bool TryReadAngle(TextBox textBox, string fieldName, out float value)
        {
            string text = textBox.Text.Trim();
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            MessageBox.Show(fieldName + " must be a numeric degree value.", "ECAA Rotator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            textBox.Focus();
            textBox.SelectAll();
            return false;
        }

        private void ApplyTheme()
        {
            BackColor = BackgroundColor;
            ForeColor = TextColor;

            ApplyTheme(Controls);

            labelTitle.ForeColor = TextColor;
            labelSubtitle.ForeColor = MutedColor;
            labelDriverId.ForeColor = TextColor;
            labelDriverId.BackColor = Color.FromArgb(18, 23, 32);

            StyleButton(buttonChoose, PanelAltColor, TextColor);
            StyleButton(buttonConnect, AccentColor, AccentTextColor);
            StyleButton(button1, PanelAltColor, TextColor);
            StyleButton(button2, PanelAltColor, TextColor);
            StyleButton(button3, PanelAltColor, TextColor);
            StyleButton(button4, AccentColor, AccentTextColor);
            StyleButton(button5, DangerColor, DangerTextColor);
        }

        private void ApplyTheme(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                Panel panel = control as Panel;
                if (panel != null)
                {
                    panel.BackColor = PanelColor;
                    panel.ForeColor = TextColor;
                }

                Label label = control as Label;
                if (label != null)
                {
                    label.ForeColor = MutedColor;
                    label.BackColor = Color.Transparent;
                }

                TextBox textBox = control as TextBox;
                if (textBox != null)
                {
                    textBox.BackColor = Color.FromArgb(18, 23, 32);
                    textBox.ForeColor = TextColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }

                if (control.HasChildren)
                {
                    ApplyTheme(control.Controls);
                }
            }
        }

        private void StyleButton(Button button, Color backColor, Color foreColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = LineColor;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.UseVisualStyleBackColor = false;
        }
    }
}
