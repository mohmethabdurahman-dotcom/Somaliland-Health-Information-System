using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuMedicine
    {
        public string MedId { get; set; }
        /// <summary>
        /// 1-口服
        /// 2-針劑
        /// 3-外用
        /// </summary>
        public string MedType { get; set; }
        public string GenericName { get; set; }
        public string BrandName { get; set; }
        /// <summary>
        /// 醫囑單位
        /// </summary>
        public string UnitSpec { get; set; }
        /// <summary>
        /// 包裝單位(藥局發藥)
        /// </summary>
        public string PackSpec { get; set; }
        /// <summary>
        /// 開立時預設頻次(可空白)
        /// </summary>
        public string DefaultFreq { get; set; }
        /// <summary>
        /// 建議的用藥天數(不用預設)
        /// </summary>
        public string RefDuration { get; set; }
        /// <summary>
        /// 其他備註說明
        /// </summary>
        public string Remarks { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>
        /// 醫囑系統是否顯示
        /// </summary>
        public char? Status { get; set; }
        public DateTime? CreateDate { get; set; }
        public string CreateUser { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string ModifyUser { get; set; }
    }
}
