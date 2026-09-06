
using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels
{
    public partial class PrintClass
    {
        public string HospName { get; set; }
        public string FormTitle { get; set; }

        public PersonalCardClass PersonalInfo { get; set; }

        public string QrCodeStr { get; set; }

        public string ModifyID { get; set; }

        public string ModifyName { get; set; }
    }
}
