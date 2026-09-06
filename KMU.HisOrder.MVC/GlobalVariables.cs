using KMU.HisOrder.MVC.Areas.HisOrder.Models;

namespace KMU.HisOrder.MVC
{
    public class GlobalVariables
    {
        public  PatientDTO Patient { get; set; }

        public  ClinicDTO Clinic { get; set; }

        public  LoginDTO Login { get; set; }
        public string Target = "HGH";

    }
}
