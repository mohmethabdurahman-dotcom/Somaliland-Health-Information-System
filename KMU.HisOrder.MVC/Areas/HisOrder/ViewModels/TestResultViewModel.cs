using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.HisOrder.ViewModels
{
   
        public class subresult
        {
     
            public string code { get; set; }
            public string contents { get; set; }
            public string result { get; set; }
            public string sufix { get; set; }
            public string NvalueMale { get; set; }
            public string NvalueFemale { get; set; }
        }


        public class TestResultViewModel
    {
        public string sex { get; set; }
        public string Code { get; set; }
            public string TestName { get; set; }
            public string TestGroup { get; set; }
            public List<subresult> subresults { get; set; }
        }

    }

