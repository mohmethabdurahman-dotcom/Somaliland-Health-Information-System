using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    /// <summary>
    /// User Auth File(Account permissions)
    /// </summary>
    public partial class KmuAuth
    {
        public string UserIdno { get; set; }
        public string ProjectId { get; set; }
        public string Creator { get; set; }
        public DateTime CreateTime { get; set; }

        public virtual KmuUser UserIdnoNavigation { get; set; }
    }
}
