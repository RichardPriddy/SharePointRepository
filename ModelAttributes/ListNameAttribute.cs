using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ListNameAttribute : Attribute
    {
        public string ListName { get; set; }

        public ListNameAttribute(string listName)
        {
            ListName = listName;
        }
    }
}
