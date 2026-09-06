using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuMedfrequencyInd
    {
        public string FrqCode { get; set; }
        public string IndCode { get; set; }
        public string IndDesc { get; set; }
        public decimal? Showseq { get; set; }
        public char EnableStatus { get; set; }
        public string CreateUser { get; set; }
        public string ModifyUser { get; set; }
    }
}
