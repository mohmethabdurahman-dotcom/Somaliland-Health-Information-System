using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
    public partial class PrintClass
    {
        public List<string> ShowPrint { get; set; }
        public string HospName { get; set; }
        public string FormTitle { get; set; }
        public GlobalVariableDTO GlobalVariable { get; set; }
        public List<Hisorderplan> OrderData { get; set; }
        public List<PrintHisordersoa> Ordersoa { get; set; }
        public string RePrintOrderplanids { get; set; }
        public string RePrintInHospID { get; set; }

    }
}

public partial class PrintHisordersoa
{
    public string ClinicRemarks { get; set; }
    public string Management { get; set; }
    public string Transfer { get; set; }
    public string Version { get; set; }
    public DateTime CreateDate { get; set; }
}

