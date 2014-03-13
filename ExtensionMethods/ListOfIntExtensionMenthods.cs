using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Amt.SharePoint.Integration.ExtensionMethods
{
    public static class ListOfIntExtensionMenthods
    {
        public static string ToCamlQuery(this IEnumerable<int> ids, string type)
        {
            var query = "<View><Query><Where>{0}</Where></Query></View>";

            var groups = ids.Select(id => "<Eq>" +
                                                 "   <FieldRef Name='ID' />" +
                                                 "   <Value Type='Counter'>" + id + "</Value>" +
                                                 "</Eq>").ToList();

            if (groups.Count != 1)
            {
                while (groups.Count > 1)
                {
                    groups = CreateOrStatement(groups, type);
                }
            }

            query = string.Format(query, groups[0]);
            return FormatXml(query);
        }

        private static List<string> CreateOrStatement(IEnumerable<string> eqs, string type)
        {
            var groups = eqs.Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 2)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            return groups
                    .Select(@group => @group.Count == 1
                                        ? @group[0]
                                        : string.Format("<{0}>{1}{2}</{0}>", type, @group[0], @group[1]))
                    .ToList();
        }

        public static String FormatXml(String xml)
        {
            var mStream = new MemoryStream();
            var document = new XmlDocument();
            var writer = new XmlTextWriter(mStream, Encoding.Unicode)
            {
                Formatting = Formatting.Indented
            };

            try
            {
                document.LoadXml(xml);
                document.WriteContentTo(writer);

                writer.Flush();

                mStream.Flush();
                mStream.Position = 0;

                var sReader = new StreamReader(mStream);

                return sReader.ReadToEnd();
            }
            catch (XmlException)
            {
                return string.Empty;
            }
            finally
            {
                mStream.Close();
                writer.Close();
            }
        }
    }
}
