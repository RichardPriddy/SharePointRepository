using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ListNameAttribute : Attribute
    {
        public string ListName { get; set; }
        public string Language { get; set; }

        public ListNameAttribute(string listName)
        {
            Language = "*";
            ListName = listName;
        }

        public ListNameAttribute(string language, string listName)
        {
            Language = language;
            ListName = listName;
        }
    }
}
