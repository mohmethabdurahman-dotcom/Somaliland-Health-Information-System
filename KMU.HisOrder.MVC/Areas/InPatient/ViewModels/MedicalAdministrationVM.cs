using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.InPatient.ViewModels
{
    public class MedicalAdministrationVM
    {
        public KmuChart Patient { get; set; }
        public List<MedicalAdministration> MedicalAdministration { get; set; }
        public List<MedicalAdministration> MilkAdministration { get; set; }
        public List<Hisorderplan> administeredMedication { get; set; }
        public string inhospid { get; set; }
    }
}
