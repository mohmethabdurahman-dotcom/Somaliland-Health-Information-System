using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Areas.Radiology.Models
{
    public class WalkInRequestViewModel
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public string PatientId { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Room is required")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "At least one body part is required")]
        public List<string> BodyPartIds { get; set; } = new List<string>();

        public string Remark { get; set; }

        public string RequestedBy { get; set; }
    }

    public class PatientSearchResult
    {
        public string ChrHealthId { get; set; }
        public string FullName { get; set; }
        public string ChrSex { get; set; }
        public DateOnly? ChrBirthDate { get; set; }
        public int? Age { get; set; }
        public string ChrMobilePhone { get; set; }
        public string ChrAddress { get; set; }
    }

    public class RoomListItem
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string Department { get; set; }
        public string Modality { get; set; }
        public string Description { get; set; }
    }

    public class BodyPartListItem
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string GroupCode { get; set; }
    }

    public class TicketViewModel
    {
        public int RequestId { get; set; }
        public string OrderId { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientSex { get; set; }
        public string PatientAge { get; set; }
        public string Modality { get; set; }
        public string RoomNumber { get; set; }
        public int TicketNumber { get; set; }
        public DateOnly OrderedDate { get; set; }
        public string PrintDate { get; set; }
        public string BodyPart { get; set; }
    }
}
