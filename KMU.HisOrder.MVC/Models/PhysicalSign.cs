using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMU.HisOrder.MVC.Models
{
    public partial class PhysicalSign
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // or .Computed if using a SQL trigger/GUID
        public string PhyId { get; set; }
        public string Inhospid { get; set; }
        public string PhyType { get; set; }
        public string PhyValue { get; set; }
        public string ModifyUser { get; set; }
        public int? phy_version { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
