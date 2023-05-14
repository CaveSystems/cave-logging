using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cave.Logging;

namespace Cave.Syslog;

/// <summary>Provides udp logging to a syslog server.</summary>
public class LogUdpSyslog : LogReceiver, IDisposable
{
    #region Static

    static IPAddress GetIPAddress(ConnectionString connection)
    {
        var addresses = Dns.GetHostAddresses(connection.Server);

        // prefer IPv4
        foreach (var address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
        {
            return address;
        }

        // search IPv6
        foreach (var address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
        {
            return address;
        }

        throw new InvalidDataException(string.Format("Cannot find a valid IPv4 / v6 IPAddress for server {0}!", connection.Server));
    }

    #endregion Static

    #region Private Fields

    int maximumMessageLength = 2048;

    UdpClient udpClient;

    SyslogMessageVersion version = SyslogMessageVersion.RFC3164;

    #endregion Private Fields

    #region Public Fields

    /// <summary>Retrieves the Facility used to send the syslog messages. The default facility is local0.</summary>
    public SyslogFacility Facility = SyslogFacility.Local0;

    /// <summary>Retrieves the destination address used to send log items to.</summary>
    public IPEndPoint Target = new(IPAddress.Loopback, 514);

    #endregion Public Fields

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogUdpSyslog"/> class.</summary>
    /// <remarks>This is the default instance logging to localhost.</remarks>
    public LogUdpSyslog()
    {
        udpClient = new UdpClient();
        Target = new IPEndPoint(IPAddress.Loopback, 514);
    }

    /// <summary>Initializes a new instance of the <see cref="LogUdpSyslog"/> class.</summary>
    /// <param name="target"><see cref="IPEndPoint"/>: target server.</param>
    public LogUdpSyslog(IPEndPoint target)
        : this() =>
        Target = target;

    /// <summary>Initializes a new instance of the <see cref="LogUdpSyslog"/> class.</summary>
    /// <param name="address">Target ip address.</param>
    /// <param name="port">Target port.</param>
    public LogUdpSyslog(IPAddress address, int port)
        : this() =>
        Target = new IPEndPoint(address, port);

    /// <summary>Initializes a new instance of the <see cref="LogUdpSyslog"/> class.</summary>
    /// <param name="connection"><see cref="ConnectionString"/> of the form udp://server:port.</param>
    public LogUdpSyslog(ConnectionString connection)
        : this(GetIPAddress(connection), connection.GetPort(514))
    {
        if (connection.Protocol == null)
        {
            throw new ArgumentNullException(nameof(connection), "Protocol (udp) has to be given!");
        }

        switch (connection.Protocol.ToUpperInvariant())
        {
            case "UDP":
                Version = SyslogMessageVersion.RFC3164;
                break;

            case "TCP":
            case "TCPS":
                Version = SyslogMessageVersion.RFC5424;
                throw new NotImplementedException("Not jet completed!");
            default:
                throw new NotSupportedException(string.Format("Syslog protocol version '{0}' unknown or not supported!", connection.Protocol));
        }
    }

    #endregion Constructors

    #region Properties

    /// <summary>Gets or sets the maximum message length used.</summary>
    public int MaximumMessageLength
    {
        get => maximumMessageLength;
        set => maximumMessageLength = Math.Max(480, Math.Min(65400, value));
    }

    /// <summary>Gets or sets the syslog protocol version to be used to send messages. Valid values: [0..1].</summary>
    public SyslogMessageVersion Version
    {
        get => version;
        set
        {
            switch (value)
            {
                case SyslogMessageVersion.RFC3164:
                case SyslogMessageVersion.RFC5424:
                case SyslogMessageVersion.RSYSLOG:
                    version = value;
                    return;

                default:
                    throw new NotSupportedException(string.Format("Syslog protocol version '{0}' unknown or not supported!", value));
            }
        }
    }

    #endregion Properties

    #region Overrides

    /// <summary>Writes the specified log message.</summary>
    /// <param name="dateTime">The date time.</param>
    /// <param name="level">The level.</param>
    /// <param name="source">The source.</param>
    /// <param name="content">The content.</param>
    protected override void Write(DateTime dateTime, LogLevel level, string source, LogText content)
    {
        var udp = udpClient;
        if (udp == null)
        {
            return;
        }

        var text = source + ": " + content.Text;
        foreach (var part in text.SplitNewLineAndLength(maximumMessageLength))
        {
            var severity = (SyslogSeverity)((int)level & 0x7);
            var item = new SyslogMessage(version, Facility, severity, dateTime, Logger.HostName, Logger.ProcessName, 0, null, part, null);
            var data = Encoding.UTF8.GetBytes(item.ToString());
            udp.Send(data, data.Length, Target);
        }
    }

    /// <summary>Disposes the <see cref="LogUdpSyslog"/> instance and releases the socket used.</summary>
    public override void Close()
    {
        if (Closed)
        {
            return;
        }

        base.Close();
        udpClient?.Close();
        udpClient = null;
    }

    #region IDisposable Member

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            udpClient?.Close();
            udpClient = null;
        }

        base.Dispose(disposing);
    }

    #endregion IDisposable Member

    #endregion Overrides

    #region Overrides

    /// <summary>Obtains an identification string for the object.</summary>
    public override string ToString() => "Syslog<" + Target + ">[" + version + "]";

    #endregion Overrides
}