namespace KMU.HisOrder.MVC.Areas.Radiology.Models
{
    public sealed class AIAssistOptions
    {
        public const string SectionName = "AIAssist";

        /// <summary>
        /// OpenAI API key. Set via appsettings.Development.json or environment variable (never commit real keys).
        /// </summary>
        public string OpenAIApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gpt-4o";

        /// <summary>low, auto, or high — low reduces policy refusals on medical imaging.</summary>
        public string ImageDetail { get; set; } = "low";

        public int MaxImages { get; set; } = 4;

        /// <summary>Images sent on the simplified retry after a refusal or empty response.</summary>
        public int RetryMaxImages { get; set; } = 2;

        public int MaxTokens { get; set; } = 2000;

        public string Disclaimer { get; set; } =
            "AI-generated draft for radiologist review only. Not a final diagnosis.";
    }
}
