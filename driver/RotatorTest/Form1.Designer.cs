namespace ASCOM.scopefocus
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.buttonChoose = new System.Windows.Forms.Button();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.labelDriverId = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelSubtitle = new System.Windows.Forms.Label();
            this.panelConnection = new System.Windows.Forms.Panel();
            this.labelConnection = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.panelRelative = new System.Windows.Forms.Panel();
            this.labelRelative = new System.Windows.Forms.Label();
            this.panelAbsolute = new System.Windows.Forms.Panel();
            this.labelAbsolute = new System.Windows.Forms.Label();
            this.panelConnection.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.panelRelative.SuspendLayout();
            this.panelAbsolute.SuspendLayout();
            this.SuspendLayout();
            //
            // buttonChoose
            //
            this.buttonChoose.Location = new System.Drawing.Point(384, 24);
            this.buttonChoose.Name = "buttonChoose";
            this.buttonChoose.Size = new System.Drawing.Size(88, 34);
            this.buttonChoose.TabIndex = 1;
            this.buttonChoose.Text = "Choose";
            this.buttonChoose.UseVisualStyleBackColor = true;
            this.buttonChoose.Click += new System.EventHandler(this.buttonChoose_Click);
            //
            // buttonConnect
            //
            this.buttonConnect.Location = new System.Drawing.Point(482, 24);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(88, 34);
            this.buttonConnect.TabIndex = 2;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            //
            // labelDriverId
            //
            this.labelDriverId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelDriverId.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ASCOM.scopefocus.Properties.Settings.Default, "DriverId", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.labelDriverId.Location = new System.Drawing.Point(18, 29);
            this.labelDriverId.Name = "labelDriverId";
            this.labelDriverId.Size = new System.Drawing.Size(346, 26);
            this.labelDriverId.TabIndex = 0;
            this.labelDriverId.Text = global::ASCOM.scopefocus.Properties.Settings.Default.DriverId;
            this.labelDriverId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(18, 92);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(118, 36);
            this.button1.TabIndex = 1;
            this.button1.Text = "CW";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // button2
            //
            this.button2.Location = new System.Drawing.Point(146, 92);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(118, 36);
            this.button2.TabIndex = 2;
            this.button2.Text = "CCW";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            //
            // textBox1
            //
            this.textBox1.Location = new System.Drawing.Point(18, 52);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(170, 20);
            this.textBox1.TabIndex = 1;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Position (deg)";
            //
            // button3
            //
            this.button3.Location = new System.Drawing.Point(18, 92);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(118, 36);
            this.button3.TabIndex = 3;
            this.button3.Text = "Home";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            //
            // textBox2
            //
            this.textBox2.Location = new System.Drawing.Point(18, 52);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(246, 20);
            this.textBox2.TabIndex = 0;
            this.textBox2.Text = "1";
            this.textBox2.Leave += new System.EventHandler(this.textBox2_Leave);
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Relative step (deg)";
            //
            // timer1
            //
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(402, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Is Moving";
            //
            // textBox3
            //
            this.textBox3.Location = new System.Drawing.Point(402, 52);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(170, 20);
            this.textBox3.TabIndex = 5;
            //
            // button4
            //
            this.button4.Location = new System.Drawing.Point(146, 92);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(118, 36);
            this.button4.TabIndex = 2;
            this.button4.Text = "Move";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            //
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Target angle (deg)";
            //
            // textBox4
            //
            this.textBox4.Location = new System.Drawing.Point(18, 52);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(246, 20);
            this.textBox4.TabIndex = 1;
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(210, 32);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(92, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Target Position (deg)";
            //
            // textBox5
            //
            this.textBox5.Location = new System.Drawing.Point(210, 52);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(170, 20);
            this.textBox5.TabIndex = 3;
            //
            // button5
            //
            this.button5.Location = new System.Drawing.Point(18, 459);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(594, 44);
            this.button5.TabIndex = 6;
            this.button5.Text = "STOP";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            //
            // labelTitle
            //
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.Location = new System.Drawing.Point(22, 18);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(234, 30);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "ECAA Rotator Control";
            //
            // labelSubtitle
            //
            this.labelSubtitle.AutoSize = true;
            this.labelSubtitle.Location = new System.Drawing.Point(24, 50);
            this.labelSubtitle.Name = "labelSubtitle";
            this.labelSubtitle.Size = new System.Drawing.Size(197, 13);
            this.labelSubtitle.TabIndex = 1;
            this.labelSubtitle.Text = "ASCOM test panel for serial or TCP control";
            //
            // panelConnection
            //
            this.panelConnection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelConnection.Controls.Add(this.labelConnection);
            this.panelConnection.Controls.Add(this.labelDriverId);
            this.panelConnection.Controls.Add(this.buttonChoose);
            this.panelConnection.Controls.Add(this.buttonConnect);
            this.panelConnection.Location = new System.Drawing.Point(24, 82);
            this.panelConnection.Name = "panelConnection";
            this.panelConnection.Size = new System.Drawing.Size(592, 76);
            this.panelConnection.TabIndex = 2;
            //
            // labelConnection
            //
            this.labelConnection.AutoSize = true;
            this.labelConnection.Location = new System.Drawing.Point(18, 10);
            this.labelConnection.Name = "labelConnection";
            this.labelConnection.Size = new System.Drawing.Size(35, 13);
            this.labelConnection.TabIndex = 0;
            this.labelConnection.Text = "Driver";
            //
            // panelStatus
            //
            this.panelStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStatus.Controls.Add(this.labelStatus);
            this.panelStatus.Controls.Add(this.label1);
            this.panelStatus.Controls.Add(this.textBox1);
            this.panelStatus.Controls.Add(this.label5);
            this.panelStatus.Controls.Add(this.textBox5);
            this.panelStatus.Controls.Add(this.label3);
            this.panelStatus.Controls.Add(this.textBox3);
            this.panelStatus.Location = new System.Drawing.Point(24, 174);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(592, 100);
            this.panelStatus.TabIndex = 3;
            //
            // labelStatus
            //
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(18, 10);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(37, 13);
            this.labelStatus.TabIndex = 0;
            this.labelStatus.Text = "Status";
            //
            // panelRelative
            //
            this.panelRelative.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelRelative.Controls.Add(this.labelRelative);
            this.panelRelative.Controls.Add(this.label2);
            this.panelRelative.Controls.Add(this.textBox2);
            this.panelRelative.Controls.Add(this.button1);
            this.panelRelative.Controls.Add(this.button2);
            this.panelRelative.Location = new System.Drawing.Point(24, 290);
            this.panelRelative.Name = "panelRelative";
            this.panelRelative.Size = new System.Drawing.Size(286, 146);
            this.panelRelative.TabIndex = 4;
            //
            // labelRelative
            //
            this.labelRelative.AutoSize = true;
            this.labelRelative.Location = new System.Drawing.Point(18, 10);
            this.labelRelative.Name = "labelRelative";
            this.labelRelative.Size = new System.Drawing.Size(75, 13);
            this.labelRelative.TabIndex = 0;
            this.labelRelative.Text = "Relative move";
            //
            // panelAbsolute
            //
            this.panelAbsolute.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelAbsolute.Controls.Add(this.labelAbsolute);
            this.panelAbsolute.Controls.Add(this.label4);
            this.panelAbsolute.Controls.Add(this.textBox4);
            this.panelAbsolute.Controls.Add(this.button3);
            this.panelAbsolute.Controls.Add(this.button4);
            this.panelAbsolute.Location = new System.Drawing.Point(330, 290);
            this.panelAbsolute.Name = "panelAbsolute";
            this.panelAbsolute.Size = new System.Drawing.Size(286, 146);
            this.panelAbsolute.TabIndex = 5;
            //
            // labelAbsolute
            //
            this.labelAbsolute.AutoSize = true;
            this.labelAbsolute.Location = new System.Drawing.Point(18, 10);
            this.labelAbsolute.Name = "labelAbsolute";
            this.labelAbsolute.Size = new System.Drawing.Size(75, 13);
            this.labelAbsolute.TabIndex = 0;
            this.labelAbsolute.Text = "Absolute move";
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 526);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.panelAbsolute);
            this.Controls.Add(this.panelRelative);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelConnection);
            this.Controls.Add(this.labelSubtitle);
            this.Controls.Add(this.labelTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ECAA Rotator Control";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.panelConnection.ResumeLayout(false);
            this.panelConnection.PerformLayout();
            this.panelStatus.ResumeLayout(false);
            this.panelStatus.PerformLayout();
            this.panelRelative.ResumeLayout(false);
            this.panelRelative.PerformLayout();
            this.panelAbsolute.ResumeLayout(false);
            this.panelAbsolute.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonChoose;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Label labelDriverId;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelSubtitle;
        private System.Windows.Forms.Panel panelConnection;
        private System.Windows.Forms.Label labelConnection;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Panel panelRelative;
        private System.Windows.Forms.Label labelRelative;
        private System.Windows.Forms.Panel panelAbsolute;
        private System.Windows.Forms.Label labelAbsolute;
    }
}
