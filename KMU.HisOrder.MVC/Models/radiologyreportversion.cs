using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public sealed class radiologyreportversion
    {
        [Key]
        public long version_id { get; set; }
        public int report_id { get; set; }
        public int version_no { get; set; }
        public string snapshot { get; set; } = "{}";
        public string created_by { get; set; } = string.Empty;
        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}
