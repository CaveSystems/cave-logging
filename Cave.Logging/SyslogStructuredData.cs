using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cave.Logging
{
    /// <summary>
    /// Provides structured data encoding / decoding for syslog messages according to RFC 5424.
    /// </summary>
    public class SyslogStructuredData : ICollection<SyslogStructuredDataPart>, ICollection<string>
    {
        /// <summary>
        /// Parses an array of structured data instances.
        /// </summary>
        /// <param name="text">The string containing one or more structured data instances.</param>
        /// <returns></returns>
        public static SyslogStructuredData Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var result = new List<SyslogStructuredDataPart>();
            int start = 0;
            while (true)
            {
                int l_End = text.IndexOf(']', start);
                if (l_End > -1)
                {
                    result.Add(SyslogStructuredDataPart.Parse(text.Substring(start, l_End - start + 1)));
                    start = l_End + 1;
                }
                else
                {
                    break;
                }
            }
            if (start < text.Length)
            {
                result.Add(SyslogStructuredDataPart.Parse(text.Substring(start)));
            }
            return new SyslogStructuredData(result);
        }

        readonly Dictionary<string, SyslogStructuredDataPart> data = new Dictionary<string, SyslogStructuredDataPart>();
        readonly List<string> names = new List<string>();

        /// <summary>
        /// Creates a new empty SyslogStructuredData instance.
        /// </summary>
        public SyslogStructuredData()
        {
        }

        /// <summary>
        /// Creates a new SyslogStructuredData instance with the specified parts.
        /// </summary>
        /// <param name="items"><see cref="SyslogStructuredDataPart"/>.</param>
        public SyslogStructuredData(IEnumerable<SyslogStructuredDataPart> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            foreach (SyslogStructuredDataPart part in items)
            {
                Add(part);
            }
        }

        /// <summary>
        /// Obtains the encoded string of this instances name and data.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Count == 0)
            {
                return null;
            }

            var result = new StringBuilder();
            foreach (SyslogStructuredDataPart part in this)
            {
                result.Append(part.ToString());
            }
            return result.ToString();
        }

        #region ICollection<string> implementation

        /// <summary>
        /// Adds a new empty structured data part with the specified name.
        /// </summary>
        /// <param name="item">The name of the <see cref="SyslogStructuredDataPart"/>.</param>
        public void Add(string item)
        {
            Add(new SyslogStructuredDataPart(item));
        }

        /// <summary>
        /// Checks whether a structured data part with the specified name exists.
        /// </summary>
        /// <param name="item">The name of the <see cref="SyslogStructuredDataPart"/>.</param>
        /// <returns></returns>
        public bool Contains(string item)
        {
            return data.ContainsKey(item);
        }

        /// <summary>
        /// Copies all <see cref="SyslogStructuredDataPart"/> names to the specified array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(string[] array, int arrayIndex)
        {
            data.Keys.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is readonly. This always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes a <see cref="SyslogStructuredDataPart"/> by name.
        /// </summary>
        /// <param name="item">The name of the <see cref="SyslogStructuredDataPart"/>.</param>
        /// <returns></returns>
        public bool Remove(string item)
        {
            names.Remove(item);
            return data.Remove(item);
        }

        /// <summary>
        /// Obtains an enumerator for all <see cref="SyslogStructuredDataPart"/> names.
        /// </summary>
        /// <returns></returns>
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return data.Keys.GetEnumerator();
        }
        #endregion

        #region ICollection<SyslogStructuredPart>

        /// <summary>
        /// Adds a <see cref="SyslogStructuredDataPart"/>.
        /// </summary>
        /// <param name="item"></param>
        public void Add(SyslogStructuredDataPart item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            data.Add(item.Name, item);
            names.Add(item.Name);
        }

        /// <summary>
        /// Removes all <see cref="SyslogStructuredDataPart"/>s.
        /// </summary>
        public void Clear()
        {
            data.Clear();
            names.Clear();
        }

        /// <summary>
        /// Checks whether a specified <see cref="SyslogStructuredDataPart"/> exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(SyslogStructuredDataPart item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return data.ContainsKey(item.Name) && data[item.Name].Equals(item);
        }

        /// <summary>
        /// Copies all <see cref="SyslogStructuredDataPart"/> to the specified array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(SyslogStructuredDataPart[] array, int arrayIndex)
        {
            data.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of <see cref="SyslogStructuredDataPart"/>s present.
        /// </summary>
        public int Count => data.Count;

        /// <summary>
        /// Removes a specified <see cref="SyslogStructuredDataPart"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(SyslogStructuredDataPart item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!Contains(item))
            {
                return false;
            }

            return Remove(item.Name);
        }

        /// <summary>
        /// Obtains an enumerator for all present <see cref="SyslogStructuredDataPart"/>s.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<SyslogStructuredDataPart> GetEnumerator()
        {
            return data.Values.GetEnumerator();
        }

        /// <summary>
        /// Obtains an enumerator for all present <see cref="SyslogStructuredDataPart"/>s.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.Values.GetEnumerator();
        }
        #endregion

        /// <summary>
        /// Obtains the SyslogStructuredDataPart with the specified index.
        /// </summary>
        /// <param name="index">Index of the part.</param>
        /// <returns></returns>
        public SyslogStructuredDataPart this[int index] => this[names[index]];

        /// <summary>
        /// Obtains the SyslogStructuredDataPart with the specified name.
        /// </summary>
        /// <param name="name">Name of the part.</param>
        /// <returns></returns>
        public SyslogStructuredDataPart this[string name] => data[name];

        /// <summary>
        /// Obtains all parts of the instance.
        /// </summary>
        /// <returns></returns>
        public SyslogStructuredDataPart[] ToArray()
        {
            var result = new SyslogStructuredDataPart[Count];
            data.Values.CopyTo(result, 0);
            return result;
        }
    }
}
