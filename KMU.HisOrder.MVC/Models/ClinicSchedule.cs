using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class ClinicSchedule
    {
        /// <summary>
        /// 星期別
        /// </summary>
        public string ScheWeek { get; set; }
        /// <summary>
        /// 午別
        /// </summary>
        public string ScheNoon { get; set; }
        /// <summary>
        /// 診間號碼
        /// </summary>
        public string ScheRoom { get; set; }
        public string shift { get; set; }   
        /// <summary>
        /// 科別名稱
        /// </summary>
        public string ScheDptName { get; set; }
        /// <summary>
        /// 醫師職編
        /// </summary>
        public string ScheDoctor { get; set; }
        /// <summary>
        /// 醫師姓名
        /// </summary>
        public string ScheDoctorName { get; set; }
        /// <summary>
        /// 診次是否開放
        /// </summary>
        public string ScheOpenFlag { get; set; }
        /// <summary>
        /// 叫號號碼
        /// </summary>
        public long? ScheCallNo { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
        /// <summary>
        /// 科別代碼
        /// </summary>
        public string ScheDptCode { get; set; }
        public string ScheRemark { get; set; }
        /// <summary>
        /// Calling Time Update
        /// </summary>
        public DateTime? ScheCallTime { get; set; }
       
    }
}
