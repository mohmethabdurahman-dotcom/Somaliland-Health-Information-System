using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.Radiology.ViewComponents
{
    public sealed class ViewerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string studyInstanceUid)
        {
            var model = new ViewerFrameModel
            {
                StudyInstanceUid = studyInstanceUid,
                StreamUrl = $"/Radiology/Stream/dicom-web/studies/{studyInstanceUid}"
            };

            return View("~/Areas/Radiology/Views/Shared/Components/Viewer/Default.cshtml", model);
        }
    }

    public sealed class ViewerFrameModel
    {
        public string StudyInstanceUid { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
    }
}
