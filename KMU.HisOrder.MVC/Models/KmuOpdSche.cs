using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KmuOpdSche
    {
        public char ScheWeek { get; set; }
        public string ScheNoon { get; set; } = null!;
        public string ScheRoom { get; set; } = null!;
        public string ScheRoomName { get; set; } = null!;
        public string? ScheDoctor { get; set; }
        public string? ScheDoctorName { get; set; }
        public char? ScheOpenFlag { get; set; }
    }
}
