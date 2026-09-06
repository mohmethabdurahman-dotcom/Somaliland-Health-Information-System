using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class Registration
    {
        /// <summary>
        /// 看診日
        /// </summary>
        public DateOnly RegDate { get; set; }
        public string? shift { get; set; }
        /// <summary>
        /// 看診科別
        /// </summary>
        public string RegDepartment { get; set; }
        /// <summary>
        /// 午別
        /// </summary>
        public string RegNoon { get; set; }
        /// <summary>
        /// 門診:看診號
        /// 急診:檢傷序號
        /// </summary>
        public short RegSeqNo { get; set; }
        /// <summary>
        /// 病歷號
        /// </summary>
        public string RegHealthId { get; set; }
        /// <summary>
        /// 就醫序號
        /// </summary>
        public string Inhospid { get; set; }
        /// <summary>
        /// 檢傷分類
        /// 0：一般門診(白燈)
        /// 1：急診分類(綠燈)
        /// 2：急診分類(黃燈)
        /// 3：急診分類(紅燈)
        /// 4：急診分類(黑燈)
        /// </summary>
        public string RegTriage { get; set; }
        public string score { get; set; }
        /// <summary>
        /// 急診床號
        /// </summary>
        public string RegBedNo { get; set; }
        /// <summary>
        /// 特殊身分-&gt;參考kmu_attribute
        /// </summary>
        public string RegAttribute { get; set; }
        /// <summary>
        /// 醫師職邊
        /// </summary>
        public string RegDoctorId { get; set; }
        /// <summary>
        /// 診間號
        /// </summary>
        public string RegRoomNo { get; set; }
        /// <summary>
        /// 看診狀態
        /// N:未看診
        /// T :暫存
        /// * :已看診
        /// C:取消掛號
        /// </summary>
        public string RegStatus { get; set; }
        /// <summary>
        /// 叫號時間
        /// </summary>
        public DateTime? RegCallTime { get; set; }
        /// <summary>
        /// 開始看診時間
        /// </summary>
        public DateTime? RegStartTime { get; set; }
        /// <summary>
        /// 結束看診時間
        /// </summary>
        public DateTime? RegEndTime { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
        public DateTime? physign_time { get; set; }
        public string RegFollowCode { get; set; }
        public string RegFollowDesc { get; set; }
        /// <summary>
        /// 身分備註
        /// </summary>
        public string RegAttrDesc { get; set; }
        /// <summary>
        /// click examining start time
        /// </summary>
        public DateTime? RegExamStartTime { get; set; }
        public char? upload_status { get; set; } = 'N';
        /// <summary>
        /// finish examining return time
        /// </summary>
        public DateTime? RegExamEndTime { get; set; }

        public DateTime? reg_observe_start_time { get; set; }
        public DateTime? reg_observe_end_time { get; set; }
        //public DateTime? RegObserveEndTime { get; set; }
        /// <summary>
        /// create datetime for registration 
        /// </summary>
        public DateTime? RegCreateTime { get; set; }
    }
}
