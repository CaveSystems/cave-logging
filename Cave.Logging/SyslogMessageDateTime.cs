using System;
using System.Globalization;
using System.Text;

namespace Cave.Logging
{
    /// <summary>
    /// Provides available syslog message versions.
    /// </summary>
    public class SyslogMessageDateTime : IEquatable<SyslogMessageDateTime>, IComparable<SyslogMessageDateTime>, IComparable
    {
        static readonly string[] DateTimeFormats = new string[]
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffzzz",
        };

        /// <summary>Implements the operator ==.</summary>
        /// <param name="dateTime1">The date time1.</param>
        /// <param name="dateTime2">The date time2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(SyslogMessageDateTime dateTime1, SyslogMessageDateTime dateTime2)
        {
            if (ReferenceEquals(dateTime1, null))
            {
                return ReferenceEquals(dateTime2, null);
            }

            if (ReferenceEquals(dateTime2, null))
            {
                return false;
            }

            return dateTime1.Value == dateTime2.Value;
        }

        /// <summary>Implements the operator !=.</summary>
        /// <param name="dateTime1">The date time1.</param>
        /// <param name="dateTime2">The date time2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(SyslogMessageDateTime dateTime1, SyslogMessageDateTime dateTime2)
        {
            if (ReferenceEquals(dateTime1, null))
            {
                return !ReferenceEquals(dateTime2, null);
            }

            if (ReferenceEquals(dateTime2, null))
            {
                return true;
            }

            return dateTime1.Value != dateTime2.Value;
        }

        /// <summary>Implements the operator &lt;.</summary>
        /// <param name="dateTime1">The date time1.</param>
        /// <param name="dateTime2">The date time2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(SyslogMessageDateTime dateTime1, SyslogMessageDateTime dateTime2)
        {
            if (ReferenceEquals(dateTime1, null))
            {
                return true;
            }

            if (ReferenceEquals(dateTime2, null))
            {
                return false;
            }

            return dateTime1.Value < dateTime2.Value;
        }

        /// <summary>Implements the operator &gt;.</summary>
        /// <param name="dateTime1">The date time1.</param>
        /// <param name="dateTime2">The date time2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(SyslogMessageDateTime dateTime1, SyslogMessageDateTime dateTime2)
        {
            if (ReferenceEquals(dateTime1, null))
            {
                return false;
            }

            if (ReferenceEquals(dateTime2, null))
            {
                return true;
            }

            return dateTime1.Value > dateTime2.Value;
        }

        /// <summary>
        /// Allows implicit conversion from <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The DateTime value.</param>
        /// <returns>Returns a new SyslogMessageDateTime instance.</returns>
        public static implicit operator SyslogMessageDateTime(DateTime dateTime)
        {
            return new SyslogMessageDateTime(dateTime);
        }

        /// <summary>
        /// Allows implicit conversion from <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The DateTimeOffset value.</param>
        /// <returns>Returns a new SyslogMessageDateTime instance.</returns>
        public static implicit operator SyslogMessageDateTime(DateTimeOffset dateTime)
        {
            return new SyslogMessageDateTime(dateTime);
        }

        /// <summary>
        /// Parses a SyslogMessageDateTime value according to RFC3164 from the specified string.
        /// </summary>
        /// <param name="text">The string containing the RFC3164 formatted datetime value.</param>
        /// <returns>Returns a new SyslogMessageDateTime instance.</returns>
        public static SyslogMessageDateTime ParseRFC3164(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (text.Length < 14)
            {
                throw new ArgumentException("text");
            }

            string str = (text.Length == 14) ? text : text.Substring(0, 15).TrimEnd(' ');
            return new SyslogMessageDateTime(DateTime.ParseExact(str, "MMM d HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal));
        }

        /// <summary>
        /// Parses a SyslogMessageDateTime value according to RFC5424 from the specified string.
        /// </summary>
        /// <param name="text">The string containing the RFC5424 formatted datetime value.</param>
        /// <returns>Returns a new SyslogMessageDateTime instance.</returns>
        public static SyslogMessageDateTime ParseRFC5424(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            int index = text.IndexOf(' ');
            string str = (index == -1) ? text : text.Substring(0, index);
            if (str.Length < 19)
            {
                throw new ArgumentException("text");
            }

            DateTimeOffset value = DateTimeOffset.ParseExact(str, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            return new SyslogMessageDateTime(value);
        }

        /// <summary>
        /// Parses a SyslogMessageDateTime value from the specified string. The format of the datetime may be RFC3164 or RFC5424.
        /// </summary>
        /// <param name="text">The string containing the formatted datetime value.</param>
        /// <returns>Returns a new SyslogMessageDateTime instance.</returns>
        public static SyslogMessageDateTime Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (text.Length < 15)
            {
                throw new ArgumentException("text");
            }

            if (text[11] == 'T')
            {
                return ParseRFC5424(text);
            }

            return ParseRFC3164(text);
        }

        /// <summary>
        /// Creates a new SyslogMessageDateTime instance with the current date and time.
        /// </summary>
        public SyslogMessageDateTime()
        {
            Value = DateTimeOffset.Now;
        }

        /// <summary>
        /// Creates a new SyslogMessageDateTime from the specified local datetime value.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="ms"></param>
        /// <param name="ns"></param>
        public SyslogMessageDateTime(int year, int month, int day, int hour, int minute, int second, int ms, int ns)
        {
            if (ms < 0 || ms > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(ms));
            }

            if (ns < 0 || ns > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(ns));
            }

            Value = new DateTimeOffset(new DateTime(year, month, day, hour, minute, second) + new TimeSpan(10L * (ns + (ms * 1000L))));
        }

        /// <summary>
        /// Creates a new SyslogMessageDateTime from the specified local datetime value.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="ms"></param>
        /// <param name="ns"></param>
        /// <param name="offset"></param>
        public SyslogMessageDateTime(int year, int month, int day, int hour, int minute, int second, int ms, int ns, TimeSpan offset)
        {
            if (ms < 0 || ms > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(ms));
            }

            if (ns < 0 || ns > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(ns));
            }

            Value = new DateTimeOffset(new DateTime(year, month, day, hour, minute, second) + new TimeSpan(10L * (ns + (ms * 1000L))), offset);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyslogMessageDateTime"/> class.
        /// </summary>
        /// <param name="localDateTime">The local datetime.</param>
        public SyslogMessageDateTime(DateTime localDateTime)
        {
            Value = new DateTimeOffset(localDateTime);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyslogMessageDateTime"/> class.
        /// </summary>
        /// <param name="dateTime">The date and time.</param>
        /// <param name="offset">The offset.</param>
        public SyslogMessageDateTime(DateTime dateTime, TimeSpan offset)
        {
            Value = new DateTimeOffset(dateTime, offset);
        }

        /// <summary>
        /// Creates a new SyslogMessageDateTime using the specified date, time and offset.
        /// </summary>
        /// <param name="dateTime">The date, time and offset.</param>
        public SyslogMessageDateTime(DateTimeOffset dateTime)
        {
            Value = dateTime;
        }

        /// <summary>
        /// Obtains the DateTime value of this instance converted to the Universal Coordinated Time (Zulu).
        /// </summary>
        public DateTime Utc => Value.UtcDateTime;

        /// <summary>
        /// Obtains the DateTime value of this instance converted to the local time.
        /// </summary>
        public DateTime Local => Value.LocalDateTime;

        /// <summary>
        /// Obtains the source value of this instance.
        /// </summary>
        public DateTimeOffset Value { get; private set; }

        /// <summary>
        /// Obtains the RFC3164 string representation of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToStringRFC3164()
        {
            return Local.ToString("MMM dd HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Obtains the RFC5424 string representation of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToStringRFC5424()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo));
            long l_Ticks = Value.Ticks % TimeSpan.TicksPerSecond;

            // try to keep the date field as short as possible
            if (l_Ticks != 0)
            {
                // we got milli seconds
                if (l_Ticks % 10000 != 0)
                {
                    // we got micro seconds
                    stringBuilder.Append(Value.ToString("'.'ffffff"));
                }
                else
                {
                    stringBuilder.Append(Value.ToString("'.'fff"));
                }
            }
            if (Value.Offset == TimeSpan.Zero)
            {
                stringBuilder.Append("Z");
            }
            else if (Value.Offset > TimeSpan.Zero)
            {
                stringBuilder.AppendFormat("+{0:00}:{1:00}", Value.Offset.Hours, Value.Offset.Minutes);
            }
            else
            {
                stringBuilder.AppendFormat("{0:00}:{1:00}", Value.Offset.Hours, Value.Offset.Minutes);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Obtains the <see cref="ToStringRFC5424()"/> result.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToStringRFC5424();
        }

        /// <summary>
        /// Obtains the hash code for the <see cref="Value"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Determines whether a SyslogMessageDateTime object represents the same point in time as a specified object.
        /// </summary>
        /// <param name="obj">The value to compare to the current SyslogMessageDateTime object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SyslogMessageDateTime);
        }

        /// <summary>
        /// Determines whether a SyslogMessageDateTime object represents the same point in time as a specified object.
        /// </summary>
        /// <param name="other">The value to compare to the current SyslogMessageDateTime object.</param>
        /// <returns></returns>
        public bool Equals(SyslogMessageDateTime other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Value.Equals(other.Value);
        }

        /// <summary>
        /// Compares the current SyslogMessageDateTime object to a specified SyslogMessageDateTime object and indicates whether the current object is earlier than,
        /// the same as, or later than the specified one.
        /// </summary>
        /// <param name="other">An object to compare with the current SyslogMessageDateTime object.</param>
        /// <returns></returns>
        public int CompareTo(SyslogMessageDateTime other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            return Value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares the current SyslogMessageDateTime object to a specified SyslogMessageDateTime object and indicates whether the current object is earlier than,
        /// the same as, or later than the specified one.
        /// </summary>
        /// <param name="obj">An object to compare with the current SyslogMessageDateTime object.</param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            SyslogMessageDateTime other = obj as SyslogMessageDateTime;
            if (other == null)
            {
                return -1;
            }

            return CompareTo(other);
        }
    }
}
