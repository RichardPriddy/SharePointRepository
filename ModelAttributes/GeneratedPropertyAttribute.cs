using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class GeneratedPropertyAttribute : Attribute
    {
        public bool IsPropertyGenerated { get; set; }

        public GeneratedPropertyAttribute(bool isPropertyGenerated)
        {
            IsPropertyGenerated = isPropertyGenerated;
        }
    }
}