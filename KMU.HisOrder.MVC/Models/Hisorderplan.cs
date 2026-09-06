using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class Hisorderplan
    {
        public Hisorderplan()
        {
            HisorderplanAttrs = new HashSet<HisorderplanAttr>();
        }

        public long Orderplanid { get; set; }
        public string Inhospid { get; set; }
        public string HealthId { get; set; }
        public string HplanType { get; set; }
        public short SeqNo { get; set; }
        public string PlanCode { get; set; }
        public string PlanDes { get; set; }
        public char? FreeCharge { get; set; }
        public DateTime? ExecDateFrom { get; set; }
        public DateTime? ExecDateTo { get; set; }
        public string OrderDept { get; set; }
        public string OrderDr { get; set; }
        public short? PlanDays { get; set; }
        public decimal? QtyDose { get; set; }
        public decimal? QtyDaily { get; set; }
        public string UnitDose { get; set; }
        public string FreqCode { get; set; }
        public string DoseIndication { get; set; }
        public string DosePath { get; set; }
        public string MadeType { get; set; }
        public decimal? TotalQty { get; set; }
        public string ExamLoc { get; set; }
        public char? UrgFlag { get; set; }
        public char? PreopFlag { get; set; }
        public char? AddFlag { get; set; }
        public char? KeepspecFlag { get; set; }
        public string LocationCode { get; set; }
        public string TriggerTablecode { get; set; }
        public long? TriggerRecid { get; set; }
        public char? DcStatus { get; set; }
        public char Status { get; set; }
        public char? ExecStatus { get; set; }
        public char? ChargeStatus { get; set; }
        public string CreateUser { get; set; }
        public string DcUser { get; set; }
        public string ModifyUser { get; set; }
        public string PrintUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? DcDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public DateTime? PrintDate { get; set; }
        public string Remark { get; set; }
        public short? MedBag { get; set; }

        public virtual ICollection<HisorderplanAttr> HisorderplanAttrs { get; set; }
    }
}
