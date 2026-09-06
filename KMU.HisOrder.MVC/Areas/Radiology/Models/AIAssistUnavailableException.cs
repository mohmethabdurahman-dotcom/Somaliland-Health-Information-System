namespace KMU.HisOrder.MVC.Areas.Radiology.Models
{
    public sealed class AIAssistUnavailableException : InvalidOperationException
    {
        public AIAssistUnavailableException(string message, OrthancInstanceFetchResult? orthancDiagnostics = null)
            : base(message)
        {
            OrthancDiagnostics = orthancDiagnostics;
        }

        public OrthancInstanceFetchResult? OrthancDiagnostics { get; }
    }
}
