using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class Dhis2Disease
    {
        public int Dhis2Code { get; set; }
        public string Diseases { get; set; }
        public int? ShowSeq { get; set; }
    }
}
