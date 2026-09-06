using KMU.HisOrder.MVC.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels

{
    public class testr
    {
        public string Billno { get; set; }
        public string Billdate { get; set; }
        public string TestGroup { get; set; }
        public string Reportedby { get; set; }
        public string Result { get; set; }
        public string DefualtValue { get; set; }
        public string TestName { get; set; }
        public string Contents { get; set; }
        public string NvalueMale { get; set; }
        public string NvalueFemale { get; set; }
        public string Sufix { get; set; }
        public string DefVal { get; set; }
        public string OriOrder { get; set; }
        public object AttachFile { get; set; }
        public string refbillorder { get; set; }
        public string code { get; set; }
    }


    public class PatientResponse
    {
        public bool Response { get; set; }
        public object Error { get; set; }
        //public PatientDetails patient { get; set; }
        public List<TestResult> testDetails { get; set; }
        public List<TestResult> testresultSave { get; set; }
    }

    public class testnamecode
    {
        public string code { get; set; }
        public string testname { get; set; }
    }

    public class Root
    {
        public List<testnamecode> testnamecode { get; set; }
        public List<testr> readresult { get; set; }
    }

}
