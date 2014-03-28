using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class LookupPropertyValue : Attribute
    {
        public bool IsLookupProperty { get; set; }

        public LookupPropertyValue(bool isLookupProperty)
        {
            IsLookupProperty = isLookupProperty;
        }
    }
}