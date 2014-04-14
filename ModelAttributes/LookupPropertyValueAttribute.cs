using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class LookupPropertyValueAttribute : Attribute
    {
        public bool IsLookupProperty { get; set; }

        public LookupPropertyValueAttribute(bool isLookupProperty)
        {
            IsLookupProperty = isLookupProperty;
        }
    }
}