//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Rotator driver for ECAA
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Rotator interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Rotator

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;


namespace ASCOM.scopefocus
{
    //Started on 2-6-17
    // Your driver's DeviceID is ASCOM.ECAA.Rotator
    //
    // The Guid attribute sets the CLSID for ASCOM.ECAA.Rotator
    // The ClassInterface/None addribute prevents an empty interface called
    // _scopefocus from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Rotator Driver for ECAA.
    /// </summary>
    [Guid("6995b27f-5e2f-4c73-adae-8c9338c57762")]
    [ProgId("ASCOM.ECAA.Rotator")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Rotator : IRotatorV2
    {

        private IRotatorConnection connection;


        //  private TextWriter log;
        System.Threading.Mutex mutex = new System.Threading.Mutex();


        float lastPos = 0;
        //    double lastTemp = 0;
        bool lastMoving = false;
        bool lastLink = false;
        bool isReversed = false;
        long homeOffsetSteps = 0; // cached from firmware I# JSON, updated after Sync

        long UPDATETICKS = (long)(0.3 * 10000000.0); // 3,000,000 ticks = 300ms for faster UI updates
        long lastUpdate = 0;


        long lastL = 0;
        //************end add




        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.ECAA.Rotator";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "ASCOM Rotator Driver for ECAA ESP8266.";

        internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
        internal static string comPortDefault = "COM1";
        internal static string comPortLegacyProfileName = "ComPort";
        internal static string transportProfileName = "Transport";
        internal static string transportDefault = "Serial";
        internal static string tcpHostProfileName = "TcpHost";
        internal static string tcpHostDefault = "192.168.4.1";
        internal static string tcpPortProfileName = "TcpPort";
        internal static string tcpPortDefault = "4030";
        internal static string commandTimeoutProfileName = "CommandTimeoutMs";
        internal static string commandTimeoutDefault = "15000";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";
        internal static string tcpPasswordProfileName = "TcpPassword";
        internal static string tcpPasswordDefault = "";

        internal static string comPort; // Variables to hold the currrent device configuration
        internal static string transport;
        internal static string tcpHost;
        internal static int tcpPort;
        internal static int commandTimeoutMs;
        internal static bool traceState;
        internal static int stepsPerDegree;
        internal static int maxSpeed;
        internal static int acceleration;
        internal static string tcpPassword;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger tl;

        /// <summary>
        /// Debug log file path - always writes to this file regardless of trace setting
        /// </summary>
        private static string debugLogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ASCOM", "Logs", "ECAA-Rotator-Debug.log");

        /// <summary>
        /// Write debug message to file (always enabled, independent of trace setting)
        /// </summary>
        private static void DebugLog(string method, string message)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(debugLogPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + method + "] " + message;
                System.IO.File.AppendAllText(debugLogPath, line + Environment.NewLine);
            }
            catch { }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="scopefocus"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Rotator()
        {
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "ECAA Rotator");
            tl.Enabled = traceState;
            tl.LogMessage("Rotator", "Starting initialization");
            DebugLog("Rotator", "Starting initialization, trace=" + traceState);

            connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object
            //TODO: Implement your additional construction here

            tl.LogMessage("Rotator", "Completed initialization");
        }


        //
        // PUBLIC COM INTERFACE IRotatorV2 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                try

                {
                    var sa = new ArrayList();
                    sa.Add("Home");
                    return sa;

                }
                catch (Exception ex)

                {

                    throw new ASCOM.DriverException("Cannot get supported actions list.", ex);

                }

                //tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
               // return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            if (actionName == "Home")
            {
                CommandString("H#", false);
                return "";
            }
            else
               throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            this.CommandString(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            return ret.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0 || ret.Trim().StartsWith("1");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");

            if (!this.Connected || connection == null)
            {
                throw new ASCOM.NotConnectedException();
            }

            string temp = "999";
            mutex.WaitOne();
            try
            {
                tl.LogMessage("Sending Command: ", command);
                temp = connection.CommandString(command);
                tl.LogMessage("Got Response: ", temp);
            }
            catch (Exception e)
            {
                tl.LogMessage("Caught exception in CommandString ", e.Message);
                throw new ASCOM.DriverException("Rotator command failed.", e);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return temp;
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;

            if (connection == null)
                return;
            connection.Dispose();
            connection = null;
        }

        

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected Get", IsConnected.ToString());
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == IsConnected)
                    return;

                if (value)
                {
                    float posValue = 0;
                    bool setPos = false;
                    bool contHold = false;

                    if (connection != null && connection.IsConnected)
                        return;

                    using (ASCOM.Utilities.Profile p = new Profile())
                    {
                        p.DeviceType = "Rotator";
                        if (!p.IsRegistered(driverID))
                        {
                            p.Register(driverID, driverDescription);
                        }

                        transport = GetProfileValue(p, transportProfileName, transportDefault);
                        comPort = GetProfileValue(p, comPortLegacyProfileName, GetProfileValue(p, comPortProfileName, comPortDefault));
                        tcpHost = GetProfileValue(p, tcpHostProfileName, tcpHostDefault);
                        tcpPort = ParseInt(GetProfileValue(p, tcpPortProfileName, tcpPortDefault), 4030);
                        commandTimeoutMs = ParseInt(GetProfileValue(p, commandTimeoutProfileName, commandTimeoutDefault), 15000);
                        tcpPassword = GetProfileValue(p, tcpPasswordProfileName, tcpPasswordDefault);
                        setPos = GetProfileValue(p, "SetPos", "false").ToLowerInvariant().Equals("true");
                        contHold = GetProfileValue(p, "ContHold", "false").ToLowerInvariant().Equals("true");

                        if (setPos)
                            posValue = System.Convert.ToSingle(GetProfileValue(p, "Pos", "0"));
                        stepsPerDegree = ParseInt(GetProfileValue(p, "StepsPerDegree", "100"), 100);
                        maxSpeed = ParseInt(GetProfileValue(p, "MaxSpeed", "800"), 800);
                        acceleration = ParseInt(GetProfileValue(p, "Acceleration", "1000"), 1000);
                        tl.LogMessage("Steps per degree:", stepsPerDegree.ToString());
                        tl.LogMessage("MaxSpeed:", maxSpeed.ToString());
                        tl.LogMessage("Acceleration:", acceleration.ToString());
                        tl.LogMessage("stepSize", StepSize.ToString());

                        try
                        {
                            tl.LogMessage("Transport", transport);
                            tl.LogMessage("TcpHost", tcpHost);
                            tl.LogMessage("TcpPort", tcpPort.ToString());
                            tl.LogMessage("TcpPassword", string.IsNullOrEmpty(tcpPassword) ? "(empty)" : "(set, length=" + tcpPassword.Length + ")");
                            DebugLog("Connect", "Transport=" + transport + " Host=" + tcpHost + " Port=" + tcpPort + " HasPassword=" + !string.IsNullOrEmpty(tcpPassword));
                            
                            connection = CreateConnection();
                            tl.LogMessage("Connecting to rotator", connection.EndpointDescription);
                            DebugLog("Connect", "Connecting to " + connection.EndpointDescription);
                            connection.Connect();
                            tl.LogMessage("Connection", "SUCCESS");
                            DebugLog("Connect", "SUCCESS");
                            connectedState = true;
                            lastLink = true;

                            if (setPos)
                                CommandString("P " + Math.Round(posValue * stepsPerDegree + (360 * stepsPerDegree), 0).ToString() + "#", false);   // was + 36000 not 360*stepsperdegree

                            if (IsTcpTransport())
                                CommandString("D " + stepsPerDegree.ToString() + "#", false);

                            CommandString("A " + acceleration.ToString() + "#", false);
                            CommandString("X " + maxSpeed.ToString() + "#", false);

                            if (contHold)
                                CommandString("C 1#", false); //continuous hold on
                            else
                                CommandString("C 0#", false);

                            string ver = CommandString("V#", false);
                            string verTrim = ver.Replace('#', ' ');
                            string versn = verTrim.Replace('V', ' ').Trim();
                            tl.LogMessage("Firmware Version: ", versn.ToString());

                            // Verify this is a Rotator, not a Focuser
                            try
                            {
                                string devType = CommandString("T#", false);
                                string devTrim = devType.Replace('#', ' ').Trim();
                                if (devTrim != "T Rotator")
                                {
                                    throw new Exception("Device reports type '" + devTrim + "', expected 'T Rotator'. "
                                        + "This may be a focuser or unknown device. Check the firmware on your ESP8266.");
                                }
                                tl.LogMessage("Device type", "confirmed Rotator");
                            }
                            catch (Exception ex) when (!(ex is ASCOM.NotConnectedException))
                            {
                                tl.LogMessage("Device type check", ex.Message);
                                throw new ASCOM.NotConnectedException(
                                    "The device on " + comPort + " is NOT an ECAA Rotator. "
                                    + "It may be running focuser firmware. "
                                    + "Please flash the ECAA Rotator firmware (ESP8266RotatorFirmware.ino). "
                                    + "Detail: " + ex.Message);
                            }

                            // Read reversed direction state and homeOffsetSteps from firmware
                            try
                            {
                                string jsonStatus = CommandString("I#", false);
                                string jsonTrim = jsonStatus.Replace('#', ' ').Trim();
                                isReversed = jsonTrim.Contains("\"reversed\":true");
                                tl.LogMessage("Reverse state: ", isReversed.ToString());

                                // Parse homeOffsetSteps from JSON: "homeOffsetSteps":-1234
                                int idx = jsonTrim.IndexOf("\"homeOffsetSteps\":");
                                if (idx >= 0)
                                {
                                    idx += 19; // length of "homeOffsetSteps":
                                    int end = jsonTrim.IndexOfAny(new char[] { ',', ' ', '}' }, idx);
                                    if (end < 0) end = jsonTrim.Length;
                                    string val = jsonTrim.Substring(idx, end - idx);
                                    if (long.TryParse(val, out long parsed))
                                    {
                                        homeOffsetSteps = parsed;
                                        tl.LogMessage("HomeOffsetSteps: ", homeOffsetSteps.ToString());
                                    }
                                }
                            }
                            catch
                            {
                                isReversed = false;
                                homeOffsetSteps = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLog("Connect", "FAILED: " + ex.GetType().Name + " - " + ex.Message);
                            if (ex.InnerException != null)
                                DebugLog("Connect", "Inner: " + ex.InnerException.Message);
                            connectedState = false;
                            lastLink = false;
                            if (connection != null)
                            {
                                connection.Dispose();
                                connection = null;
                            }
                            throw new ASCOM.NotConnectedException("Rotator connection error", ex);
                        }
                    }
                }
                else
                {
                    try
                    {
                        CommandString("C 0#", false); //release the continuous hold
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("Disconnect hold release failed", ex.Message);
                    }
                    System.Threading.Thread.Sleep(500);
                    connectedState = false;
                    lastLink = false;
                    if (connection != null)
                    {
                        tl.LogMessage("Connected Set", "Disconnecting from " + connection.EndpointDescription);
                        connection.Dispose();
                        connection = null;
                    }
                }
            }
        }


        //  }
        //else
        //{
        //    connectedState = false;
        //    tl.LogMessage("Connected Set", "Disconnecting from port " + comPort);
        //    // TODO disconnect from the device
        //}
        //   }
        //  }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion; //
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                tl.LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        public string Name
        {
            get
            {
                string name = "ECAA ESP8266 Rotator";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IRotator Implementation

        private float targetPosition = 0; // Absolute stepper position of the rotator (in steps)  

        public bool CanReverse
        {
            get
            {
                tl.LogMessage("CanReverse Get", true.ToString());
                return true;
            }
        }

        public void Halt()
        {

            CommandString("S#", false);
            //  tl.LogMessage("Halt", "Not implemented");
            //  throw new ASCOM.MethodNotImplementedException("Halt");
        }

        public bool IsMoving
        {
            get
            {

                DoUpdate();
                return lastMoving;
                //tl.LogMessage("IsMoving Get", false.ToString()); // This rotator has instantaneous movement
                //return false;
            }
        }

        public bool Link
        {
            get
            {
                long now = DateTime.Now.Ticks;
                if (now - lastL > UPDATETICKS)
                {
                    if (connection != null)
                        lastLink = connection.IsConnected;

                    lastL = now;
                    return lastLink;
                }

                return lastLink;
            }
            set
            {
                this.Connected = value;
            }


            /*
            get
            {
                tl.LogMessage("Link Get", this.Connected.ToString());
                return this.Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
                tl.LogMessage("Link Set", value.ToString());
                this.Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
             */
        }


        public void Move(float pos)
        {
            lastUpdate = 0; // force Position refresh next read
            
            // float moveTo = rotatorPosition * stepsPerDegree + Position * stepsPerDegree;  // corrects for 100 steps per degree, need to replace with user defined variable.  
            double moveTo = StepperPos + RelativeAngleToMotorSteps(pos);  // current position in steps + number of steps needed to 
            targetPosition = pos;
            tl.LogMessage("Move called", $"relative={pos:F4}° stepperPos={StepperPos} moveTo={moveTo:F0}");
            if (moveTo >= 720 * stepsPerDegree) // was 72000
                moveTo -= 360 * stepsPerDegree;
            if (moveTo < 0)
                moveTo += 360 * stepsPerDegree;
            string resp = CommandString("M " + Math.Round(moveTo, 0) + "#", false);  // Position was 'int value' for focuser
            tl.LogMessage("Move response", resp);
            lastMoving = true;  //remd 1-12-15

            //  tl.LogMessage("Move", Position.ToString()); // Move by this amount
            //rotatorPosition += Position * stepsPerDegree;
            //rotatorPosition = (float)astroUtilities.Range(rotatorPosition, 0.0, true, 360.0, false); // Ensure value is in the range 0.0..359.9999...
        }

        //private float rotatorPosition // convert absolute step position to angle.  
        // {
        //    get  { return (lastPos - 9000) / 100 % 360; }
        //    set { rotatorPosition = value; }
        // }

        public double RelativeAngleToMotorSteps(float angle)
        {
            var targetSteps1 = angle % 360.00F * stepsPerDegree;
            return targetSteps1;
        }
        public double PositionAngleToMotorSteps(float targetPositionAngle)
        {
            // Convert target angle (0-360) to logical steps
            // Firmware formula: angle = (logicalSteps - centerSteps) / stepsPerDegree
            // So: logicalSteps = angle * stepsPerDegree + centerSteps
            var centerSteps = 200 * stepsPerDegree;
            
            // Normalize target angle to 0-360
            while (targetPositionAngle < 0) targetPositionAngle += 360.0F;
            while (targetPositionAngle >= 360.0F) targetPositionAngle -= 360.0F;
            
            // Calculate target logical steps
            var targetSteps = targetPositionAngle * stepsPerDegree + centerSteps;
            
            return targetSteps;
        }

        public void MoveAbsolute(float pos)
        {
            lastUpdate = 0; // force Position refresh next read
            var stepPosition = PositionAngleToMotorSteps(pos);
            targetPosition = pos;
         //   TargetPosition = pos;
            CommandString("M " + Math.Round(stepPosition, 0) + "#", false);  // Position was 'int value' for focuser  // corrects for 100 steps per degree, need to replace with user defined variable.  
            lastMoving = true;  //remd 1-12-15

            //     tl.LogMessage("MoveAbsolute", Position.ToString()); // Move to this position
            //rotatorPosition = Position * stepsPerDegree;
            //rotatorPosition = (float)astroUtilities.Range(rotatorPosition, 0.0, true, 360.0, false); // Ensure value is in the range 0.0..359.9999...
        }

        /// <summary>
        /// Sync the rotator's current mechanical position to the given sky angle.
        /// Called by NINA after plate-solving to align the rotator's reported position
        /// with the measured camera angle.
        /// </summary>
        /// <param name="position">Sky angle in degrees (0..360).</param>
        public void Sync(float position)
        {
            CheckConnected("Sync");
            // Convert sky angle to logical steps using the same offset formula as SetPos on connect.
            // logicalSteps = angle * stepsPerDegree + 360 * stepsPerDegree
            double logicalSteps = (double)position * stepsPerDegree + 360.0 * stepsPerDegree;
            CommandString("P " + Math.Round(logicalSteps, 0).ToString() + "#", false);
            targetPosition = position;
            tl.LogMessage("Sync", "Synced position to " + position.ToString("F2") + " degrees (" + Math.Round(logicalSteps, 0).ToString() + " logical steps)");

            // Refresh homeOffsetSteps from firmware so MechanicalPosition stays accurate
            try
            {
                string jsonStatus = CommandString("I#", false);
                string jsonTrim = jsonStatus.Replace('#', ' ').Trim();
                int idx = jsonTrim.IndexOf("\"homeOffsetSteps\":");
                if (idx >= 0)
                {
                    idx += 19;
                    int end = jsonTrim.IndexOfAny(new char[] { ',', ' ', '}' }, idx);
                    if (end < 0) end = jsonTrim.Length;
                    string val = jsonTrim.Substring(idx, end - idx);
                    if (long.TryParse(val, out long parsed))
                    {
                        homeOffsetSteps = parsed;
                        tl.LogMessage("Sync", "Updated homeOffsetSteps to " + homeOffsetSteps.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("Sync", "Failed to refresh homeOffsetSteps: " + ex.Message);
            }
        }

        // this is the stepper motor position in steps.  
        public float StepperPos
        {
            get
            {
                DoUpdate();
                return lastPos;

            }
        }



        public float Position
        {
            get
            {
                DoUpdate();
                // Angle = (logicalSteps - centerSteps) / stepsPerDegree
                // centerSteps = maxSteps/2 = 200 * stepsPerDegree (for 400-degree range)
                var centerSteps = 200 * stepsPerDegree;
                var pos = (lastPos - centerSteps) / stepsPerDegree;
                while (pos < 0)
                    pos += 360.0F;
                while (pos >= 360.0F)
                    pos -= 360.0F;
                return pos;


                //tl.LogMessage("Position Get", rotatorPosition.ToString()); // This rotator has instantaneous movement
                //return rotatorPosition;
            }
        }

        /// <summary>
        /// Raw mechanical position of the rotator in degrees (0..360).
        /// Computed from the physical step count so it reflects the actual
        /// stepper position, independent of the sky-angle sync offset.
        /// </summary>
        public float MechanicalPosition
        {
            get
            {
                DoUpdate();
                // lastPos is logical steps from G#.
                // Reconstruct physical steps = logical + homeOffsetSteps,
                // then convert to mechanical degrees (wrapped to 0..360).
                float mechanicalDeg = (lastPos + homeOffsetSteps) % (360.0F * stepsPerDegree) / stepsPerDegree;
                if (mechanicalDeg < 0) mechanicalDeg += 360.0F;
                return mechanicalDeg;
            }
        }

        public bool Reverse
        {
            get
            {
                tl.LogMessage("Reverse Get", isReversed.ToString());
                return isReversed;
            }
            set
            {
                CheckConnected("Reverse");
                string cmd = value ? "R 1#" : "R 0#";
                CommandString(cmd, false);
                isReversed = value;
                tl.LogMessage("Reverse Set", value.ToString());
            }
        }

        /// <summary>
        /// Motor max speed in steps per second (firmware setting).
        /// </summary>
        public int MaxSpeed
        {
            get
            {
                tl.LogMessage("MaxSpeed Get", maxSpeed.ToString());
                return maxSpeed;
            }
            set
            {
                CheckConnected("MaxSpeed");
                if (value <= 0) return;
                if (value > stepsPerDegree * 10) value = stepsPerDegree * 10;
                maxSpeed = value;
                CommandString("X " + value.ToString() + "#", false);
                tl.LogMessage("MaxSpeed Set", value.ToString());
            }
        }

        /// <summary>
        /// Motor acceleration in steps per second² (firmware setting).
        /// </summary>
        public int Acceleration
        {
            get
            {
                tl.LogMessage("Acceleration Get", acceleration.ToString());
                return acceleration;
            }
            set
            {
                CheckConnected("Acceleration");
                if (value <= 0) return;
                acceleration = value;
                CommandString("A " + value.ToString() + "#", false);
                tl.LogMessage("Acceleration Set", value.ToString());
            }
        }

        public float StepSize
        {
            get
            {
                if (stepsPerDegree > 100)  
                    return .01F;  // minimum of 0.01
                else
                    return 1F/stepsPerDegree ; // since carrying out 3 decimla points doesn't work mult by 10

                //tl.LogMessage("StepSize Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("StepSize", false);
            }
        }

        public float TargetPosition  
        {
            get
            {
                return targetPosition;
               // DoUpdate();
               //// tl.LogMessage("TargetPosition Get", Position.ToString()); // This rotator has instantaneous movement
               // return lastPos;
            }
            //set
            //{
            //    TargetPosition = value;
            //}
        }

        private void DoUpdate()
        {
            // only allow access for "gets" once per second.
            // if inside of 1 second the buffered value will be used.
            if (DateTime.Now.Ticks > UPDATETICKS + lastUpdate)
            {
                lastUpdate = DateTime.Now.Ticks;


                // focuser returns a string like:
                // m:false;s:1000;t:25.20$
                //   m - denotes moving or not
                //   s - denotes the position in steps
                //   t - denotes the temperature, always in C


                String val = CommandString("G#", false);


                // split the values up.  Ideally you should check for null here.  
                // if something goes wrong this will throw an exception...no bueno...


                //focuser sends P 200;M true#  for e.g.

                String[] vals = val.Replace('#', ' ').Trim().Split(';');

                string valTrim = vals[0].Replace('#', ' ');
                string pos = valTrim.Replace('P', ' ').Trim();
                tl.LogMessage("DoUpdate", $"G response raw='{val}' logicalSteps={pos}");
                // these values are used in the "Get" calls.  That way the client gets an immediate
                // response.  However it may up to 1 second out of date.
                // Thus "lastMoving" must be set to true when the move is initiated in "Move"

                lastPos = Convert.ToSingle(pos);  // raw stepper position in 'steps' from 0 (which is -90 degrees) 
                //    lastMoving = false;
                lastMoving = vals[1].Substring(2) == "true" ? true : false;  //*** remd 1-12-15
                //   *** 1-12-15  to implement this need to change arduino code to retrun something liek "M:True" 
                //   *** like example above line 640, then slipt ther string into an array and decifer them



                //    lastPos = Convert.ToInt16(vals[1].Substring(2));
                //    lastTemp = Convert.ToDouble(vals[2].Substring(2));
            }
        }



        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        private bool IsTcpTransport()
        {
            return string.Equals(transport, "TCP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(transport, "WiFi TCP", StringComparison.OrdinalIgnoreCase);
        }

        private IRotatorConnection CreateConnection()
        {
            if (IsTcpTransport())
            {
                if (string.IsNullOrWhiteSpace(tcpHost))
                {
                    throw new ASCOM.NotConnectedException("No TCP host selected");
                }

                return new TcpRotatorConnection(tcpHost, tcpPort, commandTimeoutMs, tcpPassword);
            }

            if (string.IsNullOrWhiteSpace(comPort) || comPort == "COM1")
            {
                string detected = DetectRotatorPort();
                if (!string.IsNullOrWhiteSpace(detected))
                {
                    tl.LogMessage("COM port auto-detected", detected);
                    comPort = detected;
                }
            }

            if (string.IsNullOrWhiteSpace(comPort))
            {
                throw new ASCOM.NotConnectedException("No COM port selected and auto-detection failed. "
                    + "Please open Setup, select the correct COM port, or use the Detect button.");
            }

            return new SerialRotatorConnection(comPort);
        }

        /// <summary>
        /// Scan available COM ports for an ECAA Rotator device.
        /// </summary>
        private static string DetectRotatorPort()
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                try
                {
                    using (var sp = new System.IO.Ports.SerialPort(port, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One))
                    {
                        sp.ReadTimeout = 2000;
                        sp.WriteTimeout = 2000;
                        sp.DtrEnable = true;
                        sp.RtsEnable = true;
                        sp.Open();
                        System.Threading.Thread.Sleep(3000);
                        sp.DiscardInBuffer();
                        sp.DiscardOutBuffer();
                        sp.WriteLine("T#");
                        System.Threading.Thread.Sleep(300);
                        string response = sp.ReadExisting();
                        if (!string.IsNullOrEmpty(response) && response.Contains("T Rotator"))
                        {
                            return port;
                        }
                    }
                }
                catch
                {
                    // Port unavailable — skip
                }
            }
            return null;
        }

        internal static int ParseInt(string value, int defaultValue)
        {
            int parsed;
            if (int.TryParse(value, out parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        internal static string GetProfileValue(Profile profile, string name, string defaultValue)
        {
            string value = profile.GetValue(driverID, name, string.Empty, defaultValue);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value;
        }

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Rotator";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Rotator";
                traceState = Convert.ToBoolean(GetProfileValue(driverProfile, traceStateProfileName, traceStateDefault));
                comPort = GetProfileValue(driverProfile, comPortLegacyProfileName, GetProfileValue(driverProfile, comPortProfileName, comPortDefault));
                transport = GetProfileValue(driverProfile, transportProfileName, transportDefault);
                tcpHost = GetProfileValue(driverProfile, tcpHostProfileName, tcpHostDefault);
                tcpPort = ParseInt(GetProfileValue(driverProfile, tcpPortProfileName, tcpPortDefault), 4030);
                commandTimeoutMs = ParseInt(GetProfileValue(driverProfile, commandTimeoutProfileName, commandTimeoutDefault), 15000);
                tcpPassword = GetProfileValue(driverProfile, tcpPasswordProfileName, tcpPasswordDefault);
                stepsPerDegree = ParseInt(GetProfileValue(driverProfile, "StepsPerDegree", "100"), 100);
                maxSpeed = ParseInt(GetProfileValue(driverProfile, "MaxSpeed", "800"), 800);
                acceleration = ParseInt(GetProfileValue(driverProfile, "Acceleration", "1000"), 1000);
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Rotator";
                driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString());
                driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, comPortLegacyProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, transportProfileName, transport.ToString());
                driverProfile.WriteValue(driverID, tcpHostProfileName, tcpHost.ToString());
                driverProfile.WriteValue(driverID, tcpPortProfileName, tcpPort.ToString());
                driverProfile.WriteValue(driverID, commandTimeoutProfileName, commandTimeoutMs.ToString());
                driverProfile.WriteValue(driverID, tcpPasswordProfileName, tcpPassword ?? "");
                driverProfile.WriteValue(driverID, "StepsPerDegree", stepsPerDegree.ToString());
                driverProfile.WriteValue(driverID, "MaxSpeed", maxSpeed.ToString());
                driverProfile.WriteValue(driverID, "Acceleration", acceleration.ToString());
            }
        }

        #endregion

    }
}
