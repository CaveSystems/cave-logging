using System;
using System.Net.Sockets;
using System.Text;
using Cave.Collections.Generic;
using Cave.Logging;

namespace Cave.Syslog;

/// <summary>Provides a tcp and udp syslog sender for RFC5424, RFC3164 and RSYSLOG format.</summary>
public class SyslogSender : IDisposable
{
    #region Private Fields

#if NETSTANDARD1_0_OR_GREATER || NET5_0_OR_GREATER
    static readonly byte[] empty = Array.Empty<byte>();
#else
    static readonly byte[] empty = new byte[0];
#endif
    ConnectionString target;
    TcpClient? tcpClient;
    UdpClient? udpClient;
    SyslogMessageVersion version = SyslogMessageVersion.Undefined;

    #endregion Private Fields

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="SyslogSender"/> class.</summary>
    public SyslogSender()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SyslogSender"/> class.</summary>
    /// <param name="version">The <see cref="SyslogMessageVersion"/> to use for all outgoing messages.</param>
    public SyslogSender(SyslogMessageVersion version) => this.version = version;

    #endregion Constructors

    #region Members

    /// <summary>
    /// Connects a socket to the specified destination. If the default constructor was used to create this instance it is possible to set the syslog message
    /// version by using the version=&lt;version&gt; switch.
    /// </summary>
    /// <example>tcp://server.tld udp://1.2.3.4:512?version=RFC3164.</example>
    /// <param name="connectionString">Destination and syslog message version (optional, if not set at constructor already).</param>
    public void Connect(ConnectionString connectionString)
    {
        var options = OptionCollection.FromStrings(connectionString.Options.Split('&'));
        if (version == SyslogMessageVersion.Undefined)
        {
            if (options.Contains("version"))
            {
                version = (SyslogMessageVersion)Enum.Parse(typeof(SyslogMessageVersion), options["version"].Value, true);
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
                tcpClient.Connect(connectionString.Server, connectionString.GetPort(514));
                return;

            case ConnectionType.UDP:
                udpClient = new UdpClient();
                udpClient.Connect(connectionString.Server, connectionString.GetPort(514));
                udpClient.Send(empty, 0);
                return;

            default:
                throw new NotImplementedException(string.Format("Unknown connection type {0}!", connectionString.ConnectionType));
        }
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
                tcpClient.Connect(target.Server, target.GetPort(514));
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

    #endregion Members

    #region IDisposable Support

    bool disposed;

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

    /// <summary>Releases all resources used by the this instance.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}
