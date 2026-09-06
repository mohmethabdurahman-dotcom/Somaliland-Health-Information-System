using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuCondition
    {
        public string CndCodetype { get; set; }
        public string CndCode { get; set; }
        public string CndValue1 { get; set; }
        public string CndSymbol1 { get; set; }
        public string CndValue2 { get; set; }
        public string CndSymbol2 { get; set; }
        public char? CndEnable { get; set; }
        public char? CndWeek { get; set; }
        public string CndNoon { get; set; }
        public string CndRoom { get; set; }
        public string CndDesc { get; set; }
        public string ModifyUser { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
