using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuMedpathway
    {
        public char MedType { get; set; }
        public string PathCode { get; set; }
        public string PathDesc { get; set; }
        public decimal? Showseq { get; set; }
        public char EnableStatus { get; set; }
        public string CreateUser { get; set; }
        public string ModifyUser { get; set; }
    }
}
