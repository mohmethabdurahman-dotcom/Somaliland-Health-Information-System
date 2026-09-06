using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class home_physicalsign
    {
        [Key]
        public int phyid { get; set; }
        public string inhospid { get; set; }

        public DateTime date { get; set; }

        public string category { get; set; }

        public string before_breakfast { get; set; }
        public string after_breakfast { get; set; }

        public string before_dinner { get; set; }
        public string after_dinner { get; set; }

        public string morning_systolic { get; set; }
        public string morning_diastolic { get; set; }

        public string evining_systolic { get; set; }
        public string evining_diastolic { get; set; }

        public string modify_user { get; set; }
        public DateTime? modify_time { get; set; }
    }
}
