
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.Reservation.ViewModels
{
    public partial class PrintClass
    {
        public string HospName { get; set; }
        public string FormTitle { get; set; }

        public string reserveType { get; set; }

        public PersonalCardClass patientInfo { get; set; }

        public AppointmentDetail AppointmentSheet { get; set; }

        public List<PhysicalSignItem> phySignTitle { get; set; }

        public List<PhysicalSign> phySignItems { get; set; }

        public string ModifyID { get; set; }

        public string ModifyName { get; set; }
    }
}
