using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amt.SharePoint.Integration.Models
{
    public class User : SharePointDomainModel
    {
        public string Email { get; set; }
        public string LoginName { get; set; }
    }
}
