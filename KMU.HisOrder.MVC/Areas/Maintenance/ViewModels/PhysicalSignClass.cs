using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.Maintenance.ViewModels
{
    public class PhysicalSignClass
    {
    }

    public class PhysicalSignItem
    {
        public string ClinicType { get; set; }
        public string ConditonType { get; set; }

        public string CodeName { get; set; }
        
        public string InputType { get; set; }

        public bool DefaultFlag { get; set; }

        public List<KmuCondition> Conditions { get; set; }
    }

    public class PhysicalConditionItem
    {
        public string CODETYPE { get; set; }

        public string VALUE { get; set; }
    }

    public class TriageReturnClass
    {
        public string TriageLevel { get; set; }

        public string TriagePoint { get; set; }

        public string lightColor { get; set; }
    }
}
