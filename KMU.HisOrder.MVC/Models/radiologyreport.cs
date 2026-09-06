using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMU.HisOrder.MVC.Models
{
    public class radiologyreport
    {
        public static class ReportStatuses
        {
            public const string Draft = "Draft";
            public const string Preliminary = "Preliminary";
            public const string Final = "Final";
        }

        [Key]
        public int reportid { get; set; }

        [Required]
        public int radrequestid { get; set; }

        /// <summary>
        /// Same visit ID so OPD doctor can see it
        /// </summary>
        [Required]
        public string? inhospid { get; set; }

        [Required]
        public string? radiologistid { get; set; }
        public string? studyinstanceuid { get; set; }
        [Required]
        public string? report_text { get; set; }

        /// <summary>
        /// DRAFT / FINAL / SIGNED
        /// </summary>
        [Required]
        public string? status { get; set; }
        public string? impression { get; set; }
        public string structured_findings { get; set; } = "{}";
        public bool is_locked { get; set; }
        public DateTime? finalized_at { get; set; }
        public DateTime updatedat { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public uint xmin { get; set; }

        public DateTime createdat { get; set; } = DateTime.Now;
        public DateTime? signedat { get; set; }

        /* Navigation */
        public radiologyexamrequest radiologyexamrequest { get; set; }
    }
}
