using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuChartLog
    {
        public string LogId { get; set; }
        /// <summary>
        /// Modify by User
        /// </summary>
        public string LogUser { get; set; }
        /// <summary>
        /// Modify Time
        /// </summary>
        public DateTime? LogTime { get; set; }
        /// <summary>
        /// Command Mode: Insert, Update, Delete
        /// </summary>
        public char? LogMode { get; set; }
        public string ChrHealthId { get; set; }
        public string ChrNationalId { get; set; }
        public string ChrPatientFirstname { get; set; }
        public string ChrPatientMidname { get; set; }
        public string ChrPatientLastname { get; set; }
        public string ChrSex { get; set; }
        public DateOnly? ChrBirthDate { get; set; }
        public string ChrMobilePhone { get; set; }
        public string ChrAddress { get; set; }
        public string ChrEmgContact { get; set; }
        public string ChrContactRelation { get; set; }
        public string ChrContactPhone { get; set; }
        public char? ChrCombineFlag { get; set; }
        public string ChrRemark { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
        public string ChrAreaCode { get; set; }
        public char? ChrRefugeeFlag { get; set; }
    }
}
