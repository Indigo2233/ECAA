using System;
using System.Windows.Forms;

namespace ASCOM.scopefocus
{
    public partial class Form1 : Form
    {

        private ASCOM.DriverAccess.Rotator driver;

        public Form1()
        {
            InitializeComponent();
            SetUIState();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsConnected)
                driver.Connected = false;

            Properties.Settings.Default.Save();
        }
        private float stepsize;
        //private float stepsize
        //{
        //    get { return stepsize; }
        //    set { stepsize = value; }
        //}


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
            else
            {
                driver = new ASCOM.DriverAccess.Rotator(Properties.Settings.Default.DriverId);
                driver.Connected = true;
                SetUIState();
                timer1.Start();
             //   stepsize = driver.StepSize;
            }
            SetUIState();
        }

        private void SetUIState()
        {
            buttonConnect.Enabled = !string.IsNullOrEmpty(Properties.Settings.Default.DriverId);
            buttonChoose.Enabled = !IsConnected;
            buttonConnect.Text = IsConnected ? "Disconnect" : "Connect";
        }

        private bool IsConnected
        {
            get
            {
                return ((this.driver != null) && (driver.Connected == true));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // CW button
            driver.Move(stepsize);
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            driver.Move(-stepsize);
            //CCW button
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            stepsize = Convert.ToSingle(textBox2.Text);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.Text = driver.Position.ToString();
            textBox3.Text = driver.IsMoving.ToString();
            textBox5.Text = driver.TargetPosition.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            driver.MoveAbsolute(Convert.ToSingle(textBox4.Text));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            driver.Halt();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            driver.Action("Home", "");
        }
    }
}
