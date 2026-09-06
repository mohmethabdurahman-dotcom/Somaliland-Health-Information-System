using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class TestResult
    {

        [Key]
        public int testresultid { get; set; }
        public string? Billno { get; set; }
        public string? Billdate { get; set; }
        public string? TestGroup { get; set; }
        public string? Reportedby { get; set; }
        public string? Result { get; set; }
		public string? status { get; set; }

		//public string DefualtValue { get; set; }
		public string? TestName { get; set; }
        public string? Contents { get; set; }
        public string? NvalueMale { get; set; }
        public string? NvalueFemale { get; set; }
        public string? Sufix { get; set; }
        //public string DefVal { get; set; }
        public string? OriOrder { get; set; }
        public string? AttachFile { get; set; }
        public string? refbillorder { get; set; }
        public string? code { get; set; }
    }
}
