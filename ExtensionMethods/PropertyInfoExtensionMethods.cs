using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amt.SharePoint.Integration.ModelAttributes;

namespace Amt.SharePoint.Integration.ExtensionMethods
{
    internal static class PropertyInfoExtensionMethods
    {
        public static string PropertyName(this PropertyInfo propInfo)
        {
            var attribute = propInfo.GetCustomAttribute<ColumnNameAttribute>();

            var name = attribute == null
                ? propInfo.Name
                : EncodeToInternalField(attribute.ColumnName);

            return name.Length > 32
                ? name.Substring(0, 32)
                : name;
        }

        /// <summary>
        /// Thanks to: http://www.n8d.at/blog/encode-and-decode-field-names-from-display-name-to-internal-name/
        /// </summary>
        private static string EncodeToInternalField(string toEncode)
        {
            if (toEncode == null) return null;

            var encodedString = new StringBuilder();

            foreach (var chr in toEncode)
            {
                var encodedChar = HttpUtility.UrlEncode(chr.ToString(CultureInfo.InvariantCulture));

                if (encodedChar != null && encodedChar.StartsWith("%"))
                {
                    encodedChar = encodedChar.Replace("u", "x");
                    encodedChar = encodedChar.Substring(1, encodedChar.Length - 1);
                    encodedChar = String.Format("_{0}_", encodedChar);
                    encodedString.Append(encodedChar);
                }
                else switch (encodedChar)
                    {
                        case " ":
                        case "+":
                            encodedString.Append("_x0020_");
                            break;
                        case ".":
                            encodedString.Append("_x002e_");
                            break;
                        default:
                            encodedString.Append(chr);
                            break;
                    }

            }
            return encodedString.ToString();
        }
    }
}
