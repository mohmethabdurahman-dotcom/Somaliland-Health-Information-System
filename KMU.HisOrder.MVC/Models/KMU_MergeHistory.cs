namespace KMU.HisOrder.MVC.Models
{
    public class KMU_MergeHistory
    {
        public int Id { get; set; }
        public string InhospId { get; set; }
        public string chr_halth_id { get; set; }
        public string mh_health_id { get; set; }
        public DateTime merged_time { get; set; }
    }
}
