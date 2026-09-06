using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuMedfrequency
    {
        public string FrqCode { get; set; }
        public string FreqDesc { get; set; }
        public int? FrqForDays { get; set; }
        public int? FrqForTimes { get; set; }
        public int? FrqOneDayTimes { get; set; }
        public char EnableStatus { get; set; }
        public string CreateUser { get; set; }
        public string ModifyUser { get; set; }
        public int? FrqSeqNo { get; set; }
    }
}
