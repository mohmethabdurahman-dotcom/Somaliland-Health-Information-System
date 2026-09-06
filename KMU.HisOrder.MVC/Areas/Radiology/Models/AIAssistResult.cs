namespace KMU.HisOrder.MVC.Areas.Radiology.Models
{
    public sealed class AIAssistResult
    {
        public string Impression { get; set; } = string.Empty;
        public string Narrative { get; set; } = string.Empty;
        public int ImagesAnalyzed { get; set; }
        public string Disclaimer { get; set; } = string.Empty;
    }
}
