using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class RegisterMain
    {
        public long RegSerialM { get; set; }
        public DateOnly RegDate { get; set; }
        public string ChartNo { get; set; } = null!;
        public string VisitTime { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string CreateUser { get; set; } = null!;
    }
}
