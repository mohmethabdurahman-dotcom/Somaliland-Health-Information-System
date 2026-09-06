using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class TransactionCall
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int CallId { get; set; }
        /// <summary>
        /// 看診日
        /// </summary>
        public DateOnly? CallRegDate { get; set; }
        /// <summary>
        /// 看診科
        /// </summary>
        public string CallRegDepartment { get; set; }
        /// <summary>
        /// 午別
        /// </summary>
        public string CallRegNoon { get; set; }
        /// <summary>
        /// 看診號
        /// </summary>
        public short? CallRegSeqNo { get; set; }
        /// <summary>
        /// 病歷號
        /// </summary>
        public string CallPatientId { get; set; }
        /// <summary>
        /// 就醫序號
        /// </summary>
        public long? Inhospid { get; set; }
        /// <summary>
        /// 叫號時間
        /// </summary>
        public DateTime? CallTime { get; set; }
        public string ModifySuer { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
