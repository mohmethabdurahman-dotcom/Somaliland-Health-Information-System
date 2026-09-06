using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class TransactionFee
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int TransationId { get; set; }
        /// <summary>
        /// 交易時間
        /// </summary>
        public DateTime TransactionTime { get; set; }
        /// <summary>
        /// 就醫序號
        /// </summary>
        public long? Inhospid { get; set; }
        /// <summary>
        /// 收費項目
        /// </summary>
        public string FeeType { get; set; }
        /// <summary>
        /// 是否已收費
        /// </summary>
        public string FeePaidFlag { get; set; }
        /// <summary>
        /// 收費金額
        /// </summary>
        public int? FeePaidMoney { get; set; }
        public string ModifyUser { get; set; }
    }
}
