using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cave.Data;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a syslog entry. RFC 3164, RFC 3195, RFC 5424.
    /// </summary>
    public struct SyslogMessage : IComparable
    {
        /// <summary>Implements the operator ==.</summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(SyslogMessage x, SyslogMessage y)
        {
            return x.Content == y.Content
                && x.Facility == y.Facility
                && x.HostName == y.HostName
                && x.MessageID == y.MessageID
                && x.ProcessID == y.ProcessID
                && x.ProcessName == y.ProcessName
                && x.Severity == y.Severity
                && x.TimeStamp == y.TimeStamp
                && x.Version == y.Version
                && x.StructuredData == y.StructuredData;
        }

        /// <summary>Implements the operator !=.</summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(SyslogMessage x, SyslogMessage y)
        {
            return x.Content != y.Content
             || x.Facility != y.Facility
             || x.HostName != y.HostName
             || x.MessageID != y.MessageID
             || x.ProcessID != y.ProcessID
             || x.ProcessName != y.ProcessName
             || x.Severity != y.Severity
             || x.TimeStamp != y.TimeStamp
             || x.Version != y.Version
             || x.StructuredData != y.StructuredData;
        }

        /// <summary>Implements the operator &lt;.</summary>
        /// <param name="msg1">The MSG1.</param>
        /// <param name="msg2">The MSG2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(SyslogMessage msg1, SyslogMessage msg2)
        {
            return msg1.TimeStamp < msg2.TimeStamp;
        }

        /// <summary>Implements the operator &gt;.</summary>
        /// <param name="msg1">The MSG1.</param>
        /// <param name="msg2">The MSG2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(SyslogMessage msg1, SyslogMessage msg2)
        {
            return msg1.TimeStamp > msg2.TimeStamp;
        }

        /// <summary>
        /// Provides the chars valid for names and ids at structured data.
        /// </summary>
        public const string ValidNameChars = "!\"#$%&'()*+,-./0123456789:;<>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        /// <summary>
        /// Gets the maximum message length.
        /// </summary>
        public static int GetMaximumMessageLength(SyslogMessageVersion version)
        {
            switch (version)
            {
                case SyslogMessageVersion.RFC3164: return 2048;
                case SyslogMessageVersion.RSYSLOG:
                case SyslogMessageVersion.RFC5424: return 65400;
                default: throw new NotImplementedException(string.Format("SyslogMessageVersion {0} undefined!", version));
            }
        }

        /// <summary>
        /// Parses a RFC3164 formatted syslog message.
        /// http://www.ietf.org/rfc/rfc3164.txt.
        /// </summary>
        /// <param name="data">BSD syslog data.</param>
        /// <returns></returns>
        public static SyslogMessage ParseRFC3164(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var result = new SyslogMessage
            {
                Version = SyslogMessageVersion.RFC3164,
            };
            int start = 0;

            // decode priority
            {
                if (!data.StartsWith("<"))
                {
                    throw new FormatException(string.Format("Could not parse PRI part"));
                }

                string l_PriorityString = StringExtensions.GetString(data, 0, '<', '>');
                if (!int.TryParse(l_PriorityString, out int l_Priority))
                {
                    throw new FormatException(string.Format("Invalid priority!"));
                }

                start += l_PriorityString.Length + 2;
                result.Severity = (SyslogSeverity)(l_Priority % 8);
                result.Facility = (SyslogFacility)(l_Priority / 8);
            }

            // check space
            if (!char.IsLetter(data[start]))
            {
                throw new FormatException(string.Format("Invalid parser for this message type!"));
            }

            result.TimeStamp = SyslogMessageDateTime.ParseRFC3164(data.Substring(start));
            start += 14;
            if (data[start] != ' ')
            {
                start++;
            }

            // copy HostName
            {
                result.HostName = StringExtensions.GetString(data, start, ' ', ' ');
                start += result.HostName.Length + 1;
            }

            // decode ProcessName
            {
                result.ProcessName = StringExtensions.GetString(data, start, ' ', ':');
                result.ProcessID = 0;
                start += result.ProcessName.Length + 2;
                int startProcessID = result.ProcessName.IndexOf('[');
                if (startProcessID > -1)
                {
                    result.ProcessID = 0;
                    string id = StringExtensions.GetString(result.ProcessName, startProcessID, '[', ']');
                    if (id != null)
                    {
                        if (uint.TryParse(id, out result.ProcessID))
                        {
                            result.ProcessName = result.ProcessName.Substring(0, startProcessID);
                        }
                    }
                }
                start++;
            }

            // copy content
            string content = data.Substring(start, data.Length - start);
            result.Content = content.Replace('\0', ' ').TrimEnd('\r', '\n');
            return result;
        }

        /// <summary>
        /// Parses a RFC5424 formatted syslog message.
        /// http://www.ietf.org/rfc/rfc5424.txt.
        /// </summary>
        /// <param name="data">Syslog Protocol Data.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static SyslogMessage ParseRFC5424(string data)
        {
            var result = new SyslogMessage
            {
                Version = SyslogMessageVersion.RFC5424,
            };
            int start = 0;

            // decode priority
            {
                string l_PriorityString = StringExtensions.GetString(data, 0, '<', '>');
                if (!int.TryParse(l_PriorityString, out int l_Priority))
                {
                    throw new InvalidDataException(string.Format("Invalid priority!"));
                }

                start += l_PriorityString.Length + 2;
                result.Severity = (SyslogSeverity)(l_Priority % 8);
                result.Facility = (SyslogFacility)(l_Priority / 8);
            }

            // check 1
            if (data[start++] != (byte)'1')
            {
                throw new InvalidOperationException(string.Format("Invalid parser for this message type!"));
            }

            string dateTimeString = StringExtensions.GetString(data, start, ' ', ' ');
            start += dateTimeString.Length + 1;
            result.TimeStamp = SyslogMessageDateTime.ParseRFC5424(dateTimeString);

            result.HostName = StringExtensions.GetString(data, start, ' ', ' ');
            start += result.HostName.Length + 1;
            if (result.HostName == "-")
            {
                result.HostName = null;
            }

            result.ProcessName = StringExtensions.GetString(data, start, ' ', ' ');
            start += result.ProcessName.Length + 1;
            if (result.ProcessName == "-")
            {
                result.ProcessName = null;
            }

            result.ProcessID = 0;
            string l_ProcessID = StringExtensions.GetString(data, start, ' ', ' ');
            start += l_ProcessID.Length + 1;
            if (l_ProcessID != "-")
            {
                if (!uint.TryParse(l_ProcessID, out result.ProcessID))
                {
                    throw new InvalidDataException(string.Format("Invalid ProcessID field!"));
                }
            }

            result.MessageID = StringExtensions.GetString(data, start, ' ', ' ');
            start += result.MessageID.Length + 1;
            if (result.MessageID == "-")
            {
                result.MessageID = null;
            }

            start += 1;
            if (data[start] == '-')
            {
                start++;
            }
            else
            {
                var parts = new List<SyslogStructuredDataPart>();
                try
                {
                    while (true)
                    {
                        // TODO escaped control chars
                        string s = StringExtensions.GetString(data, start, '[', ']');
                        parts.Add(SyslogStructuredDataPart.Parse("[" + s + "]"));
                        start += s.Length + 2;
                        if (start >= data.Length)
                        {
                            break;
                        }

                        if (data[start] == '[')
                        {
                            continue;
                        }

                        break;
                    }
                }
                catch
                {
                    /*ignore invalid structured data and add it to the message content */
                }
                if (parts.Count > 0)
                {
                    result.StructuredData = new SyslogStructuredData(parts);
                }
            }
            if (start < data.Length)
            {
                // check for space (some syslog servers do not send this)
                if (data[start] == ' ')
                {
                    start++;
                }

                // get message content
                string content = data.Substring(start);

                // remove utf8 bom
                if (content.StartsWith(ASCII.Strings.UTF8BOM))
                {
                    content = content.Substring(ASCII.Strings.UTF8BOM.Length);
                }

                // replace invalid chars and trim end
                result.Content = content.Replace('\0', ' ').TrimEnd('\r', '\n');
            }
            return result;
        }

        /// <summary>
        /// Parses proprietary RSYSLOG protocol messages.
        /// </summary>
        /// <param name="data">RSYSLOG message data.</param>
        /// <returns></returns>
        public static SyslogMessage ParseRSYSLOG(string data)
        {
            var result = new SyslogMessage
            {
                Version = SyslogMessageVersion.RSYSLOG,
            };
            int start = 0;

            // decode priority
            {
                string l_PriorityString = StringExtensions.GetString(data, 0, '<', '>');
                if (!int.TryParse(l_PriorityString, out int l_Priority))
                {
                    throw new FormatException(string.Format("Invalid priority!"));
                }

                start += l_PriorityString.Length + 1;
                result.Severity = (SyslogSeverity)(l_Priority % 8);
                result.Facility = (SyslogFacility)(l_Priority / 8);
            }

            string dateTimeString = StringExtensions.GetString(data, start, '>', ' ');
            start += dateTimeString.Length + 1;
            result.TimeStamp = SyslogMessageDateTime.ParseRFC5424(dateTimeString);

            result.HostName = StringExtensions.GetString(data, start, ' ', ' ');
            start += result.HostName.Length + 1;

            result.ProcessName = StringExtensions.GetString(data, start, ' ', ':');
            start += result.ProcessName.Length + 2;

            if (result.ProcessName.IndexOf('[') > -1)
            {
                result.ProcessID = 0;
                try
                {
                    while (true)
                    {
                        int idStart = result.ProcessName.IndexOf('[');
                        if (idStart < 0)
                        {
                            break;
                        }

                        int idEnd = result.ProcessName.IndexOf(']', idStart);
                        if (idEnd < 0)
                        {
                            break;
                        }

                        string l_ProcessID = result.ProcessName.Substring(idStart, idEnd - idStart + 1);
                        l_ProcessID = l_ProcessID.Unbox("[", "]", false);
                        if (!uint.TryParse(l_ProcessID, out result.ProcessID))
                        {
                            break;
                        }

                        result.ProcessName = result.ProcessName.Remove(idStart, idEnd - idStart + 1);
                        break;
                    }
                }
                catch { }
            }

            // check for space (some syslog servers do not send this)
            if (data[start] == ' ')
            {
                start++;
            }

            // got structured data ?
            if ((data[start] == '[') && (data.IndexOf(']', start) > -1))
            {
                var parts = new List<SyslogStructuredDataPart>();
                try
                {
                    while (true)
                    {
                        // TODO escaped control chars
                        string item = StringExtensions.GetString(data, start, '[', ']');
                        parts.Add(SyslogStructuredDataPart.Parse("[" + item + "]"));
                        start += item.Length + 2;
                        if (start >= data.Length)
                        {
                            break;
                        }

                        if (data[start] == '[')
                        {
                            continue;
                        }

                        break;
                    }
                }
                catch
                {
                    /*ignore invalid structured data and add it to the message content */
                }
                if (parts.Count > 0)
                {
                    result.StructuredData = new SyslogStructuredData(parts);
                }
            }
            if (start < data.Length)
            {
                // check for space (some syslog servers do not send this)
                if (data[start] == ' ')
                {
                    start++;
                }

                // get message content
                string content = data.Substring(start);

                // remove utf8 bom
                if (content.StartsWith(ASCII.Strings.UTF8BOM))
                {
                    content = content.Substring(ASCII.Strings.UTF8BOM.Length);
                }

                // replace invalid chars and trim end
                result.Content = content.Replace('\0', ' ').TrimEnd('\r', '\n');
            }
            return result;
        }

        /// <summary>
        /// Creates a syslog item from specified encoded syslog data.
        /// </summary>
        /// <param name="data">The data to be parsed.</param>
        public static SyslogMessage Parse(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int index = data.IndexOf('>');
            if (index < 0)
            {
                throw new FormatException(string.Format("Could not parse PRI part!"));
            }

            char version = data[index + 1];
            if (char.IsDigit(version))
            {
                // check rsyslog
                if (data.IndexOf('-', index) == index + 5)
                {
                    return ParseRSYSLOG(data);
                }
                else
                {
                    if (version != '1')
                    {
                        throw new NotImplementedException(string.Format("Syslog message version {0} not implemented !", version));
                    }

                    return ParseRFC5424(data);
                }
            }
            else
            {
                return ParseRFC3164(data);
            }
            throw new FormatException();
        }

        /// <summary>
        /// Gets / sets the maximum message length used.
        /// </summary>
        public int MaximumMessageLength => GetMaximumMessageLength(Version);

        /// <summary>
        /// Initializes a new instance of the <see cref="SyslogMessage"/> struct.
        /// </summary>
        /// <param name="version">Used SyslogVersion (used at ToString() methods).</param>
        /// <param name="facility"><see cref="SyslogFacility"/>.</param>
        /// <param name="severity"><see cref="SyslogSeverity"/>.</param>
        /// <param name="timeStamp">The time stamp this message was created.</param>
        /// <param name="hostName">The server the message belongs to.</param>
        /// <param name="processName">The process name the message belongs to.</param>
        /// <param name="processID">The process ID the message belongs to.</param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="content">The content of the message.</param>
        /// <param name="data">The structured syslog data.</param>
        public SyslogMessage(SyslogMessageVersion version, SyslogFacility facility, SyslogSeverity severity, SyslogMessageDateTime timeStamp, string hostName, string processName, uint processID, string messageID, string content, SyslogStructuredData data)
        {
            ID = -1;
            if (string.IsNullOrEmpty(hostName))
            {
                hostName = Environment.MachineName.ToLower();
            }
            {
                int index = hostName.IndexOf('.');
                if (index > -1)
                {
                    hostName = hostName.Substring(0, index);
                }
            }

            if (string.IsNullOrEmpty(processName))
            {
                processName = Process.GetCurrentProcess().MainModule.ModuleName;
                if (processID == 0)
                {
                    processID = (uint)Process.GetCurrentProcess().Id;
                }
            }
            if (timeStamp == null)
            {
                timeStamp = DateTime.Now;
            }

            Version = version;
            Facility = facility;
            Severity = severity;
            TimeStamp = timeStamp;
            HostName = hostName;
            ProcessName = processName;
            ProcessID = processID;
            MessageID = messageID;
            Content = content;
            StructuredData = data;
        }

        /// <summary>
        /// ID of the message.
        /// </summary>
        [Field(Flags = FieldFlags.ID)]
        public long ID;

        /// <summary>
        /// Syslog protocol version.
        /// </summary>
        [Field]
        public SyslogMessageVersion Version;

        /// <summary>
        /// The syslog facility.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public SyslogFacility Facility;

        /// <summary>
        /// The syslog severity.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public SyslogSeverity Severity;

        /// <summary>
        /// Provides access to the time stamp.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public SyslogMessageDateTime TimeStamp;

        /// <summary>
        /// The server this message belongs to.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public string HostName;

        /// <summary>
        /// The process this messages belongs to.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public string ProcessName;

        /// <summary>
        /// The process id this messages belongs to.
        /// </summary>
        [Field(Flags = FieldFlags.Index)]
        public uint ProcessID;

        /// <summary>
        /// The structured syslog data.
        /// </summary>
        [Field]
        public SyslogStructuredData StructuredData;

        /// <summary>
        /// The content of the message.
        /// </summary>
        [Field]
        public string Content;

        /// <summary>
        /// Senders ID of the message (do not use this as uuid!), this is only present for SyslogMessageVersion.RFC5424.
        /// </summary>
        [Field]
        public string MessageID;

        /// <summary>Retrieves the syslog items encoded syslog string.</summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// ProcessName
        /// or
        /// Hostname
        /// or
        /// Content.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// </exception>
        public string ToStringRFC3164()
        {
            if (ProcessName == null)
            {
                throw new InvalidDataException("Missing ProcessName");
            }

            if (HostName == null)
            {
                throw new InvalidDataException("Missing Hostname");
            }

            if (Content == null)
            {
                throw new InvalidDataException("Missing Content");
            }

            if (MessageID != null)
            {
                throw new NotSupportedException(string.Format("{0} is not supported within RFC 3164!", MessageID));
            }

            if (StructuredData != null)
            {
                throw new NotSupportedException(string.Format("{0} is not supported within RFC 3164!", StructuredData));
            }

            if (TimeStamp.Value.Year != DateTime.Now.Year)
            {
                throw new NotSupportedException(string.Format("Year of message can not be saved within RFC 3164!"));
            }

            if (ProcessName.HasInvalidChars(ASCII.Strings.Printable))
            {
                throw new InvalidDataException(string.Format("{0} may only contain us-ascii characters!", "ProcessName"));
            }
            if (HostName.HasInvalidChars(ASCII.Strings.Printable))
            {
                throw new InvalidDataException(string.Format("{0} may only contain us-ascii characters!", "Hostname"));
            }
            if (Content.HasInvalidChars(ASCII.Strings.Printable))
            {
                throw new InvalidDataException(string.Format("{0} may only contain us-ascii characters!", "Content"));
            }

            var stringBuilder = new StringBuilder();

            // PRI: <PRI>
            stringBuilder.Append("<");
            stringBuilder.Append(((int)Facility * 8) + (int)Severity);
            stringBuilder.Append(">");

            // HEADER: timestamp hostname
            stringBuilder.Append(TimeStamp.ToStringRFC3164());
            stringBuilder.Append(' ');
            stringBuilder.Append(HostName);
            stringBuilder.Append(' ');

            // MSG Process: Message
            if (ProcessName.Length > 0)
            {
                stringBuilder.Append(ProcessName);
                if (ProcessID != 0)
                {
                    stringBuilder.Append("[" + ProcessID + "]");
                }
                stringBuilder.Append(": ");
            }
            stringBuilder.Append(Content);
            if (stringBuilder.Length > 2048)
            {
                throw new InvalidDataException(string.Format("Message length exceeds MaximumLength!"));
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the syslog items encoded syslog string.
        /// </summary>
        /// <returns></returns>
        public string ToStringRFC5424()
        {
            var stringBuilder = new StringBuilder();

            // PRI: <PRI>Version
            stringBuilder.Append("<");
            stringBuilder.Append(((int)Facility * 8) + (int)Severity);
            stringBuilder.Append(">1 ");

            // TIMESTAMP
            stringBuilder.Append(TimeStamp.ToString());
            stringBuilder.Append(' ');

            // HOSTNAME
            if (string.IsNullOrEmpty(HostName))
            {
                stringBuilder.Append('-');
            }
            else
            {
                stringBuilder.Append(HostName);
            }

            // APP-NAME
            stringBuilder.Append(' ');
            if (string.IsNullOrEmpty(ProcessName))
            {
                stringBuilder.Append('-');
            }
            else
            {
                stringBuilder.Append(ProcessName);
            }

            // PROCID
            stringBuilder.Append(' ');
            if (ProcessID == 0)
            {
                stringBuilder.Append('-');
            }
            else
            {
                stringBuilder.Append(ProcessID.ToString());
            }

            // MSGID
            stringBuilder.Append(' ');
            if (string.IsNullOrEmpty(MessageID))
            {
                stringBuilder.Append('-');
            }
            else
            {
                stringBuilder.Append(MessageID);
            }

            // STRUCTURED-DATA
            stringBuilder.Append(' ');
            string valueuredDataString = StructuredData?.ToString();
            if (valueuredDataString == null)
            {
                stringBuilder.Append('-');
            }
            else
            {
                stringBuilder.Append(valueuredDataString);
            }

            if (Content != null)
            {
                stringBuilder.Append(' ');
                if (!ASCII.IsClean(Content))
                {
                    stringBuilder.Append(ASCII.Strings.UTF8BOM);
                }
                stringBuilder.Append(Content);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the syslog items encoded syslog string.
        /// </summary>
        /// <returns></returns>
        public string ToStringRSYSLOG()
        {
            if (ProcessName == null)
            {
                throw new InvalidDataException("Missing ProcessName");
            }

            if (HostName == null)
            {
                throw new InvalidDataException("Missing Hostname");
            }

            if (Content == null)
            {
                throw new InvalidDataException("Missing Content");
            }

            if (MessageID != null)
            {
                throw new NotSupportedException(string.Format("{0} is not supported within RSYSLOG!", MessageID));
            }

            if (ProcessName.HasInvalidChars(ASCII.Strings.Printable))
            {
                throw new InvalidDataException(string.Format("{0} may only contain us-ascii characters!", "ProcessName"));
            }
            if (HostName.HasInvalidChars(ASCII.Strings.Printable))
            {
                throw new InvalidDataException(string.Format("{0} may only contain us-ascii characters!", "Hostname"));
            }

            var stringBuilder = new StringBuilder();

            // PRI: <PRI>
            stringBuilder.Append("<");
            stringBuilder.Append(((int)Facility * 8) + (int)Severity);
            stringBuilder.Append(">");

            // TIMESTAMP
            stringBuilder.Append(TimeStamp.ToString());
            stringBuilder.Append(' ');

            // HOSTNAME
            stringBuilder.Append(HostName);
            stringBuilder.Append(' ');

            // MSG Process: Message
            if (ProcessName.Length > 0)
            {
                stringBuilder.Append(ProcessName);
                if (ProcessID != 0)
                {
                    stringBuilder.Append("[" + ProcessID + "]");
                }
                stringBuilder.Append(": ");
            }

            string dataString = StructuredData?.ToString();
            if (dataString != null)
            {
                stringBuilder.Append(dataString);
                stringBuilder.Append(" ");
            }
            stringBuilder.Append(Content);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the syslog items encoded syslog string.
        /// </summary>
        public override string ToString()
        {
            switch (Version)
            {
                case SyslogMessageVersion.RFC3164: return ToStringRFC3164();
                case SyslogMessageVersion.RSYSLOG: return ToStringRSYSLOG();
                default:
                case SyslogMessageVersion.RFC5424: return ToStringRFC5424();
            }
        }

        #region IComparable Member

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows or occurs in the same position in the sort order as the other object.
        /// </summary>
        public int CompareTo(object obj)
        {
            return obj is SyslogMessage msg ? Equals(msg) ? 0 : TimeStamp.CompareTo(msg.TimeStamp) : ToString().CompareTo(obj?.ToString());
        }

        #endregion

        #region IEquatable Member

        /// <summary>
        /// Determines whether the specified Object is equal to the SyslogItem.
        /// </summary>
        /// <param name="obj">Object to test for equality.</param>
        public override bool Equals(object obj) => obj is SyslogMessage msg ? msg.ToString() == ToString() : false;

        #endregion

        /// <summary>
        /// Obtains the hash code for this item.
        /// </summary>
        /// <returns>Returns a hash code.</returns>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
