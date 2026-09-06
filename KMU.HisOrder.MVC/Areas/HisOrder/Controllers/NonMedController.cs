using AutoMapper;
using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class NonMedController : Controller
    {
        private readonly KMUContext _context;
        private readonly IMapper _mapper;
        private readonly BloodBankIntegrationService _bloodBankIntegration;
        private readonly BloodBankService _bloodBankService;

        public NonMedController(
            KMUContext context,
            IMapper mapper,
            BloodBankIntegrationService bloodBankIntegration,
            BloodBankService bloodBankService)
        {
            _context = context;
            _mapper = mapper;
            _bloodBankIntegration = bloodBankIntegration;
            _bloodBankService = bloodBankService;
        }

        public IActionResult Index()
        {
            return View();
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
                        && (c.HplanType != "Med" && c.HplanType != "ICD")
                        && c.DcDate == null).OrderBy(d => d.SeqNo);

                    ViewData["PatientDTO"] = grv.Patient;
                    //取得非藥相關
                    if (_context.KmuCoderefs.Where(c => c.RefCodetype == "NonMedLocation").Count() > 0)
                    {
                        ViewBag.NonMedLocation = _context.KmuCoderefs.Where(c => c.RefCodetype == "NonMedLocation").OrderBy(d=>d.RefShowseq).ToList();
                    }
                    else
                    {
                        ViewBag.NonMedLocation = new List<KmuCoderef>();
                    }

                    if (orderList.Any())
                    {
                        return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_NonMedPartialView.cshtml", orderList.ToList());
                    }
                    else
                    {
                        return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_NonMedPartialView.cshtml", null);
                    }
                }
                else
                {
                    return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_NonMedPartialView.cshtml", null);
                }
            }
        }

        public string GetHisOrderPlan(string inhospid)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            var data = _context.Hisorderplans.Where(c => c.Inhospid == inhospid && ( c.HplanType != "Med" && c.HplanType != "ICD")  && c.DcDate == null).OrderBy(d=>d.SeqNo).ToList();

            if (data.Any())
            {
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(data);
            }

            return JsonConvert.SerializeObject(result);
        }

        public async Task<string> GetBloodBankRequests(string inhospid, string patientId)
        {
            var result = new ResultDTO { isSuccess = false };

            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(patientId))
            {
                result.Message = "Missing visit identifiers.";
                return JsonConvert.SerializeObject(result);
            }

            var requests = await _context.BloodBankRequests
                .Where(r => r.Inhospid == inhospid && r.PatientId == patientId.Trim())
                .ToListAsync();

            if (requests.Count > 0)
            {
                await _bloodBankIntegration.SyncRequestsFromHisOrdersAsync(requests);
            }

            var summaries = await _bloodBankService.GetVisitBloodBankRequestsAsync(inhospid, patientId);
            result.isSuccess = true;
            result.returnValue = JsonConvert.SerializeObject(summaries);
            return JsonConvert.SerializeObject(result);
        }

        public async Task<string> GetBloodBankRequestDetails(long requestId, string inhospid, string patientId)
        {
            var result = new ResultDTO { isSuccess = false };

            if (requestId <= 0 || string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(patientId))
            {
                result.Message = "Invalid request.";
                return JsonConvert.SerializeObject(result);
            }

            var request = await _context.BloodBankRequests.FindAsync(requestId);
            if (request != null)
            {
                await _bloodBankIntegration.SyncRequestsFromHisOrdersAsync(new[] { request });
            }

            var details = await _bloodBankService.GetVisitBloodBankRequestDetailsAsync(requestId, inhospid, patientId);
            if (details == null)
            {
                result.Message = "Blood bank request not found for this visit.";
                return JsonConvert.SerializeObject(result);
            }

            result.isSuccess = true;
            result.returnValue = JsonConvert.SerializeObject(details);
            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string ModifyNonMedOrder(string inOrder,  string inStatus)
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
                            NormalizeBloodOrderFields(obj);
                            var _hisorderplan = _mapper.Map<Hisorderplan>(obj);
                            switch (obj.ModifyType) 
                            {
                                case "":
                                    // Blood: always persist/sync (no Fin/confirm required).
                                    // Other NonMed: only touch status on confirm.
                                    if (obj.HplanType == "Blood" || inStatus == "confirm")
                                    {
                                        UpdateNonMedByObject(_hisorderplan, grv, inStatus);
                                    }
                                    break;
                                case "I":
                                    InsertNonMedByObject(_hisorderplan, grv , inStatus);
                                    break;
                                case "D":
                                    DcNonMedByObject(_hisorderplan, grv, inStatus);
                                    break;
                                case "U":
                                    if (obj.HplanType == "Blood")
                                    {
                                        // Keep same orderplan id for blood — update + sync Dr Request
                                        UpdateNonMedByObject(_hisorderplan, grv, inStatus);
                                    }
                                    else
                                    {
                                        //2022.12.29 update by 1050325
                                        DcNonMedByObject(_hisorderplan, grv, inStatus , false);
                                        InsertNonMedByObject(_hisorderplan, grv, inStatus , true);
                                    }
                                    break;  
                            }
                        }
                    }
                }

                result.isSuccess = true;
                return JsonConvert.SerializeObject(result);
            }
        }

        /// <summary>
        /// Save one blood order immediately from Patient Visit (no Fin/confirm) and push to Blood Bank Dr Requests.
        /// </summary>
        [HttpPost]
        public string PushBloodOrder(string inOrder)
        {
            var result = new ResultDTO { isSuccess = false };
            using (HisOrderController col = new HisOrderController(_context))
            {
                var grv = col.getGlobalVariablDTO(HttpContext);
                if (string.IsNullOrWhiteSpace(inOrder) || grv?.Patient == null)
                {
                    result.Message = "Missing blood order data.";
                    return JsonConvert.SerializeObject(result);
                }

                var obj = JsonConvert.DeserializeObject<ExtendHisorderplan>(inOrder);
                if (obj == null || obj.HplanType != "Blood")
                {
                    result.Message = "Not a blood order.";
                    return JsonConvert.SerializeObject(result);
                }

                if (string.IsNullOrWhiteSpace(obj.DosePath))
                {
                    result.Message = "Blood type is required.";
                    return JsonConvert.SerializeObject(result);
                }

                NormalizeBloodOrderFields(obj);
                var plan = _mapper.Map<Hisorderplan>(obj);
                string json;

                if (obj.Orderplanid > 0 && obj.ModifyType != "I")
                {
                    json = UpdateNonMedByObject(plan, grv, "");
                }
                else
                {
                    json = InsertNonMedByObject(plan, grv, "");
                }

                var inner = JsonConvert.DeserializeObject<ResultDTO>(json) ?? new ResultDTO();
                if (!inner.isSuccess)
                {
                    result.Message = inner.Message ?? "Failed to save blood order.";
                    return JsonConvert.SerializeObject(result);
                }

                result.isSuccess = true;
                result.returnValue = inner.returnValue ?? plan.Orderplanid.ToString();
                result.Message = "Blood order sent to Blood Bank.";
                return JsonConvert.SerializeObject(result);
            }
        }

        private static void NormalizeBloodOrderFields(ExtendHisorderplan order)
        {
            if (order.HplanType != "Blood") return;

            if (!string.IsNullOrWhiteSpace(order.DosePath))
            {
                order.DosePath = order.DosePath.Trim();
            }

            if (order.UrgFlag == null || order.UrgFlag == '\0')
            {
                order.UrgFlag = '1';
            }
        }

        private void CancelBloodRequestForOrder(long orderplanid, string user)
        {
            var request = _context.BloodBankRequests
                .FirstOrDefault(r => r.Orderplanid == orderplanid);
            if (request == null || request.Status != RequestStatuses.Pending)
            {
                return;
            }

            request.Status = RequestStatuses.Rejected;
            request.RejectReason = "Order discontinued in HIS";
            request.ModifyUser = user;
            request.ModifyDate = DateTime.Now;
            _context.Update(request);
            _context.SaveChanges();
        }

        /// <summary>
        /// Push to Blood Bank → Dr Requests when a blood order is saved on Patient Visit
        /// (no visit Fin/confirm required).
        /// </summary>
        private void SyncBloodRequestFromOrder(Hisorderplan order, GlobalVariableDTO grv, string inStatus)
        {
            if (order.HplanType != "Blood" || order.Orderplanid <= 0)
            {
                return;
            }

            var savedOrder = _context.Hisorderplans.Find(order.Orderplanid);
            if (savedOrder == null)
            {
                return;
            }

            var existing = _context.BloodBankRequests
                .FirstOrDefault(r => r.Orderplanid == savedOrder.Orderplanid);

            if (existing == null)
            {
                _bloodBankIntegration.CreateRequestFromBloodOrderAsync(savedOrder, grv)
                    .GetAwaiter().GetResult();
            }
            else
            {
                _bloodBankIntegration.SyncRequestForOrderplanAsync(savedOrder.Orderplanid)
                    .GetAwaiter().GetResult();
            }
        }

        private string DcNonMedByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
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
                    // Radiology Cancel Update
                    if (_order.HplanType == "Exam")
                    {
                        var radiologyRequest = _context.radiologyexamrequests
                            .Where(r => r.inhospid == inGrv.Patient.Inhospid && r.orderplanid == _order.Orderplanid).FirstOrDefault();

                        if (radiologyRequest != null)
                        {
                            radiologyRequest.status = "Cancelled";
                            _context.Update(radiologyRequest);
                            _context.SaveChanges();
                        }
                    }

                    if (_order.HplanType == "Blood")
                    {
                        CancelBloodRequestForOrder(_order.Orderplanid, inGrv.Login.EMPCODE);
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

        private string UpdateNonMedByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus)
        {
            try
            {
                var result = new ResultDTO() { isSuccess = false };


                if (inOrder.Orderplanid == null)
                {
                    var _iResult =(ResultDTO) JsonConvert.DeserializeObject(InsertNonMedByObject(inOrder, inGrv, inStatus), typeof(ResultDTO));
                    if(_iResult.isSuccess == true && _iResult.returnValue !=null)
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
                        _order.Remark = inOrder.Remark;
                        _order.LocationCode = inOrder.LocationCode;
                        if (inOrder.HplanType == "Blood")
                        {
                            _order.DosePath = inOrder.DosePath;
                            _order.UrgFlag = inOrder.UrgFlag;
                        }

                        _context.Update(_order);
                        _context.SaveChanges();

                if (inOrder.HplanType == "Blood")
                {
                    try
                    {
                        SyncBloodRequestFromOrder(_order, inGrv, inStatus);
                    }
                    catch
                    {
                        // HIS order already saved; Blood Bank sync failure must not block the doctor.
                    }
                }
                 
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

        private string InsertNonMedByObject(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
        {
            try
            {
                var result = new ResultDTO() { isSuccess = false };

                // Avoid duplicate blood rows when Examining/Fin runs after an earlier save of the same line
                if (inOrder.HplanType == "Blood" && !string.IsNullOrWhiteSpace(inOrder.PlanCode))
                {
                    var existingBlood = _context.Hisorderplans
                        .Where(o => o.Inhospid == inGrv.Patient.Inhospid
                            && o.HplanType == "Blood"
                            && o.PlanCode == inOrder.PlanCode
                            && o.DcDate == null
                            && (o.Status == '0' || o.Status == '2')
                            && o.HealthId == inGrv.Patient.RegPatientId)
                        .OrderByDescending(o => o.CreateDate)
                        .FirstOrDefault();

                    if (existingBlood != null
                        && string.Equals(existingBlood.DosePath?.Trim(), inOrder.DosePath?.Trim(), StringComparison.OrdinalIgnoreCase)
                        && existingBlood.CreateDate.HasValue
                        && existingBlood.CreateDate.Value >= DateTime.Now.AddMinutes(-30))
                    {
                        inOrder.Orderplanid = existingBlood.Orderplanid;
                        return UpdateNonMedByObject(inOrder, inGrv, inStatus);
                    }
                }

                inOrder.Orderplanid = -1;
                inOrder.Inhospid = inGrv.Patient.Inhospid;
                inOrder.HealthId = inGrv.Patient.RegPatientId;
                //inOrder.HplanType = "Exam";
                //inOrder.SeqNo = 99;
                inOrder.CreateDate = DateTime.Now;
                inOrder.CreateUser = inGrv.Login.EMPCODE;
                inOrder.PlanDays = 1;
                inOrder.QtyDose = inOrder.QtyDose;
                inOrder.Status = string.IsNullOrWhiteSpace(inStatus) ? '0':'2';
                inOrder.PrintDate = null;
                inOrder.PrintUser = "";

                _context.Add(inOrder);

                if (doSaveChange)
                {
                    _context.SaveChanges();
                    _context.Entry(inOrder).Reload();
                }

                if (inOrder.HplanType == "Exam")
                {
                    var savedOrder = _context.Hisorderplans
                        .Where(x =>
                            x.HplanType == "Exam" &&
                            x.PlanCode == inOrder.PlanCode &&
                            x.Inhospid == inOrder.Inhospid &&
                            x.CreateUser == inGrv.Login.EMPCODE)
                        .OrderByDescending(x => x.CreateDate)
                        .FirstOrDefault();

                    if (savedOrder != null)
                    {
                        var request = new radiologyexamrequest()
                        {
                            orderplanid = savedOrder.Orderplanid,
                            inhospid = savedOrder.Inhospid,
                            patientid = inGrv.Patient.RegPatientId,
                            sourcetype = inGrv.Clinic.InhospType,
                            requestedby = inGrv.Login.EMPCODE,
                            item_id = savedOrder.PlanCode,
                            status = "PENDING",
                            ordereddate = DateOnly.FromDateTime(DateTime.Now),
                            createdat = DateTime.Now
                        };

                        _context.radiologyexamrequests.Add(request);
                        _context.SaveChanges();
                    }
                }

                if (inOrder.HplanType == "Blood")
                {
                    try
                    {
                        SyncBloodRequestFromOrder(inOrder, inGrv, inStatus);
                    }
                    catch
                    {
                        // HIS order already saved; Blood Bank sync failure must not block the doctor.
                    }
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
    }
}
