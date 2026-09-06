using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.InPatient.ViewModels
{
    public class PatientInfo:KmuChart
    {
        string inhospid { get; set; }
    }



}

public class patientlist
{
    public DateTime reserveDate { get; set; }
    public string healthid { get; set; }
    public string inhospId { get; set; }
    public string fullname { get; set; }
    public DateOnly? age { get; set; }
    public string address { get; set; }
    public string phone { get; set; }
    public string sex { get; set; }
    public string status { get; set; }
    public string bed { get; set; }
    public string? doctor { get; set; }
    public string? nurse { get; set; }
}
