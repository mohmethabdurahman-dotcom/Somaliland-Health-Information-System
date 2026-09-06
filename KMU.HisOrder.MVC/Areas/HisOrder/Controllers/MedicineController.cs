using AutoMapper;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class MedicineController : Controller
    {
        private readonly KMUContext _context;
        private readonly IMapper _mapper;

        public MedicineController(KMUContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public IActionResult ReloadPartialView()
        {
            GlobalVariableDTO grv = null;

            using (HisOrderController col = new HisOrderController(_context))
            {
                grv = col.getGlobalVariablDTO(HttpContext);
                if (grv.Patient != null)
                {
                    var orderList = _context.Hisorderplans
                        .Where(c => c.Inhospid == grv.Patient.Inhospid
                        && c.HealthId == grv.Patient.RegPatientId
                        && c.HplanType == "Med"
                        && c.DcDate == null).OrderBy(d => d.SeqNo);

                    ViewData["MedFreq"] = _context.KmuMedfrequencies.OrderBy(c => c.FrqSeqNo).ToList();
                    ViewData["MedIndication"] = _context.KmuMedfrequencyInds.OrderBy(c => c.Showseq).ToList();
                    ViewData["MedPathWay"] = _context.KmuMedpathways.OrderBy(c => c.Showseq).ToList();
                    ViewData["PatientDTO"] = grv.Patient;

                    if (orderList.Any())
                    {
                        return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_MedicinePartialView.cshtml", orderList.ToList());
                    }
                    else
                    {
                        return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_MedicinePartialView.cshtml", null);
                    }
                }
                else
                {
                    return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_MedicinePartialView.cshtml", null);
                }
            }
        }


        public string GetMedicineItem()
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.KmuMedicines.ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }


        public string GetMedPathWayData()
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.KmuMedpathways.OrderBy(c => c.Showseq).ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }

        public string GetMedIndictionData()
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.KmuMedfrequencyInds.OrderBy(c => c.Showseq).ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }

        public string GetMedFreqData()
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.KmuMedfrequencies.OrderBy(c => c.FrqSeqNo).ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }

        public string GetHisOrderPlan(string inhospid)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.Hisorderplans.Where(c => c.Inhospid == inhospid && c.HplanType == "Med" && c.DcDate == null).OrderBy(c => c.SeqNo).ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string ModifyMedOrder(string inOrder, string inStatus)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            GlobalVariableDTO grv = null;
            using (HisOrderController col = new HisOrderController(_context))
            {
                grv = col.getGlobalVariablDTO(HttpContext);
                if (!string.IsNullOrWhiteSpace(inOrder))
                {
                    var data = (List<ExtendHisorderplan>)JsonConvert.DeserializeObject(inOrder, typeof(List<ExtendHisorderplan>));
                    if (data != null)
                    {
                        foreach (var obj in data)
                        {
                            var _hisorderplan = _mapper.Map<Hisorderplan>(obj);
                            switch (obj.ModifyType)
                            {
                                case "":
                                    if (inStatus == "confirm")
                                    {
                                        UpdateMedByObject(_hisorderplan, grv, inStatus);
                                    }
                                    break;
                                case "I":
                                    InsertMedicineByObject(_hisorderplan, grv, inStatus);
                                    break;
                                case "D":
                                    DcMedByObject(_hisorderplan, grv, inStatus);
                                    break;
                                case "U":
                                    DcMedByObject(_hisorderplan, grv, inStatus, false);
                                    InsertMedicineByObject(_hisorderplan, grv, inStatus, true);
                                    //_context.SaveChanges();

                                    break;
                            }
                        }
                    }
                }
                result.isSuccess = true;
                return JsonConvert.SerializeObject(result);
            }
        }

        private string InsertMedicineByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
        {
            try
            {
                var result = new ResultDTO() { isSuccess = false };

                inOrder.Orderplanid = -1;
                inOrder.Inhospid = inGrv.Patient.Inhospid;
                inOrder.HealthId = inGrv.Patient.RegPatientId;
                inOrder.HplanType = "Med";
                inOrder.CreateDate = DateTime.Today;
                inOrder.CreateUser = inGrv.Login.EMPCODE;
                inOrder.Status = string.IsNullOrWhiteSpace(inStatus) ? '0' : '2';
                inOrder.PrintDate = null;
                inOrder.PrintUser = "";
                _context.Add(inOrder);

                if (doSaveChange)
                {
                    _context.SaveChanges();
                    _context.Entry(inOrder).Reload();
                }
                //result.returnValue = inOrder.Orderplanid != null ? inOrder.Orderplanid.ToString() : null;
                result.returnValue = inOrder.Orderplanid.ToString();

                result.isSuccess = true;

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception EX)
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false });
            }
        }

        private string DcMedByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
        {
            try
            {
                var result = new ResultDTO() { isSuccess = false };

                var _order = _context.Hisorderplans.Where(c => c.Inhospid == inGrv.Patient.Inhospid && c.Orderplanid == inOrder.Orderplanid).FirstOrDefault();

                if (_order != null)
                {

                    _order.DcDate = DateTime.Now;
                    _order.DcStatus = '2';
                    _order.DcUser = inGrv.Login.EMPCODE;
                    _order.Status = '0';

                    _context.Update(_order);

                    if (doSaveChange)
                    {
                        _context.SaveChanges();
                    }

                    result.isSuccess = true;
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception EX)
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false });
            }
        }

        private string UpdateMedByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus)
        {
            try
            {
                var result = new ResultDTO() { isSuccess = false };
                if (inOrder.Orderplanid == null)
                {
                    var _iResult = (ResultDTO)JsonConvert.DeserializeObject(InsertMedicineByObject(inOrder, inGrv, inStatus), typeof(ResultDTO));
                    if (_iResult.isSuccess == true && _iResult.returnValue != null)
                    {
                        var id = long.Parse(_iResult.returnValue);

                        var _order = _context.Hisorderplans.Where(c => c.Inhospid == inGrv.Patient.Inhospid && c.Orderplanid == id).FirstOrDefault();

                        if (_order != null)
                        {
                            _order.Status = inStatus == "confirm" ? '2' : _order.Status;

                            //_order.QtyDose = inOrder.QtyDose;
                            //_order.Remark = inOrder.Remark;

                            _context.Update(_order);
                            _context.SaveChanges();

                            result.isSuccess = true;
                        }
                    }
                }
                else
                {
                    var _order = _context.Hisorderplans.Where(c => c.Inhospid == inGrv.Patient.Inhospid && c.Orderplanid == inOrder.Orderplanid).FirstOrDefault();

                    if (_order != null)
                    {
                        _order.Status = inStatus == "confirm" ? '2' : _order.Status;
                        _order.QtyDose = inOrder.QtyDose;
                        _order.MedBag = inOrder.MedBag;
                        _order.FreqCode = inOrder.FreqCode;
                        _order.DosePath = inOrder.DosePath;
                        _order.QtyDaily = inOrder.QtyDaily;
                        _order.TotalQty = inOrder.TotalQty;
                        _order.DoseIndication = inOrder.DoseIndication;
                        _order.PlanDays = inOrder.PlanDays;
                        _order.UnitDose = inOrder.UnitDose;
                        _order.Remark = inOrder.Remark;

                        _context.Update(_order);
                        _context.SaveChanges();

                        result.isSuccess = true;
                    }

                }

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception EX)
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false });
            }


        }
    }
}
