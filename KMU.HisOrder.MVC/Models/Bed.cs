using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class Bed
    {
        [MaxLength(50)]
        public string bedId { get; set; }
        public string bedName { get; set; }
        public string wardId { get; set; }
        public string status { get; set; }
        public string createBy { get; set; }
        public DateTime createAt { get; set; }
        public string? modifyBy { get; set; }
        public DateTime? modifyAt { get; set; }
    }
}
