using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class RegisterDetail
    {
        public long RegSerialD { get; set; }
        public long RegSerialM { get; set; }
        public DateOnly RegDate { get; set; }
        public string RegClinic { get; set; } = null!;
        public string RegNoon { get; set; } = null!;
        public short RegSeqNo { get; set; }
        public string RegChartNo { get; set; } = null!;
        public string RegType { get; set; } = null!;
        public string? RegBedNo { get; set; }
        public string? RegAttribute { get; set; }
        public int? RegFee { get; set; }
        public string? RegStatus { get; set; }
        public string? RegRoomNo { get; set; }
        public long Inhospid { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateUser { get; set; } = null!;
    }
}
