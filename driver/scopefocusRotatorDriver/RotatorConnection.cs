using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using ASCOM.Utilities;

namespace ASCOM.scopefocus
{
    internal static class ConnectionLog
    {
        private static string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ASCOM", "Logs", "ECAA-Rotator-Debug.log");

        public static void Write(string method, string message)
        {
            try
            {
                string dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + method + "] " + message;
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }

    internal interface IRotatorConnection : IDisposable
    {
        bool IsConnected { get; }
        string EndpointDescription { get; }
        void Connect();
        void Disconnect();
        string CommandString(string command);
    }

    internal sealed class SerialRotatorConnection : IRotatorConnection
    {
        private readonly string portName;
        private System.IO.Ports.SerialPort serialPort;

        public SerialRotatorConnection(string portName)
        {
            this.portName = portName;
        }

        public bool IsConnected
        {
            get { return serialPort != null && serialPort.IsOpen; }
        }

        public string EndpointDescription
        {
            get { return portName; }
        }

        public void Connect()
        {
            serialPort = new System.IO.Ports.SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;
            serialPort.ReadTimeout = 3000;
            serialPort.WriteTimeout = 3000;
            serialPort.NewLine = "#";
            serialPort.Open();

            // ESP8266 takes ~2-3 s to boot with WiFi init; wait 4 s for safety,
            // then drain any boot banner.
            System.Threading.Thread.Sleep(4000);
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
        }

        public void Disconnect()
        {
            if (serialPort == null) return;
            try { serialPort.Close(); } catch { }
            try { serialPort.Dispose(); } catch { }
            serialPort = null;
        }

        public string CommandString(string command)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Serial rotator connection is closed.");

            if (!command.EndsWith("#"))
                command += "#";

            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            serialPort.Write(command);
            string response = serialPort.ReadLine();
            return response;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    internal sealed class TcpRotatorConnection : IRotatorConnection
    {
        private readonly string host;
        private readonly int port;
        private readonly int timeoutMs;
        private readonly string password;
        private TcpClient client;
        private NetworkStream stream;

        public TcpRotatorConnection(string host, int port, int timeoutMs, string password)
        {
            this.host = host;
            this.port = port;
            this.timeoutMs = timeoutMs;
            this.password = password ?? "";
        }

        public bool IsConnected
        {
            get { return client != null && client.Connected; }
        }

        public string EndpointDescription
        {
            get { return host + ":" + port.ToString(); }
        }

        public void Connect()
        {
            ConnectionLog.Write("TCP.Connect", "Connecting to " + host + ":" + port + " timeout=" + timeoutMs + "ms");
            
            client = new TcpClient();
            client.NoDelay = true;
            client.SendTimeout = timeoutMs;
            client.ReceiveTimeout = timeoutMs;
            
            ConnectionLog.Write("TCP.Connect", "Calling TcpClient.Connect...");
            try
            {
                client.Connect(host, port);
            }
            catch (Exception ex)
            {
                ConnectionLog.Write("TCP.Connect", "Connect FAILED: " + ex.GetType().Name + " - " + ex.Message);
                throw;
            }
            ConnectionLog.Write("TCP.Connect", "TCP connected successfully");
            
            stream = client.GetStream();
            stream.ReadTimeout = timeoutMs;
            stream.WriteTimeout = timeoutMs;
            ConnectionLog.Write("TCP.Connect", "Stream ready");

            if (!string.IsNullOrEmpty(password))
            {
                ConnectionLog.Write("TCP.Connect", "Sending auth command...");
                string authResponse = CommandString("K " + password);
                ConnectionLog.Write("TCP.Connect", "Auth response: " + authResponse);
                if (!authResponse.StartsWith("K ok"))
                {
                    ConnectionLog.Write("TCP.Connect", "Auth FAILED, disconnecting");
                    Disconnect();
                    throw new IOException("TCP authentication failed. Response: " + authResponse);
                }
                ConnectionLog.Write("TCP.Connect", "Auth OK");
            }
            else
            {
                ConnectionLog.Write("TCP.Connect", "No password set, skipping auth");
            }
        }

        public void Disconnect()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }
        }

        public string CommandString(string command)
        {
            if (!IsConnected || stream == null)
            {
                throw new InvalidOperationException("TCP rotator connection is closed.");
            }

            if (!command.EndsWith("#"))
            {
                command += "#";
            }

            byte[] request = Encoding.ASCII.GetBytes(command);
            stream.Write(request, 0, request.Length);
            stream.Flush();

            StringBuilder response = new StringBuilder();
            byte[] buffer = new byte[1];
            while (true)
            {
                int read = stream.Read(buffer, 0, 1);
                if (read <= 0)
                {
                    throw new IOException("TCP rotator connection closed before command response.");
                }

                char c = (char)buffer[0];
                response.Append(c);
                if (c == '#')
                {
                    return response.ToString();
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
