using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.InPatient.ViewModels
{
    public class ReservationVM
    {
        public List<KmuDepartment> Departments { get; set; }
        public string Shift { get; set; }
        public List<Ward> Wards { get; set; }
        public List<Bed> AvailableBeds { get; set; }
        public KmuChart Patient  { get; set; }
        public InpatientReservation Reservation { get; set; }  
    }
}
