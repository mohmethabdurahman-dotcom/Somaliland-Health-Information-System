namespace KMU.HisOrder.MVC.Models
{
    public class kmu_chart_MergeHistory
    {
        public int Id { get; set; }
        public string chr_halth_id { get; set; }
        public string mh_health_id { get; set; }
      
        public DateTime merged_time { get; set; }
        public string merger_user { get; set; }

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
        public char? ChrRefugeeFlag { get; set; }
    }
}
