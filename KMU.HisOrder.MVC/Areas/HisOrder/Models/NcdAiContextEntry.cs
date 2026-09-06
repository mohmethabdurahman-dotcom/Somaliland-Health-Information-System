namespace KMU.HisOrder.MVC.Areas.HisOrder.Models
{
    public sealed class NcdAiContextEntry
    {
        public string Category { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public DateTime? RecordedDate { get; set; }
    }
}
