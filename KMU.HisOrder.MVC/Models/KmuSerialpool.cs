using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuSerialpool
    {
        public string SerialOwner { get; set; }
        public long? SerialNo { get; set; }
        public string SerialPrefix { get; set; }
        public string SerialMaxno { get; set; }
    }
}
