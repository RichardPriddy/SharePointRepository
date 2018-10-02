using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class DownloadableContentAttribute : Attribute
    {
        public DownloadableContentAttribute()
        { }
    }
}