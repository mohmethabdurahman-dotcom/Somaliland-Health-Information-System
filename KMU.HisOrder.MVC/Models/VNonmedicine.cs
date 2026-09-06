using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class VNonmedicine
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string ItemSpec { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public char? Status { get; set; }
        public string CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string Remark { get; set; }
        public decimal? ShowSeq { get; set; }
        public string GroupCode { get; set; }
        public string RefName { get; set; }
        public string RefDes { get; set; }
        public string RefCasetype { get; set; }
        public int? RefShowseq { get; set; }
        public string RefDes2 { get; set; }
        public string ModifyId { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
