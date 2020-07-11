using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

            // prefer IPv4
            foreach (IPAddress address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
            {
                return address;
            }

            // search IPv6
            foreach (IPAddress address in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
            {
                return address;
            }
            throw new InvalidDataException(string.Format("Cannot find a valid IPv4 / v6 IPAddress for server {0}!", connection.Server));
        }

        UdpClient udpClient;
        SyslogMessageVersion version = SyslogMessageVersion.RFC3164;
        int maximumMessageLength = 2048;

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            UdpClient udp = udpClient;
            if (udp == null)
            {
                return;
            }

            string text = source + ": " + content.Text;
            foreach (string part in StringExtensions.SplitNewLineAndLength(text, maximumMessageLength))
            {
                var l_Severity = (SyslogSeverity)((int)level & 0x7);
                var item = new SyslogMessage(version, Facility, l_Severity, dateTime, Logger.HostName, Logger.ProcessName, 0, null, part, null);
                byte[] data = Encoding.UTF8.GetBytes(item.ToString());
                udp.Send(data, data.Length, Target);
            }
        }

        /// <summary>
        /// Gets or sets the syslog protocol version to be used to send messages. Valid values: [0..1].
        /// </summary>
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

        /// <summary>Retrieves the destination address used to send log items to.</summary>
        public IPEndPoint Target = new IPEndPoint(IPAddress.Loopback, 514);

        /// <summary>Retrieves the Facility used to send the syslog messages. The default facility is local0.</summary>
        public SyslogFacility Facility = SyslogFacility.Local0;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogUdpSyslog"/> class.
        /// </summary>
        /// <remarks>
        /// This is the default instance logging to localhost.
        /// </remarks>
        public LogUdpSyslog()
            : base()
        {
            udpClient = new UdpClient();
            Target = new IPEndPoint(IPAddress.Loopback, 514);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogUdpSyslog"/> class.
        /// </summary>
        /// <param name="target"><see cref="IPEndPoint"/>: target server.</param>
        public LogUdpSyslog(IPEndPoint target)
            : this()
        {
            Target = target;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogUdpSyslog"/> class.
        /// </summary>
        /// <param name="address">Target ip address.</param>
        /// <param name="port">Target port.</param>
        public LogUdpSyslog(IPAddress address, int port)
            : this()
        {
            Target = new IPEndPoint(address, port);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogUdpSyslog"/> class.
        /// </summary>
        /// <param name="connection"><see cref="ConnectionString"/> of the form udp://server:port.</param>
        public LogUdpSyslog(ConnectionString connection)
            : this(GetIPAddress(connection), connection.GetPort(514))
        {
            if (connection.Protocol == null)
            {
                throw new ArgumentNullException("connection", "Protocol (udp) has to be given!");
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
        /// Gets or sets the maximum message length used.
        /// </summary>
        public int MaximumMessageLength
        {
            get => maximumMessageLength;
            set => maximumMessageLength = Math.Max(480, Math.Min(65400, value));
        }

        /// <summary>
        /// Obtains an identification string for the object.
        /// </summary>
        public override string ToString()
        {
            return "Syslog<" + Target.ToString() + ">[" + version + "]";
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

        #endregion
    }
}
