using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cave.Console;
using Cave.Logging;

namespace Cave.Syslog
{
    /// <summary>
    /// Provides udp logging to a syslog server.
    /// </summary>
    public class LogUdpSyslog : LogReceiver, IDisposable
    {
        static IPAddress GetIPAddress(ConnectionString connection)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(connection.Server);
            //prefer IPv4
            foreach (IPAddress address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
            {
                return address;
            }
            //search IPv6
            foreach (IPAddress address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
            {
                return address;
            }
            throw new InvalidDataException(string.Format("Cannot find a valid IPv4 / v6 IPAddress for server {0}!", connection.Server));
        }

        UdpClient m_UdpClient;
        SyslogMessageVersion m_Version = SyslogMessageVersion.RFC3164;
        int m_MaximumMessageLength = 2048;
        string m_ProcessName;

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            UdpClient l_UdpClient = m_UdpClient;
            if (l_UdpClient == null)
            {
                return;
            }

            string text = source + ": " + content.Text;
            foreach (string part in StringExtensions.SplitNewLineAndLength(text, m_MaximumMessageLength))
            {
                SyslogSeverity l_Severity = (SyslogSeverity)((int)level & 0x7);
                SyslogMessage item = new SyslogMessage(m_Version, Facility, l_Severity, dateTime, Environment.MachineName.ToLower(), m_ProcessName, 0, null, part, null);
                byte[] data = Encoding.UTF8.GetBytes(item.ToString());
                l_UdpClient.Send(data, data.Length, Target);
            }
        }

        /// <summary>
        /// Syslog protocol version to be used to send messages. Valid values: [0..1].
        /// </summary>
        public SyslogMessageVersion Version
        {
            get => m_Version;
            set
            {
                switch (value)
                {
                    case SyslogMessageVersion.RFC3164:
                    case SyslogMessageVersion.RFC5424:
                    case SyslogMessageVersion.RSYSLOG:
                        m_Version = value;
                        return;
                    default:
                        throw new NotSupportedException(string.Format("Syslog protocol version '{0}' unknown or not supported!", value));
                }
            }
        }

        /// <summary>Retrieves the destination address used to send log items to.</summary>
        public IPEndPoint Target = new IPEndPoint(IPAddress.Loopback, 514);

        /// <summary>Retrieves the Facility used to send the syslog messages. The default facility is local0.</summary>
        public SyslogFacility Facility = SyslogFacility.Local0;

        /// <summary>Creates a new syslog instance (localhost).</summary>
        public LogUdpSyslog()
            : base()
        {
            m_ProcessName = Process.GetCurrentProcess().ProcessName;
            m_UdpClient = new UdpClient();
            Target = new IPEndPoint(IPAddress.Loopback, 514);
        }

        /// <summary>
        /// Creates a new syslog instance.
        /// </summary>
        /// <param name="target"><see cref="IPEndPoint"/>: target server.</param>
        public LogUdpSyslog(IPEndPoint target)
            : this()
        {
            Target = target;
        }

        /// <summary>
        /// Creates a new syslog instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public LogUdpSyslog(IPAddress address, int port)
            : this()
        {
            Target = new IPEndPoint(address, port);
        }

        /// <summary>
        /// Creates a new syslog instance.
        /// </summary>
        /// <param name="connection"><see cref="ConnectionString"/> of the form udp://server:port, tcp://server:port, tcps://server:port.</param>
        public LogUdpSyslog(ConnectionString connection)
            : this(GetIPAddress(connection), connection.GetPort(514))
        {
            if (connection.Protocol == null)
            {
                throw new ArgumentNullException("connection", "Protocol (udp, tcp, tcps) has to be given!");
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

        /// <summary>
        /// Gets / sets the maximum message length used.
        /// </summary>
        public int MaximumMessageLength
        {
            get => m_MaximumMessageLength;
            set => m_MaximumMessageLength = Math.Max(480, Math.Min(65400, value));
        }

        /// <summary>
        /// Obtains an identification string for the object.
        /// </summary>
        public override string ToString()
        {
            return "Syslog<" + Target.ToString() + ">[" + m_Version + "]";
        }

        /// <summary>
        /// Disposes the <see cref="LogUdpSyslog"/> instance and releases the socket used.
        /// </summary>
        public override void Close()
        {
            if (Closed)
            {
                return;
            }

            base.Close();
            m_UdpClient?.Close();
            m_UdpClient = null;
        }

        /// <summary>Obtains the name of the log.</summary>
        public override string LogSourceName => "syslog://" + Target.ToString();

        #region IDisposable Member
        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "m_UdpClient")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_UdpClient?.Close();
                m_UdpClient = null;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
