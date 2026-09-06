using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    public class PhysicalSignController : Controller
    {

        private readonly KMUContext _context;

        public PhysicalSignController(KMUContext context)
        {
            _context = context;
        }

        #region View Action

        #endregion



        public string CaculateTriage(string strTriage)
        {
            #region Variable Setting
            
            TriageReturnClass triageMsg = new TriageReturnClass();
            List<PhysicalConditionItem> objConditionDto = new List<PhysicalConditionItem>();

            #endregion

            PhysicalConditionItem[]? objArray = JsonConvert.DeserializeObject(strTriage, typeof(PhysicalConditionItem[])) as PhysicalConditionItem[];
            objConditionDto = objArray.ToList();

            using (PhysicalSignService service = new PhysicalSignService(_context))
            {
                triageMsg = service.CaculateTriage(objConditionDto);
            }

                


            return JsonConvert.SerializeObject(triageMsg);
        }
    }
}
