using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public sealed class radiologyreportaudittrail
    {
        [Key]
        public long audit_id { get; set; }
        public int report_id { get; set; }
        public string event_type { get; set; } = string.Empty;
        public string payload { get; set; } = "{}";
        public string actor_id { get; set; } = string.Empty;
        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}
