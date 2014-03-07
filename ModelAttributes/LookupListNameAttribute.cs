using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class LookupListNameAttribute : Attribute
    {
        public string LookupListName { get; set; }

        public LookupListNameAttribute(string lookupListName)
        {
            LookupListName = lookupListName;
        }
    }
}