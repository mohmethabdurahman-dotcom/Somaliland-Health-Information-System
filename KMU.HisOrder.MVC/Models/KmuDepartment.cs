using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuDepartment
    {
        public string DptCode { get; set; }
        public string DptName { get; set; }
        public string DptCategory { get; set; }
        public long? DptDepth { get; set; }
        public string DptStatus { get; set; }
        public string DptRemark { get; set; }
        /// <summary>
        /// 預設身分別
        /// </summary>
        public string DptDefaultAttr { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
        public string DptParent { get; set; }
    }
}
