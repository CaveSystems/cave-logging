using Cave.Collections.Generic;
using Cave.Logging;
using System;
using System.Net.Sockets;
using System.Text;

namespace Cave.Syslog
{
    /// <summary>
    /// Provides a tcp and udp syslog sender for RFC5424, RFC3164 and RSYSLOG format
    /// </summary>
    public class SyslogSender : IDisposable
    {
        TcpClient m_TcpClient;
        UdpClient m_UdpClient;
        ConnectionString m_Target;
        SyslogMessageVersion m_Version = SyslogMessageVersion.Undefined;

        /// <summary>
        /// Creates a new syslog sender without message version preference. The message version can be set by the Connect() function later.
        /// </summary>
        public SyslogSender() : base()
        {
        }

        /// <summary>
        /// Creates a new syslog sender with the specified syslog message version.
        /// </summary>
        /// <param name="version">The <see cref="SyslogMessageVersion"/> to use for all outgoing messages</param>
        public SyslogSender(SyslogMessageVersion version)
        {
            m_Version = version;
        }

        /// <summary>
        /// Connects a socket to the specified destination. If the default constructor was used to create this instance it is possible to set the
        /// syslog message version by using the version=&lt;version&gt; switch.
        /// </summary>
        /// <example>
        /// tcp://server.tld
        /// udp://1.2.3.4:512?version=RFC3164
        /// </example>
        /// <param name="connectionString">Destination and syslog message version (optional, if not set at constructor already)</param>
        public void Connect(ConnectionString connectionString)
        {
            OptionCollection options = OptionCollection.FromStrings(connectionString.Options.Split('&'));
            if (m_Version == SyslogMessageVersion.Undefined)
            {
                if (options.Contains("version"))
                {
                    m_Version = (SyslogMessageVersion)Enum.Parse(typeof(SyslogMessageVersion), options["version"].Value, true);
                }
                else
                {
                    m_Version = SyslogMessageVersion.RFC5424;
                }
            }
            switch (connectionString.ConnectionType)
            {
                case ConnectionType.TCP:
                    m_TcpClient = new TcpClient();
                    m_TcpClient.Connect(connectionString.Server, connectionString.GetPort(514));
                    return;

                case ConnectionType.UDP:
                    m_UdpClient = new UdpClient();
                    m_UdpClient.Connect(connectionString.Server, connectionString.GetPort(514));
                    m_UdpClient.Send(new byte[0], 0);
                    return;

                default:
                    throw new NotImplementedException(string.Format("Unknown connection type {0}!", connectionString.ConnectionType));
            }
        }

        /// <summary>
        /// Sends a syslog message to the connected destination
        /// </summary>
        /// <param name="msg"></param>
        public void Send(SyslogMessage msg)
        {
            msg.Version = m_Version;
            byte[] data = Encoding.UTF8.GetBytes(msg.ToString() + "\n");

            if (m_TcpClient != null)
            {
                if (!m_TcpClient.Connected)
                {
                    //reconnect
                    try { m_TcpClient.Connect(m_Target.Server, m_Target.GetPort(514)); }
                    catch { this.LogWarning($"No connection to {m_Target.ToString(ConnectionStringPart.NoCredentials)}"); }
                }

                m_TcpClient.Client.Send(data);
                return;
            }

            if (m_UdpClient != null)
            {
                m_UdpClient.Send(data, data.Length);
                return;
            }

            throw new InvalidOperationException("No target defined!");
        }

        #region IDisposable Support
        private bool m_Disposed = false;

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    if (m_TcpClient != null) { m_TcpClient.Close(); m_TcpClient = null; }
                    if (m_UdpClient != null) { m_UdpClient.Close(); m_UdpClient = null; }
                }
                m_Disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
