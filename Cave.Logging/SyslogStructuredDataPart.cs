using Cave.Collections;
using Cave.Console;
using System;
using System.IO;
using System.Text;

namespace Cave.Logging
{
    /// <summary>
    /// Provides structured data encoding / decoding for syslog messages according to RFC 5424
    /// </summary>
    public class SyslogStructuredDataPart
    {
        Option[] m_Items;

        /// <summary>
        /// Invalid characters that have to be escaped
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static char[] InvalidChars => new char[] { '"', '\\', ']' };

        /// <summary>
        /// Parses a structured data string and returns a new SyslogStructuredData instance.
        /// (This function does not parse an array of multiple structured data instances!)
        /// </summary>
        /// <param name="text">A string containing structured data.</param>
        /// <returns></returns>
        public static SyslogStructuredDataPart Parse(string text)
        {
            string content = StringExtensions.Unbox(text, "[", "]", true);
            Arguments arguments = Arguments.FromString(Arguments.ParseOptions.ContainsCommand, content);
            if (arguments.Parameters.Count > 0)
            {
                throw new InvalidDataException(string.Format("Invalid structured data!"));
            }

            SyslogStructuredDataPart result = new SyslogStructuredDataPart(arguments.Command, arguments.Options.ToArray());
            return result;
        }

        /// <summary>
        /// Creates a new SyslogStructuredDataPart with the specified name and elements
        /// </summary>
        /// <param name="name">The name of the structured data part</param>
        /// <param name="items">The items of the data</param>
        public SyslogStructuredDataPart(string name, params Option[] items)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
            m_Items = items;
        }

        /// <summary>
        /// Provides access to the name of the instance
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Provides access to all options
        /// </summary>
        public Option[] Items => (Option[])m_Items.Clone();

        /// <summary>
        /// Obtains the encoded string of this instances name and data.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if ((Name == null) || (Items == null))
            {
                return "[Invalid]";
            }

            if (!ASCII.IsClean(Name) || (Name.IndexOf(' ') >= 0))
            {
                return "[InvalidName]";
            }

            StringBuilder result = new StringBuilder();
            result.Append('[');
            result.Append(Name);
            foreach (Option l_Element in Items)
            {
                result.Append(' ');
                if (!ASCII.IsClean(l_Element.Name) || (l_Element.Name.IndexOf(' ') >= 0))
                {
                    result.Append("InvalidElementName");
                }
                else
                {
                    result.Append(l_Element.Name);
                }
                result.Append("=\"");
                string value = l_Element.Value == null ? "" : l_Element.Value;
                value = value.ReplaceChars(InvalidChars, "");
                result.Append(value);
                result.Append("\"");
            }
            result.Append(']');
            return result.ToString();
        }
    }
}
