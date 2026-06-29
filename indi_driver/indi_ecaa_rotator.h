/*============================================================================
 * INDI Rotator driver for CAA (Camera Angle Adjuster).
 *
 * Supports three hardware / transport combinations:
 *   - ESP8266 over TCP  (WiFi,  port 4030)
 *   - ESP8266 over Serial (USB, 9600 baud)
 *   - Arduino Nano over Serial (USB, 9600 baud)
 *
 * All three use the same text protocol (commands end with '#').
 *
 * Firmware: https://github.com/Indigo2233/ECAA
 *
 * Author:  Yao Su
 * License: LGPL-2.1+
 *============================================================================*/

#pragma once

#include <indirotator.h>
#include <string>
#include <memory>

// ---------------------------------------------------------------------------
// Abstract connection — hides TCP vs Serial behind uniform send/recv
// ---------------------------------------------------------------------------
class IConnection
{
public:
    virtual ~IConnection() = default;
    virtual bool isOpen() const = 0;
    virtual bool open(const std::string &endpoint) = 0;
    virtual void close() = 0;
    virtual bool send(const std::string &data) = 0;
    virtual bool recv(std::string &response, int timeoutMs) = 0;
    virtual std::string endpoint() const = 0;
};

// ---------------------------------------------------------------------------
// TCP connection (ESP8266 WiFi)
// ---------------------------------------------------------------------------
class TcpConnection : public IConnection
{
public:
    TcpConnection();
    ~TcpConnection() override;
    bool isOpen() const override;
    bool open(const std::string &hostPort) override;
    void close() override;
    bool send(const std::string &data) override;
    bool recv(std::string &response, int timeoutMs) override;
    std::string endpoint() const override;

private:
    int sock;
    std::string host;
    int port_;
};

// ---------------------------------------------------------------------------
// Serial connection (ESP8266 USB or Arduino Nano USB, 9600 8N1)
// ---------------------------------------------------------------------------
class SerialConnection : public IConnection
{
public:
    SerialConnection();
    ~SerialConnection() override;
    bool isOpen() const override;
    bool open(const std::string &portAndBaud) override;
    void close() override;
    bool send(const std::string &data) override;
    bool recv(std::string &response, int timeoutMs) override;
    std::string endpoint() const override;

private:
#ifdef _WIN32
    void *hSerial;      // HANDLE
#else
    int fd;
#endif
    std::string portName;
    int baudRate;
};

// ---------------------------------------------------------------------------
// Main INDI Rotator driver
// ---------------------------------------------------------------------------
class CaaRotator : public INDI::Rotator
{
public:
    CaaRotator();

    // INDI overrides
    virtual bool initProperties() override;
    virtual bool updateProperties() override;
    virtual bool Connect() override;
    virtual bool Disconnect() override;
    virtual const char *getDefaultName() override;
    virtual bool Handshake() override;
    virtual void TimerHit() override;
    virtual void ISGetProperties(const char *dev) override;
    virtual bool ISNewNumber(const char *dev, const char *name,
                             double values[], char *names[], int n) override;
    virtual bool ISNewSwitch(const char *dev, const char *name,
                             ISState *states, char *names[], int n) override;
    virtual bool ISNewText(const char *dev, const char *name,
                           char *texts[], char *names[], int n) override;

    // Rotator commands
    virtual IPState MoveRotator(double angle) override;
    virtual IPState SyncRotator(double angle) override;
    virtual bool AbortRotator() override;
    virtual bool HomeRotator() override;

private:
    bool createConnection();
    void destroyConnection();
    bool sendCmd(const std::string &cmd);
    bool readResp(std::string &response);

    bool refreshStatus();
    bool parseStatus(const std::string &response, long &logicalSteps, bool &isMoving);
    void updatePositionDisplay();

    // Coordinate conversion (matches firmware & ASCOM driver)
    double stepsToAngle(long logicalSteps);
    long   angleToLogicalSteps(double angle);

    // --- INDI Properties ---
    INDI::PropertySwitch TransportSP{2};       // TCP | Serial
    INDI::PropertyText   TcpHostTP{1};          // host
    INDI::PropertyNumber TcpPortNP{1};          // port
    INDI::PropertyText   SerialPortTP{1};       // /dev/ttyUSB0
    INDI::PropertyNumber SerialBaudNP{1};       // 9600
    INDI::PropertyNumber StepsPerDegreeNP{1};
    INDI::PropertyNumber MaxSpeedNP{1};
    INDI::PropertyNumber AccelerationNP{1};
    INDI::PropertyNumber CommandTimeoutNP{1};
    INDI::PropertySwitch ReverseSP{1};          // direction reverse
    INDI::PropertyText   FirmwareVersionTP{1};
    INDI::PropertyNumber SyncAngleNP{1};

    // --- State ---
    std::unique_ptr<IConnection> connection;

    enum Transport { TRANSPORT_TCP = 0, TRANSPORT_SERIAL = 1 };
    Transport transport;

    std::string tcpHost;
    int         tcpPort;
    std::string serialPort;
    int         serialBaud;
    int         commandTimeoutMs;
    double      stepsPerDegree;
    int         maxSpeed;
    int         acceleration;
    long        currentLogicalSteps;
    bool        moving;
    bool        reversed;
    std::string firmwareVersion;
};
