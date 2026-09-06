using KMU.HisOrder.MVC.Models;
using Microsoft.CodeAnalysis.Recommendations;
using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
    public partial class VKmuMedicine
    {
        private KmuMedicine _KmuMedicine;

        public VKmuMedicine(KmuMedicine inKmuMedicine)
        {
            _KmuMedicine = inKmuMedicine;
        }

        [Display(Name = "Code")]
        public string MedId { get { return _KmuMedicine.MedId; } set { _KmuMedicine.MedId = value; } }
        [Display(Name = "Type")]
        public string MedType { get { return _KmuMedicine.MedType; } set { _KmuMedicine.MedType = value; } }
        public string GenericName { get { return _KmuMedicine.GenericName; } set { _KmuMedicine.GenericName = value; } }
        public string? BrandName { get { return _KmuMedicine.BrandName; } set { _KmuMedicine.BrandName = value; } }
        public string? UnitSpec { get { return _KmuMedicine.UnitSpec; } set { _KmuMedicine.UnitSpec = value; } }
        public string? PackSpec { get { return _KmuMedicine.PackSpec; } set { _KmuMedicine.PackSpec = value; } }
        [Display(Name = "Default")]
        public string? DefaultFreq { get { return _KmuMedicine.DefaultFreq; } set { _KmuMedicine.DefaultFreq = value; } }
        [Display(Name = "Dura")]
        public string? RefDuration { get { return _KmuMedicine.RefDuration; } set { _KmuMedicine.RefDuration = value; } }
        public string? Remarks { get { return _KmuMedicine.Remarks; } set { _KmuMedicine.Remarks = value; } }
    }
}
