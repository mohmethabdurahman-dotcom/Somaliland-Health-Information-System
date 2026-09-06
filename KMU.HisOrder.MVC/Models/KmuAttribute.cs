using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuAttribute
    {
        /// <summary>
        /// 身分代碼
        /// </summary>
        public string AttrCode { get; set; }
        /// <summary>
        /// 身分說明
        /// </summary>
        public string AttrName { get; set; }
        /// <summary>
        /// 該身分預收的掛號費用
        /// </summary>
        public long? AttrRegFee { get; set; }
        /// <summary>
        /// 啟用狀態
        /// </summary>
        public string AttrStatus { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
