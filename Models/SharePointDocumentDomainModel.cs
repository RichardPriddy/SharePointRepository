using Amt.SharePoint.Integration.ModelAttributes;
using System.ComponentModel.DataAnnotations;

namespace Amt.SharePoint.Integration.Models
{
    public class SharePointDocumentDomainModel : SharePointDomainModel
    {
        [Display(Name = "File Link")]
        [ColumnName("FileRef")]
        public string FileRef { get; set; }
    }
}