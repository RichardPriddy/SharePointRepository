using System;
using Amt.SharePoint.Integration.Models;
using System.ComponentModel.DataAnnotations;
using Amt.SharePoint.Integration.ModelAttributes;

namespace Amt.SharePoint.Integration.Models
{
    public class SharePointDomainModel
    {
        public int ID { get; set; }

        public string Title { get; set; }

        [Display(Name = "Created By")]
        [ColumnName("Author")]
        public User CreatedBy { get; set; }

        [Display(Name = "Modified By")]
        [ColumnName("Editor")]
        public User ModifiedBy { get; set; }

        [Display(Name = "Date Created")]
        [ColumnName("Created")]
        public DateTime Created { get; set; }

        [Display(Name = "Date Modified")]
        [ColumnName("Modified")]
        public DateTime Modified { get; set; }
    }
}