using System;
using System.IO;
using System.Text;
using Cave.Collections;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Provides structured data encoding / decoding for syslog messages according to RFC 5424.</summary>
public class SyslogStructuredDataPart
{
    #region Static

    /// <summary>Gets invalid characters that have to be escaped.</summary>
    public static char[] InvalidChars =>
        new[]
        {
            '"',
            '\\',
            ']'
        };

    /// <summary>
    /// Parses a structured data string and returns a new SyslogStructuredData instance. (This function does not parse an array of multiple structured data instances!).
    /// </summary>
    /// <param name="text">A string containing structured data.</param>
    /// <returns></returns>
    public static SyslogStructuredDataPart Parse(string text)
    {
        var content = text.Unbox("[", "]");
        var arguments = Arguments.FromString(Arguments.ParseOptions.ContainsCommand, content);
        if (arguments.Parameters.Count > 0)
        {
            throw new InvalidDataException("Invalid structured data!");
        }

        var result = new SyslogStructuredDataPart(content);
        return result;
    }

    #endregion Static

    #region Private Fields

    readonly Option[] items;

    #endregion Private Fields

    #region Public Fields

    /// <summary>Provides access to the name of the instance.</summary>
    public readonly string Name;

    #endregion Public Fields

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="SyslogStructuredDataPart"/> class.</summary>
    /// <param name="name">The name of the structured data part.</param>
    /// <param name="items">The items of the data.</param>
    public SyslogStructuredDataPart(string name, params Option[] items)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
        this.items = items;
    }

    #endregion Constructors

    #region Properties

    /// <summary>Gets a copy of all options.</summary>
    public Option[] Items => (Option[])items.Clone();

    #endregion Properties

    #region Overrides

    /// <summary>Obtains the encoded string of this instances name and data.</summary>
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

        var result = new StringBuilder();
        result.Append('[');
        result.Append(Name);
        foreach (var item in Items)
        {
            result.Append(' ');
            if (!ASCII.IsClean(item.Name) || (item.Name.IndexOf(' ') >= 0))
            {
                result.Append("InvalidElementName");
            }
            else
            {
                result.Append(item.Name);
            }

            result.Append("=\"");
            var value = item.Value ?? string.Empty;
            value = value.ReplaceChars(InvalidChars, string.Empty);
            result.Append(value);
            result.Append('"');
        }

        result.Append(']');
        return result.ToString();
    }

    #endregion Overrides
}