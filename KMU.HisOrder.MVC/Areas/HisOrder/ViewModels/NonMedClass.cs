using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
    public partial class VKmuNonMedicine
    {
        private ExtendKmuNonMedicine _KmuNonMedicine;

        public VKmuNonMedicine(ExtendKmuNonMedicine inKmuNonMedicine)
        {
            _KmuNonMedicine = inKmuNonMedicine;
        }

        // [Display(Name = "Id")]
        public string ItemId { get { return _KmuNonMedicine.ItemId; } set { _KmuNonMedicine.ItemId = value; } }
        
        public string ItemName { get { return _KmuNonMedicine.ItemName; } set { _KmuNonMedicine.ItemName = value; } }

        /// <summary>
        /// 5. Lab
        /// 6. Exam
        /// 7. Path
        /// </summary>
        public string ItemType { get { return _KmuNonMedicine.ItemType; } set { _KmuNonMedicine.ItemType = value; } }
        public string? ItemSpec { get { return _KmuNonMedicine.ItemSpec; } set { _KmuNonMedicine.ItemSpec = value; } }
        public string Remark { get { return _KmuNonMedicine.Remark; } set { _KmuNonMedicine.Remark = value; } }
        public decimal? ShowSeq { get { return _KmuNonMedicine.ShowSeq; } set { _KmuNonMedicine.ShowSeq = value; } }
        public string? GroupCode { get { return _KmuNonMedicine.GroupCode; } set { _KmuNonMedicine.GroupCode = value; } }
        public bool enabled { get { return _KmuNonMedicine.enabled; } set { _KmuNonMedicine.enabled = value; } }

        // Extend join columns
        public string? RefName { get { return _KmuNonMedicine.RefName; } set { _KmuNonMedicine.RefName = value; } }
        public int? RefShowseq { get { return _KmuNonMedicine.RefShowseq; } set { _KmuNonMedicine.RefShowseq = value; } }
        public string PlanDes { get { return _KmuNonMedicine.PlanDes; } set { _KmuNonMedicine.PlanDes = value; } }
    }
}
