using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    /// <summary>
    /// Diagnosis data.
    /// </summary>
    public partial class KmuIcd
    {
        public string IcdCode { get; set; }
        public string IcdEnglishName { get; set; }
        /// <summary>
        /// ICD Code without decimal point.
        /// </summary>
        public string IcdCodeUndot { get; set; }
        /// <summary>
        /// Parent ICD Code for HisOrder UI Design.
        /// </summary>
        public string ParentCode { get; set; }
        /// <summary>
        /// ICD Code.
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// CM/PCS
        /// </summary>
        public string IcdType { get; set; }
        /// <summary>
        /// Show position for HisOrder UI Design.
        /// </summary>
        public string ShowMode { get; set; }
        /// <summary>
        /// ICD 9 / ICD 10 ...
        /// </summary>
        public string Versioncode { get; set; }
        public string ModifyUser { get; set; }
        public DateTime ModifyDate { get; set; }
        public int? Dhis2Code { get; set; }
    }
}
