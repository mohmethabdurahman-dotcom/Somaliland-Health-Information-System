using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.InPatient.ViewModels
{

    public class BedChangeVM
    {
        public KmuChart Patient { get; set; }
        public InpatientReservation Reservation { get; set; }
        public Bed CurrentBed { get; set; }
        public List<Ward> Ward { get; set; }
        public List<Bed> AvailableBeds { get; set; }
    }
}
