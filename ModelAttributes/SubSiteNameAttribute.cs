using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class SubSiteNameAttribute : Attribute
    {
        public string SubSiteName { get; set; }

        public SubSiteNameAttribute(string subsiteName)
        {
            SubSiteName = subsiteName;
        }
    }
}
