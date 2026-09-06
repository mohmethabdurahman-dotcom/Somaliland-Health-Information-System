using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.CBC.ViewModel
{
    internal class TestLabsViewModel
    {
        public string InHospitalId { get; set; }
        public object TestId { get; set; }
        public string status { get; set; }
        public object TestDate { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string LastName { get; set; }
        public string MobilePhone { get; set; }
        public string Gender { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string Address { get; set; }
        public List<TestResult> TestResults { get; set; }

    }
}