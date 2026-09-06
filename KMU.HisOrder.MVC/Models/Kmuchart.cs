using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuChart
    {
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
        public DateTime ModifyTime { get; set; }
        public string ChrAreaCode { get; set; }
        /// <summary>
        /// refugee: Y
        /// </summary>
        public char? ChrRefugeeFlag { get; set; }

        [Column("chr_drug_allergy")]
        public string? ChrDrugAllergy { get; set; }
        [Column("chr_other_medical_history")]
        public string? ChrOtherMedicalHistory { get; set; }
        [Column("chr_ncd_history")]
        public string? ChrNcdHistory { get; set; }
    }
}
