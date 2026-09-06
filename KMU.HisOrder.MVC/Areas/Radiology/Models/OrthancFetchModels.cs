using System.Net;

namespace KMU.HisOrder.MVC.Areas.Radiology.Models
{
    public sealed class OrthancInstanceFetchResult
    {
        public List<string> InstanceIds { get; init; } = new();
        public int SeriesCount { get; init; }
        public int SopCount { get; init; }
        public int NativeFromDicomWebCount { get; init; }
        public int ResolvedCount { get; init; }
        public bool StudyFoundViaRest { get; init; }
        public HttpStatusCode? DicomWebSeriesStatus { get; init; }
        public HttpStatusCode? ToolsFindStudyStatus { get; init; }
        public int ToolsFindStudyCount { get; init; }
        public HttpStatusCode? RestInstancesStatus { get; init; }
        public HttpStatusCode? FirstFailureStatus { get; init; }
        public bool OrthancAuthConfigured { get; init; }
    }

    public sealed class OrthancFetchDiagnostics
    {
        public HttpStatusCode? FirstFailureStatus { get; set; }
        public HttpStatusCode? DicomWebSeriesStatus { get; set; }
        public HttpStatusCode? ToolsFindStudyStatus { get; set; }
        public int ToolsFindStudyCount { get; set; }
        public HttpStatusCode? RestInstancesStatus { get; set; }

        public void RecordFailure(HttpStatusCode status)
        {
            if (!FirstFailureStatus.HasValue)
            {
                FirstFailureStatus = status;
            }
        }
    }
}
