using System.ComponentModel.DataAnnotations;

namespace KMU.HisOrder.MVC.Models
{
    public class room
    {
        [Key]
        public int roomid { get; set; }
        public string? department { get; set; }

        /// <summary>
        /// XRAY / CT / MRI
        /// </summary>
        [Required]
        public string? modality { get; set; }

        [Required]
        public string? room_number { get; set; }

        public string? description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
