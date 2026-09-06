using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuCoderef
    {
        public string RefCodetype { get; set; }
        public string RefCode { get; set; }
        public string RefName { get; set; }
        public string RefDes { get; set; }
        public string RefId { get; set; }
        public string RefCasetype { get; set; }
        public int? RefShowseq { get; set; }
        public string RefDes2 { get; set; }
        public string ModifyId { get; set; }
        public DateTime? ModifyTime { get; set; }
        /// <summary>
        /// 是否預設啟用
        /// </summary>
        public string RefDefaultFlag { get; set; }
    }
}
