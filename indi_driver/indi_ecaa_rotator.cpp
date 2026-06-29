/*============================================================================
 * INDI Rotator driver for CAA — TCP + Serial, ESP8266 + Arduino Nano.
 *============================================================================*/

#include "indi_ecaa_rotator.h"

#include <cmath>
#include <cstring>
#include <sstream>
#include <algorithm>

// --- POSIX / Win32 socket & serial ---
#ifdef _WIN32
  #include <winsock2.h>
  #include <ws2tcpip.h>
  #include <windows.h>
  #pragma comment(lib, "ws2_32.lib")
  static bool wsaReady = false;
#else
  #include <unistd.h>
  #include <sys/socket.h>
  #include <netinet/in.h>
  #include <arpa/inet.h>
  #include <netdb.h>
  #include <fcntl.h>
  #include <errno.h>
  #include <termios.h>
  using SOCKET = int;
  #define INVALID_SOCKET (-1)
  #define SOCKET_ERROR   (-1)
  #define closesocket    close
#endif

// ===================================================================
//  TCP connection
// ===================================================================

TcpConnection::TcpConnection() : sock(INVALID_SOCKET), port_(0) {}
TcpConnection::~TcpConnection() { close(); }

bool TcpConnection::isOpen() const { return sock != INVALID_SOCKET; }

bool TcpConnection::open(const std::string &hostPort)
{
    close();

    auto colon = hostPort.find(':');
    host = hostPort.substr(0, colon);
    port_ = (colon != std::string::npos)
                ? std::stoi(hostPort.substr(colon + 1))
                : 4030;

#ifdef _WIN32
    if (!wsaReady) { WSADATA d; WSAStartup(MAKEWORD(2,2), &d); wsaReady = true; }
#endif

    sock = (int)::socket(AF_INET, SOCK_STREAM, 0);
    if (sock == INVALID_SOCKET) return false;

    struct sockaddr_in addr = {};
    addr.sin_family = AF_INET;
    addr.sin_port   = htons((uint16_t)port_);

    if (inet_pton(AF_INET, host.c_str(), &addr.sin_addr) != 1)
    {
        struct hostent *he = gethostbyname(host.c_str());
        if (!he) { closesocket(sock); sock = INVALID_SOCKET; return false; }
        memcpy(&addr.sin_addr, he->h_addr_list[0], he->h_length);
    }

    // Non-blocking connect with 3 s timeout
#ifdef _WIN32
    u_long m = 1; ioctlsocket(sock, FIONBIO, &m);
#else
    fcntl(sock, F_SETFL, O_NONBLOCK);
#endif

    ::connect(sock, (struct sockaddr*)&addr, sizeof(addr));
    fd_set fds; FD_ZERO(&fds); FD_SET(sock, &fds);
    struct timeval tv = {3, 0};
    if (select(sock + 1, nullptr, &fds, nullptr, &tv) <= 0)
    {
        closesocket(sock); sock = INVALID_SOCKET; return false;
    }

    // Back to blocking
#ifdef _WIN32
    m = 0; ioctlsocket(sock, FIONBIO, &m);
#else
    int fl = fcntl(sock, F_GETFL, 0);
    fcntl(sock, F_SETFL, fl & ~O_NONBLOCK);
#endif

    // Quick drain in case firmware sent a banner
    char dummy[64];
    tv = {0, 100000};  // 100 ms
    FD_ZERO(&fds); FD_SET(sock, &fds);
    while (select(sock + 1, &fds, nullptr, nullptr, &tv) > 0)
    {
        ::recv(sock, dummy, sizeof(dummy), 0);
        FD_ZERO(&fds); FD_SET(sock, &fds);
        tv = {0, 0};
    }

    return true;
}

void TcpConnection::close()
{
    if (sock != INVALID_SOCKET) { closesocket(sock); sock = INVALID_SOCKET; }
    host.clear();
    port_ = 0;
}

bool TcpConnection::send(const std::string &data)
{
    std::string framed = data;
    if (framed.back() != '#') framed += '#';
    const char *p  = framed.c_str();
    int         len = (int)framed.size();
    while (len > 0)
    {
        int n = (int)::send(sock, p, len, 0);
        if (n <= 0) return false;
        p   += n;
        len -= n;
    }
    return true;
}

bool TcpConnection::recv(std::string &response, int timeoutMs)
{
    response.clear();
    fd_set fds; struct timeval tv; char ch;
    while (true)
    {
        FD_ZERO(&fds); FD_SET(sock, &fds);
        tv.tv_sec  = timeoutMs / 1000;
        tv.tv_usec = (timeoutMs % 1000) * 1000;
        int ret = select(sock + 1, &fds, nullptr, nullptr, &tv);
        if (ret <= 0) return false;
        if (::recv(sock, &ch, 1, 0) <= 0) return false;
        response += ch;
        if (ch == '#') break;
    }
    return true;
}

std::string TcpConnection::endpoint() const
{
    return host + ":" + std::to_string(port_);
}

// ===================================================================
//  Serial connection — POSIX termios + Win32 CreateFile
// ===================================================================

SerialConnection::SerialConnection() :
#ifdef _WIN32
    hSerial(nullptr),
#else
    fd(-1),
#endif
    baudRate(9600) {}

SerialConnection::~SerialConnection() { close(); }

bool SerialConnection::isOpen() const
{
#ifdef _WIN32
    return hSerial != nullptr && hSerial != INVALID_HANDLE_VALUE;
#else
    return fd != -1;
#endif
}

bool SerialConnection::open(const std::string &portAndBaud)
{
    close();

    auto colon = portAndBaud.find(':');
    portName  = portAndBaud.substr(0, colon);
    baudRate  = (colon != std::string::npos)
                    ? std::stoi(portAndBaud.substr(colon + 1))
                    : 9600;

#ifdef _WIN32
    // Windows: "\\.\COM3" naming
    std::string winPort = R"(\\.\)" + portName;
    hSerial = CreateFileA(winPort.c_str(),
                          GENERIC_READ | GENERIC_WRITE,
                          0, nullptr, OPEN_EXISTING,
                          FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hSerial == INVALID_HANDLE_VALUE)
    {
        hSerial = nullptr;
        LOGF_ERROR("Serial: cannot open %s (error %lu)", portName.c_str(), GetLastError());
        return false;
    }

    DCB dcb = {};
    dcb.DCBlength = sizeof(dcb);
    if (!GetCommState(hSerial, &dcb))
    {
        CloseHandle(hSerial); hSerial = nullptr; return false;
    }
    dcb.BaudRate = (DWORD)baudRate;
    dcb.ByteSize = 8;
    dcb.Parity   = NOPARITY;
    dcb.StopBits = ONESTOPBIT;
    dcb.fBinary  = TRUE;
    dcb.fDtrControl = DTR_CONTROL_DISABLE;
    dcb.fRtsControl = RTS_CONTROL_DISABLE;
    dcb.fOutxCtsFlow = FALSE;
    dcb.fOutxDsrFlow = FALSE;
    dcb.fDsrSensitivity = FALSE;
    dcb.fOutX = FALSE;
    dcb.fInX  = FALSE;
    dcb.fNull = FALSE;
    dcb.fAbortOnError = FALSE;
    if (!SetCommState(hSerial, &dcb))
    {
        CloseHandle(hSerial); hSerial = nullptr; return false;
    }

    COMMTIMEOUTS cto = {};
    cto.ReadIntervalTimeout        = 50;
    cto.ReadTotalTimeoutMultiplier  = 0;
    cto.ReadTotalTimeoutConstant    = 0;
    cto.WriteTotalTimeoutMultiplier = 0;
    cto.WriteTotalTimeoutConstant   = 0;
    SetCommTimeouts(hSerial, &cto);

    PurgeComm(hSerial, PURGE_RXCLEAR | PURGE_TXCLEAR);
    return true;

#else
    // POSIX: open + termios
    fd = ::open(portName.c_str(), O_RDWR | O_NOCTTY | O_SYNC);
    if (fd < 0) return false;

    struct termios tty;
    if (tcgetattr(fd, &tty) != 0) { ::close(fd); fd = -1; return false; }

    speed_t speed = B9600;
    switch (baudRate)
    {
        case 2400:   speed = B2400;   break;
        case 4800:   speed = B4800;   break;
        case 9600:   speed = B9600;   break;
        case 19200:  speed = B19200;  break;
        case 38400:  speed = B38400;  break;
        case 57600:  speed = B57600;  break;
        case 115200: speed = B115200; break;
        default:     speed = B9600;   break;
    }
    cfsetospeed(&tty, speed);
    cfsetispeed(&tty, speed);

    tty.c_cflag = (tty.c_cflag & ~CSIZE) | CS8 | CLOCAL | CREAD;
    tty.c_iflag &= ~(IXON | IXOFF | IXANY | ICRNL);
    tty.c_oflag &= ~OPOST;
    tty.c_lflag &= ~(ICANON | ECHO | ECHOE | ISIG);
    tty.c_cc[VMIN]  = 0;
    tty.c_cc[VTIME] = 5;

    tcflush(fd, TCIOFLUSH);
    if (tcsetattr(fd, TCSANOW, &tty) != 0) { ::close(fd); fd = -1; return false; }
    return true;
#endif
}

void SerialConnection::close()
{
#ifdef _WIN32
    if (hSerial && hSerial != INVALID_HANDLE_VALUE)
    {
        PurgeComm(hSerial, PURGE_RXCLEAR | PURGE_TXCLEAR);
        CloseHandle(hSerial);
        hSerial = nullptr;
    }
#else
    if (fd != -1) { ::close(fd); fd = -1; }
#endif
    portName.clear();
}

bool SerialConnection::send(const std::string &data)
{
    std::string framed = data;
    if (framed.back() != '#') framed += '#';

    if (!isOpen()) return false;

#ifdef _WIN32
    DWORD written = 0;
    if (!WriteFile(hSerial, framed.c_str(), (DWORD)framed.size(), &written, nullptr))
        return false;
    return written == (DWORD)framed.size();
#else
    int n = (int)::write(fd, framed.c_str(), framed.size());
    return n == (int)framed.size();
#endif
}

bool SerialConnection::recv(std::string &response, int timeoutMs)
{
    response.clear();
    if (!isOpen()) return false;

#ifdef _WIN32
    char ch;
    DWORD read;
    COMMTIMEOUTS ctoSave, ctoTmp;
    GetCommTimeouts(hSerial, &ctoSave);

    // Per-char timeout: read one byte with overall deadline
    DWORD deadline = GetTickCount() + (DWORD)timeoutMs;
    while (true)
    {
        ctoTmp.ReadIntervalTimeout        = MAXDWORD;
        ctoTmp.ReadTotalTimeoutMultiplier  = 0;
        ctoTmp.ReadTotalTimeoutConstant    = 0;
        ctoTmp.WriteTotalTimeoutMultiplier = 0;
        ctoTmp.WriteTotalTimeoutConstant   = 0;

        // Remaining time
        DWORD now = GetTickCount();
        if (now >= deadline) { SetCommTimeouts(hSerial, &ctoSave); return false; }
        DWORD remainMs = deadline - now;
        ctoTmp.ReadTotalTimeoutConstant = remainMs;
        SetCommTimeouts(hSerial, &ctoTmp);

        if (!ReadFile(hSerial, &ch, 1, &read, nullptr) || read == 0)
        {
            SetCommTimeouts(hSerial, &ctoSave);
            return false;
        }
        response += ch;
        if (ch == '#') break;
    }
    SetCommTimeouts(hSerial, &ctoSave);
    return true;
#else
    fd_set fds; struct timeval tv; char ch;
    while (true)
    {
        FD_ZERO(&fds); FD_SET(fd, &fds);
        tv.tv_sec  = timeoutMs / 1000;
        tv.tv_usec = (timeoutMs % 1000) * 1000;
        if (select(fd + 1, &fds, nullptr, nullptr, &tv) <= 0) return false;
        if (::read(fd, &ch, 1) <= 0) return false;
        response += ch;
        if (ch == '#') break;
    }
    return true;
#endif
}

std::string SerialConnection::endpoint() const
{
    return portName + " @ " + std::to_string(baudRate);
}

// ===================================================================
//  CAA Rotator driver
// ===================================================================

constexpr const char *DRIVER_NAME    = "CAA Rotator";
constexpr const char *DRIVER_VERSION = "1.0.0";
constexpr const char *DRIVER_INFO    = "INDI Rotator Driver for CAA (ESP8266 / Arduino Nano).";

constexpr const char *DEFAULT_TCP_HOST   = "192.168.4.1";
constexpr int  DEFAULT_TCP_PORT          = 4030;
constexpr const char *DEFAULT_SERIAL_PORT = "/dev/ttyUSB0";
constexpr int  DEFAULT_SERIAL_BAUD       = 9600;
constexpr int  DEFAULT_TIMEOUT_MS        = 3000;
constexpr int  DEFAULT_STEPS_PER_DEGREE  = 635;
constexpr int  DEFAULT_MAX_SPEED         = 800;
constexpr int  DEFAULT_ACCELERATION      = 1000;
constexpr int  POLL_MS                   = 500;

// ---------------------------------------------------------------------------
CaaRotator::CaaRotator()
    : transport(TRANSPORT_TCP)
    , tcpHost(DEFAULT_TCP_HOST)
    , tcpPort(DEFAULT_TCP_PORT)
    , serialPort(DEFAULT_SERIAL_PORT)
    , serialBaud(DEFAULT_SERIAL_BAUD)
    , commandTimeoutMs(DEFAULT_TIMEOUT_MS)
    , stepsPerDegree(DEFAULT_STEPS_PER_DEGREE)
    , maxSpeed(DEFAULT_MAX_SPEED)
    , acceleration(DEFAULT_ACCELERATION)
    , currentLogicalSteps(0)
    , moving(false)
    , reversed(false)
{
    setVersion(1, 0);
    setDriverInterface(ROTATOR_INTERFACE);

    SetRotatorCapability(ROTATOR_CAN_ABORT | ROTATOR_CAN_SYNC |
                         ROTATOR_CAN_GOTO  | ROTATOR_CAN_HOME, 0);
}

const char *CaaRotator::getDefaultName() { return DRIVER_NAME; }

// ---------------------------------------------------------------------------
// initProperties
// ---------------------------------------------------------------------------
bool CaaRotator::initProperties()
{
    INDI::Rotator::initProperties();

    // --- Transport switch ---
    TransportSP[0].fill("TCP",    "TCP",    ISS_ON);
    TransportSP[1].fill("SERIAL", "Serial", ISS_OFF);
    TransportSP.fill(getDeviceName(), "TRANSPORT", "Transport");
    TransportSP.defineProperty();

    // --- TCP config ---
    TcpHostTP[0].fill("HOST", "Host", DEFAULT_TCP_HOST);
    TcpHostTP.fill(getDeviceName(), "TCP_HOST", "TCP Host");
    TcpHostTP.defineProperty();

    TcpPortNP[0].fill("PORT", "Port", "%.0f", 1, 65535, 1, DEFAULT_TCP_PORT);
    TcpPortNP.fill(getDeviceName(), "TCP_PORT", "TCP Port");
    TcpPortNP.defineProperty();

    // --- Serial config ---
    SerialPortTP[0].fill("PORT", "Port", DEFAULT_SERIAL_PORT);
    SerialPortTP.fill(getDeviceName(), "SERIAL_PORT", "Serial Port");
    SerialPortTP.defineProperty();

    SerialBaudNP[0].fill("BAUD", "Baud", "%.0f", 300, 921600, 100, DEFAULT_SERIAL_BAUD);
    SerialBaudNP.fill(getDeviceName(), "SERIAL_BAUD", "Serial Baud");
    SerialBaudNP.defineProperty();

    // --- Steps per degree ---
    StepsPerDegreeNP[0].fill("SPD", "Steps/°", "%.1f",
                             1.0, 10000.0, 0.1, DEFAULT_STEPS_PER_DEGREE);
    StepsPerDegreeNP.fill(getDeviceName(), "STEPS_PER_DEGREE", "Steps per Degree");
    StepsPerDegreeNP.defineProperty();

    // --- Max speed ---
    MaxSpeedNP[0].fill("SPD", "Max Speed", "%.0f",
                        1.0, 50000.0, 10.0, DEFAULT_MAX_SPEED);
    MaxSpeedNP.fill(getDeviceName(), "MAX_SPEED", "Max Speed (steps/s)");
    MaxSpeedNP.defineProperty();

    // --- Acceleration ---
    AccelerationNP[0].fill("ACC", "Acceleration", "%.0f",
                           1.0, 100000.0, 10.0, DEFAULT_ACCELERATION);
    AccelerationNP.fill(getDeviceName(), "ACCELERATION", "Accel (steps/s²)");
    AccelerationNP.defineProperty();

    // --- Command timeout ---
    CommandTimeoutNP[0].fill("TIMEOUT", "Timeout (ms)", "%.0f",
                             500.0, 30000.0, 100.0, DEFAULT_TIMEOUT_MS);
    CommandTimeoutNP.fill(getDeviceName(), "CMD_TIMEOUT", "Command Timeout");
    CommandTimeoutNP.defineProperty();

    // --- Firmware version (read-only) ---
    FirmwareVersionTP[0].fill("FW_VER", "Version", "--");
    FirmwareVersionTP.fill(getDeviceName(), "FW_VERSION", "Firmware");
    FirmwareVersionTP.defineProperty();

    // --- Reverse ---
    ReverseSP[0].fill("REV", "Reverse", ISS_OFF);
    ReverseSP.fill(getDeviceName(), "REVERSE", "Direction");
    ReverseSP.defineProperty();

    // --- Sync trigger ---
    SyncAngleNP[0].fill("SYNC_ANGLE", "Angle (°)", "%.3f",
                        0.0, 360.0, 0.01, 0.0);
    SyncAngleNP.fill(getDeviceName(), "SYNC_ANGLE_TARGET", "Sync Angle");
    SyncAngleNP.defineProperty();

    // Restore saved config
    loadConfig(true, "TRANSPORT");
    loadConfig(true, "TCP_HOST");
    loadConfig(true, "TCP_PORT");
    loadConfig(true, "SERIAL_PORT");
    loadConfig(true, "SERIAL_BAUD");
    loadConfig(true, "STEPS_PER_DEGREE");
    loadConfig(true, "MAX_SPEED");
    loadConfig(true, "ACCELERATION");
    loadConfig(true, "CMD_TIMEOUT");

    // Determine which transport is active from saved state
    if (TransportSP[1].getState() == ISS_ON)
        transport = TRANSPORT_SERIAL;

    // Show/hide transport-specific props
    if (transport == TRANSPORT_SERIAL)
    {
        TcpHostTP.deleteProperty();
        TcpPortNP.deleteProperty();
    }
    else
    {
        SerialPortTP.deleteProperty();
        SerialBaudNP.deleteProperty();
    }

    addPollPeriodControl(POLL_MS);
    LOG_INFO("CAA Rotator: initProperties done");
    return true;
}

// ---------------------------------------------------------------------------
// updateProperties
// ---------------------------------------------------------------------------
bool CaaRotator::updateProperties()
{
    INDI::Rotator::updateProperties();

    if (isConnected())
    {
        FirmwareVersionTP.defineProperty();
        SyncAngleNP.defineProperty();
        ReverseSP.defineProperty();
        MaxSpeedNP.defineProperty();
        AccelerationNP.defineProperty();
        SetTimer(getCurrentPollingPeriod());
    }
    else
    {
        FirmwareVersionTP.deleteProperty();
        SyncAngleNP.deleteProperty();
        ReverseSP.deleteProperty();
        MaxSpeedNP.deleteProperty();
        AccelerationNP.deleteProperty();
    }
    return true;
}

void CaaRotator::ISGetProperties(const char *dev)
{
    INDI::Rotator::ISGetProperties(dev);
}

// ---------------------------------------------------------------------------
// ISNewSwitch
// ---------------------------------------------------------------------------
bool CaaRotator::ISNewSwitch(const char *dev, const char *name,
                             ISState *states, char *names[], int n)
{
    if (dev && !strcmp(dev, getDeviceName()))
    {
        if (TransportSP.isNameMatch(name))
        {
            TransportSP.update(states, names, n);
            transport = (TransportSP[0].getState() == ISS_ON)
                            ? TRANSPORT_TCP : TRANSPORT_SERIAL;

            // Toggle visibility
            if (transport == TRANSPORT_TCP)
            {
                SerialPortTP.deleteProperty();
                SerialBaudNP.deleteProperty();
                TcpHostTP.defineProperty();
                TcpPortNP.defineProperty();
            }
            else
            {
                TcpHostTP.deleteProperty();
                TcpPortNP.deleteProperty();
                SerialPortTP.defineProperty();
                SerialBaudNP.defineProperty();
            }

            TransportSP.setState(IPS_OK);
            TransportSP.apply();
            saveConfig(true, "TRANSPORT");

            LOGF_INFO("Transport: %s", (transport == TRANSPORT_TCP) ? "TCP" : "Serial");
            return true;
        }

        if (ReverseSP.isNameMatch(name))
        {
            ReverseSP.update(states, names, n);
            reversed = (ReverseSP[0].getState() == ISS_ON);
            if (isConnected() && connection && connection->isOpen())
            {
                std::string resp;
                sendCmd(reversed ? "R 1" : "R 0");
                readResp(resp);
            }
            ReverseSP.setState(IPS_OK);
            ReverseSP.apply();
            LOGF_INFO("Reverse: %s", reversed ? "ON" : "OFF");
            return true;
        }
    }
    return INDI::Rotator::ISNewSwitch(dev, name, states, names, n);
}

// ---------------------------------------------------------------------------
// ISNewText
// ---------------------------------------------------------------------------
bool CaaRotator::ISNewText(const char *dev, const char *name,
                           char *texts[], char *names[], int n)
{
    if (dev && !strcmp(dev, getDeviceName()))
    {
        if (TcpHostTP.isNameMatch(name))
        {
            TcpHostTP.update(texts, names, n);
            tcpHost = TcpHostTP[0].getText();
            TcpHostTP.setState(IPS_OK);
            TcpHostTP.apply();
            saveConfig(true, "TCP_HOST");
            LOGF_INFO("TCP host: %s", tcpHost.c_str());
            return true;
        }

        if (SerialPortTP.isNameMatch(name))
        {
            SerialPortTP.update(texts, names, n);
            serialPort = SerialPortTP[0].getText();
            SerialPortTP.setState(IPS_OK);
            SerialPortTP.apply();
            saveConfig(true, "SERIAL_PORT");
            LOGF_INFO("Serial port: %s", serialPort.c_str());
            return true;
        }
    }
    return INDI::Rotator::ISNewText(dev, name, texts, names, n);
}

// ---------------------------------------------------------------------------
// ISNewNumber
// ---------------------------------------------------------------------------
bool CaaRotator::ISNewNumber(const char *dev, const char *name,
                             double values[], char *names[], int n)
{
    if (dev && !strcmp(dev, getDeviceName()))
    {
        if (TcpPortNP.isNameMatch(name))
        {
            TcpPortNP.update(values, names, n);
            tcpPort = (int)TcpPortNP[0].getValue();
            TcpPortNP.setState(IPS_OK);
            TcpPortNP.apply();
            saveConfig(true, "TCP_PORT");
            LOGF_INFO("TCP port: %d", tcpPort);
            return true;
        }

        if (SerialBaudNP.isNameMatch(name))
        {
            SerialBaudNP.update(values, names, n);
            serialBaud = (int)SerialBaudNP[0].getValue();
            SerialBaudNP.setState(IPS_OK);
            SerialBaudNP.apply();
            saveConfig(true, "SERIAL_BAUD");
            LOGF_INFO("Serial baud: %d", serialBaud);
            return true;
        }

        if (StepsPerDegreeNP.isNameMatch(name))
        {
            StepsPerDegreeNP.update(values, names, n);
            stepsPerDegree = StepsPerDegreeNP[0].getValue();
            StepsPerDegreeNP.setState(IPS_OK);
            StepsPerDegreeNP.apply();
            saveConfig(true, "STEPS_PER_DEGREE");
            LOGF_INFO("Steps/°: %.1f", stepsPerDegree);
            return true;
        }

        if (MaxSpeedNP.isNameMatch(name))
        {
            MaxSpeedNP.update(values, names, n);
            maxSpeed = (int)MaxSpeedNP[0].getValue();
            if (isConnected() && connection && connection->isOpen())
            {
                std::string resp;
                sendCmd("X " + std::to_string(maxSpeed));
                readResp(resp);
            }
            MaxSpeedNP.setState(IPS_OK);
            MaxSpeedNP.apply();
            saveConfig(true, "MAX_SPEED");
            LOGF_INFO("Max speed: %d", maxSpeed);
            return true;
        }

        if (AccelerationNP.isNameMatch(name))
        {
            AccelerationNP.update(values, names, n);
            acceleration = (int)AccelerationNP[0].getValue();
            if (isConnected() && connection && connection->isOpen())
            {
                std::string resp;
                sendCmd("A " + std::to_string(acceleration));
                readResp(resp);
            }
            AccelerationNP.setState(IPS_OK);
            AccelerationNP.apply();
            saveConfig(true, "ACCELERATION");
            LOGF_INFO("Acceleration: %d", acceleration);
            return true;
        }

        if (CommandTimeoutNP.isNameMatch(name))
        {
            CommandTimeoutNP.update(values, names, n);
            commandTimeoutMs = (int)CommandTimeoutNP[0].getValue();
            CommandTimeoutNP.setState(IPS_OK);
            CommandTimeoutNP.apply();
            saveConfig(true, "CMD_TIMEOUT");
            LOGF_INFO("Timeout: %d ms", commandTimeoutMs);
            return true;
        }

        if (SyncAngleNP.isNameMatch(name))
        {
            SyncAngleNP.update(values, names, n);
            double angle = SyncAngleNP[0].getValue();
            SyncRotator(angle);
            SyncAngleNP.setState(IPS_OK);
            SyncAngleNP.apply();
            return true;
        }
    }
    return INDI::Rotator::ISNewNumber(dev, name, values, names, n);
}

// ---------------------------------------------------------------------------
// Connection helpers
// ---------------------------------------------------------------------------
bool CaaRotator::createConnection()
{
    destroyConnection();

    if (transport == TRANSPORT_TCP)
    {
        auto *tcp = new TcpConnection();
        connection.reset(tcp);
        std::string ep = tcpHost + ":" + std::to_string(tcpPort);
        if (!tcp->open(ep))
        {
            LOGF_ERROR("TCP connect failed: %s", ep.c_str());
            connection.reset();
            return false;
        }
        LOGF_INFO("TCP connected: %s", ep.c_str());
    }
    else
    {
        auto *ser = new SerialConnection();
        connection.reset(ser);
        std::string ep = serialPort + ":" + std::to_string(serialBaud);
        if (!ser->open(ep))
        {
            LOGF_ERROR("Serial open failed: %s", ep.c_str());
            connection.reset();
            return false;
        }
        LOGF_INFO("Serial opened: %s", ep.c_str());
    }
    return true;
}

void CaaRotator::destroyConnection()
{
    if (connection)
    {
        connection->close();
        connection.reset();
    }
}

bool CaaRotator::sendCmd(const std::string &cmd)
{
    if (!connection || !connection->isOpen()) return false;
    LOGF_DEBUG("TX: %s", cmd.c_str());
    return connection->send(cmd);
}

bool CaaRotator::readResp(std::string &response)
{
    if (!connection || !connection->isOpen()) return false;
    bool ok = connection->recv(response, commandTimeoutMs);
    if (ok) LOGF_DEBUG("RX: %s", response.c_str());
    return ok;
}

// ---------------------------------------------------------------------------
// Handshake / Connect / Disconnect
// ---------------------------------------------------------------------------
bool CaaRotator::Handshake()
{
    if (!createConnection())
        return false;

    std::string resp;

    // V#  — firmware version
    sendCmd("V");
    if (!readResp(resp))
    {
        LOG_ERROR("Handshake: no V# response");
        destroyConnection();
        return false;
    }
    resp.erase(std::remove(resp.begin(), resp.end(), '#'), resp.end());
    if (resp.compare(0, 2, "V ") != 0)
    {
        LOGF_ERROR("Handshake: unexpected V response: %s", resp.c_str());
        destroyConnection();
        return false;
    }
    firmwareVersion = resp.substr(2);
    FirmwareVersionTP[0].setText(firmwareVersion.c_str());
    FirmwareVersionTP.setState(IPS_OK);
    FirmwareVersionTP.apply();
    LOGF_INFO("Firmware: %s", firmwareVersion.c_str());

    // I#  — read reversed state from JSON status
    sendCmd("I");
    if (readResp(resp))
    {
        resp.erase(std::remove(resp.begin(), resp.end(), '#'), resp.end());
        reversed = (resp.find("\"reversed\":true") != std::string::npos);
        ReverseSP[0].setState(reversed ? ISS_ON : ISS_OFF);
        ReverseSP.setState(IPS_OK);
        ReverseSP.apply();
        LOGF_INFO("Reverse: %s", reversed ? "ON" : "OFF");
    }

    // D <spd>#  — sync steps-per-degree (truncated to int for firmware)
    int spdInt = (int)stepsPerDegree;
    sendCmd("D " + std::to_string(spdInt));
    readResp(resp);

    // X <n>#  — sync max speed
    sendCmd("X " + std::to_string(maxSpeed));
    readResp(resp);

    // A <n>#  — sync acceleration
    sendCmd("A " + std::to_string(acceleration));
    readResp(resp);

    // G#  — initial state
    if (!refreshStatus())
    {
        LOG_ERROR("Handshake: cannot read status");
        destroyConnection();
        return false;
    }

    return true;
}

bool CaaRotator::Connect()
{
    if (isConnected()) return true;
    if (!Handshake())
    {
        LOG_ERROR("Connection failed");
        return false;
    }
    SetTimer(getCurrentPollingPeriod());
    LOG_INFO("CAA Rotator connected");
    return true;
}

bool CaaRotator::Disconnect()
{
    if (connection && connection->isOpen())
    {
        std::string resp;
        sendCmd("C 0");          // release continuous hold
        readResp(resp);
    }
    destroyConnection();
    moving = false;
    LOG_INFO("CAA Rotator disconnected");
    return true;
}

// ---------------------------------------------------------------------------
// TimerHit — periodic polling
// ---------------------------------------------------------------------------
void CaaRotator::TimerHit()
{
    if (!isConnected())
    {
        SetTimer(getCurrentPollingPeriod());
        return;
    }

    if (!refreshStatus())
    {
        LOG_WARN("Status read failed, reconnecting...");
        destroyConnection();
        if (!createConnection())
        {
            LOG_ERROR("Reconnect failed");
            SetTimer(getCurrentPollingPeriod());
            return;
        }
        // Re-send config commands after reconnect
        int spdInt = (int)stepsPerDegree;
        std::string resp;
        sendCmd("D " + std::to_string(spdInt));
        readResp(resp);
        sendCmd("X " + std::to_string(maxSpeed));
        readResp(resp);
        sendCmd("A " + std::to_string(acceleration));
        readResp(resp);
    }

    SetTimer(getCurrentPollingPeriod());
}

// ---------------------------------------------------------------------------
// Status parsing
// ---------------------------------------------------------------------------
bool CaaRotator::refreshStatus()
{
    if (!sendCmd("G")) return false;

    std::string resp;
    if (!readResp(resp)) return false;

    long  newSteps = 0;
    bool  newMoving = false;
    if (!parseStatus(resp, newSteps, newMoving)) return false;

    currentLogicalSteps = newSteps;
    moving               = newMoving;
    updatePositionDisplay();
    return true;
}

bool CaaRotator::parseStatus(const std::string &response,
                             long &logicalSteps, bool &isMoving)
{
    // "P <steps>;M <true|false>#"
    size_t p = response.find('P');
    size_t m = response.find('M');
    if (p == std::string::npos || m == std::string::npos) return false;

    std::string steps = response.substr(p + 1, m - p - 1);
    std::string movingStr = response.substr(m + 1);

    // Strip trailing '#' and whitespace
    auto trim = [](std::string &s)
    {
        s.erase(0, s.find_first_not_of(" \t\r\n"));
        s.erase(s.find_last_not_of(" \t\r\n;#") + 1);
    };
    trim(steps);
    trim(movingStr);

    logicalSteps = std::stol(steps);
    isMoving     = (movingStr == "true");
    return true;
}

void CaaRotator::updatePositionDisplay()
{
    double angle = stepsToAngle(currentLogicalSteps);
    GotoRotatorNP[0].setValue(angle);
    GotoRotatorNP.setState(moving ? IPS_BUSY : IPS_OK);
    GotoRotatorNP.apply();
}

// ---------------------------------------------------------------------------
// Coordinate conversion (same formula as ASCOM driver and firmware)
// ---------------------------------------------------------------------------
double CaaRotator::stepsToAngle(long logicalSteps)
{
    double raw = (static_cast<double>(logicalSteps)
                  - stepsPerDegree * 360.0) / stepsPerDegree;
    while (raw < 0.0)     raw += 360.0;
    while (raw >= 360.0)  raw -= 360.0;
    return raw;
}

long CaaRotator::angleToLogicalSteps(double angle)
{
    while (angle < 0.0)    angle += 360.0;
    while (angle >= 360.0) angle -= 360.0;
    return (long)std::llround(angle * stepsPerDegree + 360.0 * stepsPerDegree);
}

// ---------------------------------------------------------------------------
// MoveRotator — GOTO absolute angle
// ---------------------------------------------------------------------------
IPState CaaRotator::MoveRotator(double angle)
{
    if (!connection || !connection->isOpen())
    {
        LOG_ERROR("MoveRotator: not connected");
        return IPS_ALERT;
    }

    long target = angleToLogicalSteps(angle);
    long rev    = (long)(360.0 * stepsPerDegree);

    // Shortest path across the two-turn range
    long a = target, b = target + rev, c = target - rev;
    long chosen = a;
    if (std::abs(b - currentLogicalSteps) < std::abs(chosen - currentLogicalSteps))
        chosen = b;
    if (std::abs(c - currentLogicalSteps) < std::abs(chosen - currentLogicalSteps))
        chosen = c;

    LOGF_INFO("MoveRotator: angle=%.3f° target=%ld chosen=%ld current=%ld",
              angle, target, chosen, currentLogicalSteps);

    sendCmd("M " + std::to_string(chosen));
    std::string resp;
    readResp(resp);

    long dummy; bool mv = false;
    parseStatus(resp, dummy, mv);
    moving = mv;
    updatePositionDisplay();
    return IPS_BUSY;
}

// ---------------------------------------------------------------------------
// SyncRotator
// ---------------------------------------------------------------------------
IPState CaaRotator::SyncRotator(double angle)
{
    if (!connection || !connection->isOpen())
    {
        LOG_ERROR("SyncRotator: not connected");
        return IPS_ALERT;
    }

    long steps = angleToLogicalSteps(angle);
    LOGF_INFO("SyncRotator: %.3f° -> logicalSteps=%ld", angle, steps);

    sendCmd("P " + std::to_string(steps));

    std::string resp;
    if (!readResp(resp))
    {
        LOG_ERROR("SyncRotator: no response");
        return IPS_ALERT;
    }

    long  respSteps = 0;
    bool  respMoving = false;
    parseStatus(resp, respSteps, respMoving);
    currentLogicalSteps = respSteps;
    moving = respMoving;

    GotoRotatorNP[0].setValue(angle);
    GotoRotatorNP.setState(IPS_OK);
    GotoRotatorNP.apply();

    LOGF_INFO("SyncRotator: done, angle=%.3f°", stepsToAngle(currentLogicalSteps));
    return IPS_OK;
}

// ---------------------------------------------------------------------------
// Abort / Halt
// ---------------------------------------------------------------------------
bool CaaRotator::AbortRotator()
{
    if (!connection || !connection->isOpen())
    {
        LOG_ERROR("AbortRotator: not connected");
        return false;
    }

    sendCmd("S");
    std::string resp;
    readResp(resp);

    moving = false;
    GotoRotatorNP.setState(IPS_OK);
    GotoRotatorNP.apply();
    LOG_INFO("AbortRotator: halted");
    return true;
}

// ---------------------------------------------------------------------------
// Home
// ---------------------------------------------------------------------------
bool CaaRotator::HomeRotator()
{
    if (!connection || !connection->isOpen())
    {
        LOG_ERROR("HomeRotator: not connected");
        return false;
    }

    sendCmd("H");
    std::string resp;
    if (!readResp(resp))
    {
        LOG_ERROR("HomeRotator: no response");
        return false;
    }

    moving = true;
    GotoRotatorNP.setState(IPS_BUSY);
    GotoRotatorNP.apply();
    LOG_INFO("HomeRotator: homing started");
    return true;
}
