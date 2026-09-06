using KMU.HisOrder.MVC.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
    public partial class DiagnosisClass
    {

    }

    public partial class VKmuIcd
    {
        private KmuIcd _KmuIcd;

        public VKmuIcd(KmuIcd inKmuIcd)
        {
            _KmuIcd = inKmuIcd;
        }

        [Display(Name = "ICD10")]
        public string IcdCode { get { return _KmuIcd.IcdCode; } set { _KmuIcd.IcdCode = value; } }
        [Display(Name = "Name")]
        public string IcdEnglishName { get { return _KmuIcd.IcdEnglishName; } set { _KmuIcd.IcdEnglishName = value; } }
        public string? ParentCode { get { return _KmuIcd.ParentCode; } set { _KmuIcd.ParentCode = value; } }
        public bool IsParent { get; set; }

    }
}
