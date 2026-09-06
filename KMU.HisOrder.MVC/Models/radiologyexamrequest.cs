using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class radiologyexamrequest
    {
        public static class WorkflowStatuses
        {
            public const string Ordered = "Ordered";
            public const string Scheduled = "Scheduled";
            public const string InProgress = "In_Progress";
            public const string Interpreted = "Interpreted";
            public const string Finalized = "Finalized";
            public const string Cancelled = "Cancelled";
        }

        [Key]
        public int id { get; set; }
        public DateOnly? ordereddate { get; set; }
        public string orderid { get; set; }
        public long? orderplanid { get; set; }
        public string? inhospid { get; set; }
        public string patientid { get; set; }
        //storing roomid from rooms (modility)
        public int? roomid { get; set; }

        //this is storing the nonmedcineid (bodyparty id)
        public string? item_id { get; set; }
        public int? seq_no { get; set; }
        public string? accessionnumber { get; set; }
        public string? studyinstanceuid { get; set; }
        public string? orthancstudyid { get; set; }
        public string? technicianid { get; set; }

        public string sourcetype { get; set; }
        public string requestedby { get; set; }
        public string? remark { get; set; }
        public string? status { get; set; }
        public DateTime createdat { get; set; }
    }
}
