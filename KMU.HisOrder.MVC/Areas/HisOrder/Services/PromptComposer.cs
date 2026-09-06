namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public sealed class PromptComposer
    {
        private readonly string _promptsRoot;
        private readonly ILogger<PromptComposer> _logger;

        public PromptComposer(IWebHostEnvironment environment, ILogger<PromptComposer> logger)
        {
            _promptsRoot = Path.Combine(environment.ContentRootPath, "prompts", "clinic");
            _logger = logger;
        }

        public string ComposeNcdSystemPrompt()
        {
            var core = ReadPromptFile("core-system.txt");
            var ncd = ReadPromptFile("ncd-system.txt");
            return core + Environment.NewLine + Environment.NewLine + ncd;
        }

        public string ComposeNcdUserPrompt(string runtimeContext)
        {
            var template = ReadPromptFile("ncd-user-template.txt");
            return template + Environment.NewLine + Environment.NewLine
                + "Patient Information from the current visit:" + Environment.NewLine
                + runtimeContext + Environment.NewLine + Environment.NewLine
                + "Now, generate the note.";
        }

        private string ReadPromptFile(string fileName)
        {
            var path = Path.Combine(_promptsRoot, fileName);
            if (!File.Exists(path))
            {
                _logger.LogError("Prompt file not found: {Path}", path);
                throw new InvalidOperationException($"Prompt file not found: {fileName}");
            }

            return File.ReadAllText(path).Trim();
        }
    }
}
