using System;

namespace Amt.SharePoint.Integration.ModelAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ColumnNameAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public ColumnNameAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}