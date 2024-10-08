using System;
using System.Net.Sockets;
using System.Text;
using Cave.Collections.Generic;

namespace Cave.Logging;

/// <summary>Provides a tcp and udp syslog sender for RFC5424, RFC3164 and RSYSLOG format.</summary>
public class SyslogSender : IDisposable
{
    #region Private Fields

    static readonly byte[] Empty = [];
    bool disposed;
    ConnectionString target;
    TcpClient? tcpClient;
    UdpClient? udpClient;
    SyslogMessageVersion version = SyslogMessageVersion.Undefined;

    #endregion Private Fields

    #region Protected Methods

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
            }

            disposed = true;
        }
    }

    #endregion Protected Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="SyslogSender"/> class.</summary>
    public SyslogSender()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SyslogSender"/> class.</summary>
    /// <param name="version">The <see cref="SyslogMessageVersion"/> to use for all outgoing messages.</param>
    public SyslogSender(SyslogMessageVersion version) => this.version = version;

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Connects a socket to the specified destination. If the default constructor was used to create this instance it is possible to set the syslog message
    /// version by using the version=&lt;version&gt; switch.
    /// </summary>
    /// <example>tcp://server.tld udp://1.2.3.4:512?version=RFC3164.</example>
    /// <param name="connectionString">Destination and syslog message version (optional, if not set at constructor already).</param>
    public void Connect(ConnectionString connectionString)
    {
        var options = OptionCollection.FromStrings(connectionString.Options?.Split('&'));
        if (version == SyslogMessageVersion.Undefined)
        {
            if (options.Contains("version"))
            {
                version = (SyslogMessageVersion)Enum.Parse(typeof(SyslogMessageVersion), options["version"].Value ?? "0", true);
            }
            else
            {
                version = SyslogMessageVersion.RFC5424;
            }
        }

        switch (connectionString.ConnectionType)
        {
            case ConnectionType.TCP:
                tcpClient = new TcpClient();
                tcpClient.Connect(connectionString.Server ?? "localhost", connectionString.GetPort(514));
                return;

            case ConnectionType.UDP:
                udpClient = new UdpClient();
                udpClient.Connect(connectionString.Server ?? "localhost", connectionString.GetPort(514));
                udpClient.Send(Empty, 0);
                return;

            default:
                throw new NotImplementedException(string.Format("Unknown connection type {0}!", connectionString.ConnectionType));
        }
    }

    /// <summary>Releases all resources used by the this instance.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Sends a syslog message to the connected destination.</summary>
    /// <param name="msg"></param>
    public void Send(SyslogMessage msg)
    {
        msg.Version = version;
        var data = Encoding.UTF8.GetBytes(msg + "\n");
        if (tcpClient != null)
        {
            if (!tcpClient.Connected)
            {
                // reconnect
                tcpClient.Connect(target.Server ?? "localhost", target.GetPort(514));
            }

            tcpClient.Client.Send(data);
            return;
        }

        if (udpClient != null)
        {
            udpClient.Send(data, data.Length);
            return;
        }

        throw new InvalidOperationException("No target defined!");
    }

    #endregion Public Methods
}
