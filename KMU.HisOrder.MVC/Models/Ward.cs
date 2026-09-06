using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class Ward
    {
        [MaxLength(50)]
        public string wardid { get; set; }
        public string wardName { get; set; }
        public string department { get; set; }
        public int capacity { get; set; }
        public string createBy { get; set; }
        public DateTime createAt { get; set; }
        public string? modifyBy { get; set; }
        public DateTime? modifyAt { get; set; }

    }
}
