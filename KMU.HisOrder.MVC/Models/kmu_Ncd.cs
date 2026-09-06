using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class kmu_ncd
    {
        [Key]
        public int ncdid { get; set; }

        public string inhospid { get; set; }

        public string healthid { get; set; }

        public string plancode { get; set; }

        public DateTime? createdate { get; set; }

        public string createuser { get; set; }

        public string modifyuser { get; set; }

        public string patient_answer { get; set; }
    }
}