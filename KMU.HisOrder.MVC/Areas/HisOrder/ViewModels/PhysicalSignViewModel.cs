namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
    internal class PhysicalSignViewModel
    {
        public DateTime? ModifyTime { get; set; }
        public string ModifyUser { get; set; }
        public string Inhospid { get; set; }
        public int? phy_version { get; set; }
        public string PhyType { get; set; }
        public string PhyValue { get; set; }
        public object Shift { get; set; }
    }
}