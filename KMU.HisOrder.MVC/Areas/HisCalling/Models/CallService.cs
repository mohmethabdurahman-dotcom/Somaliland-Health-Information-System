using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using System.Drawing;

namespace KMU.HisOrder.MVC.Areas.HisCalling.Models
{
    public class CallService : IDisposable
    {
        private readonly KMUContext _context;

        public CallService(KMUContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public List<ClinicSchedule> getCallGroup(string inWeek, string inArea, string inNoon)
        {
            List<ClinicSchedule> result = new List<ClinicSchedule>();
            try
            {
                var shift = "";
                var ShiftA = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift A").RefDes;
                var ShiftB = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift B").RefDes;
                var ShiftC = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift C").RefDes;

                TimeSpan duration = DateTime.Now.TimeOfDay;

                var rooms = _context.KmuCoderefs
                    .Where(c => c.RefCodetype == "call_group" && c.RefCasetype == inArea).Select(c => c.RefCode);

                if (duration > TimeSpan.Parse(ShiftA) && duration < TimeSpan.Parse(ShiftB))
                {
                    result = _context.ClinicSchedules
                                      .Where(c => c.ScheWeek == inWeek && c.ScheNoon == inNoon && rooms.Contains(c.ScheRoom) && (c.shift == "Shift A" || c.shift == "All Time")).ToList();
                }
                else if (duration > TimeSpan.Parse(ShiftB) && duration < TimeSpan.Parse(ShiftC))
                {
                    result = _context.ClinicSchedules
                                       .Where(c => c.ScheWeek == inWeek && c.ScheNoon == inNoon && rooms.Contains(c.ScheRoom) && (c.shift == "Shift B" || c.shift == "All Time")).ToList();
                }
                else
                {
                    result = _context.ClinicSchedules
                                     .Where(c => c.ScheWeek == inWeek && c.ScheNoon == inNoon && rooms.Contains(c.ScheRoom) && (c.shift == "Shift C" || c.shift == "All Time")).ToList();
                }
                if (result.Any())
                {
                    result.ForEach(c =>
                    {
                        //2023.04.11 叫號燈開啟時將號碼歸0 update by elain.
                        //c.ScheCallNo = 0;

                        //2023.04.27 判斷ScheCallTime日期非當日則取0號,反之取ScheCallNo為叫號燈開啟時初始號 update by elain.
                        if (c.ScheCallNo == null || (c.ScheCallTime ?? DateTime.Now.AddDays(-1)).Date != DateTime.Now.Date)
                        {
                            c.ScheCallNo = 0;
                        }
                    });
                }

                return result;
            }
            catch
            {
                return result;
            }
        }

        public List<SelectListItem> getCallArea(string inArea = null)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            try
            {
                var arealist = _context.KmuCoderefs.Where(c => c.RefCodetype == "call_area")
                                    .OrderBy(c => c.RefShowseq).ThenBy(c => c.RefCode).ToList();
                if (arealist.Any())
                {
                    foreach (var dto in arealist)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.RefCode))
                        {
                            result.Add(new SelectListItem()
                            {
                                Value = dto.RefCode,
                                Text = dto.RefName,
                                Selected = string.IsNullOrWhiteSpace(inArea) ? false :
                                    (dto.RefCode == inArea) ? true : false
                            });
                        }
                    }
                }
                return result;
            }
            catch
            {
                return result;
            }
        }

        public ResultDTO setScheCallNo(string inWeek, string inNoon, string inRoom, int inNo)
        {
            var result = new ResultDTO() { isSuccess = false };

            try
            {
                var dto = _context.ClinicSchedules.Where(c =>
                    c.ScheWeek == inWeek && c.ScheNoon == inNoon && c.ScheRoom == inRoom).FirstOrDefault();
                if (dto == null)
                {
                    result.Message = string.Format("ScheWeek={0}, ScheNoon={1}, ScheRoom={2}: No data found.", inWeek, inNoon, inRoom);
                    return result;
                }

                dto.ScheCallNo = inNo;
                dto.ScheCallTime = DateTime.Now;    //2023.04.27 更新叫號時需紀錄更新時間 update by elain.
                _context.SaveChanges();

                result.isSuccess = true;
                result.Message = string.Format("successfully calling number: {0} ", inNo);
                return result;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                return result;
            }
        }


    }
}
