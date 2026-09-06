using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    /// <summary>
    /// Auth change log
    /// </summary>
    public partial class KmuAuthsLog
    {
        public string UserIdno { get; set; }
        public string EditType { get; set; }
        public string ProjectId { get; set; }
        public DateTime EditTime { get; set; }
        public string EditUser { get; set; }
    }
}
