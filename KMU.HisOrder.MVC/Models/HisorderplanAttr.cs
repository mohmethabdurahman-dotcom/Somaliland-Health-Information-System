using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class HisorderplanAttr
    {
        public long Orderplanatrrid { get; set; }
        public long Orderplanid { get; set; }
        public string AttrCode { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }
        public string Parameter3 { get; set; }
        public string Parameter4 { get; set; }
        public string Parameter5 { get; set; }
        public string Parameter6 { get; set; }
        public string Des { get; set; }

        public virtual Hisorderplan Orderplan { get; set; }
    }
}
