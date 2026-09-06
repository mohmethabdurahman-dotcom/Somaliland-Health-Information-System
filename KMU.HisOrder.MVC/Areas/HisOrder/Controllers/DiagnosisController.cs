using AutoMapper;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NuGet.DependencyResolver;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class DiagnosisController : Controller
    {
        //private readonly ILogger<DiagnosisController> _logger;
        private readonly KMUContext _context;
        //private readonly ILoggerFactory _LoggerFactory;


        public DiagnosisController(KMUContext context)
        {
            _context = context;
            //_logger = logger;
            //_LoggerFactory = loggerFactory;
            ////_LoggerFactory.AddFile("C:\\Users\\ASUS\\Desktop\\Logg\\HIS-{Date}.txt");
            //_LoggerFactory.AddFile("/var/log/HIS-{Date}.txt");
        }
        public JsonResult getICDHisorderplanData()
        {
            using (HisOrderController col = new HisOrderController(_context))
            {
                GlobalVariableDTO Grv = col.getGlobalVariablDTO(HttpContext);

                if (Grv.Patient != null)
                {
                    var IcdOrder = _context.Hisorderplans.Where(c => c.Inhospid == Grv.Patient.Inhospid && c.HplanType == "ICD" && c.DcDate == null).OrderBy(c => c.SeqNo);

                    if (IcdOrder.Any())
                    {
                        return Json(IcdOrder);
                    }
                    else
                    {
                        return Json(null);
                    }
                }
                else
                {
                    return Json(null);
                }
            }
        }

        public JsonResult ModifyICDOrder(IEnumerable<Hisorderplan> inOrder, string inStatus)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            if (string.IsNullOrWhiteSpace(inStatus)) { inStatus = ""; };

            try
            {
                using (HisOrderController col = new HisOrderController(_context))
                {
                    using (var _ContextTransaction = _context.Database.BeginTransaction())
                    {
                        GlobalVariableDTO Grv = col.getGlobalVariablDTO(HttpContext);

                        inOrder = inOrder ?? new List<Hisorderplan>();

                        IEnumerable<Hisorderplan> _dbData = _context.Hisorderplans.Where(c => c.Inhospid == Grv.Patient.Inhospid && c.HplanType == "ICD" && c.DcDate == null).OrderBy(c => c.SeqNo);
                        string _DBStatus = _dbData.Count() == 0 ? "" : _dbData.FirstOrDefault().Status == '2' ? "confirm" : "";

                        #region 1. No any ICD,update all status DC
                        if (inOrder == null || inOrder.Count() == 0)
                        {
                            if (_dbData.Count() > 0)
                            {
                                foreach (Hisorderplan order in _dbData)
                                {
                                    result = DcICDHisOrderPlan(order, Grv, false);
                                    if (result.isSuccess == false) { break; }
                                }
                            }
                            else
                            {
                                result.isSuccess = true; // No any data need dc
                            }

                            if (result.isSuccess)
                            {
                                _context.SaveChanges();
                                _ContextTransaction.Commit();
                            }
                            else
                            {
                                _ContextTransaction.Rollback();
                            }

                            return Json(result);
                        }
                        #endregion

                        #region 2. All data is the same
                        List<string> _dbDataCompare = new List<string>();
                        if (_dbData != null)
                        {
                            foreach (Hisorderplan p in _dbData)
                            {
                                _dbDataCompare.Add(p.PlanCode.ToString());
                                _dbDataCompare.Add((p.SeqNo - 1).ToString());
                            }
                        }

                        List<string> inOrderCompare = new List<string>();
                        if (inOrder != null)
                        {
                            foreach (Hisorderplan p in inOrder)
                            {
                                inOrderCompare.Add(p.PlanCode.ToString());
                                inOrderCompare.Add(p.SeqNo.ToString());
                            }
                        }

                        if (inOrderCompare.SequenceEqual(_dbDataCompare))
                        {
                            if (_DBStatus == inStatus)
                            {
                                // No data modification (contain stauts)
                                result.isSuccess = true;
                            }
                            else
                            {
                                // STATUS：Save -> Confirm
                                foreach (Hisorderplan data in inOrder)
                                {
                                    result = updateICDHisOrderPlan(data, Grv, inStatus, false);
                                    if (result.isSuccess == false) { break; }
                                }

                                if (result.isSuccess)
                                {
                                    _context.SaveChanges();
                                    _ContextTransaction.Commit();
                                }
                                else
                                {
                                    _ContextTransaction.Rollback();
                                }
                            }

                            return Json(result);
                        }
                        #endregion

                        #region 3. Data have been modify
                        List<Hisorderplan> inOrderList = inOrder.ToList();

                        // DC no match Orderplanid
                        List<string> _dbOrderplanidArr = _dbData.Select(c => c.Orderplanid.ToString()).Where(c => !string.IsNullOrWhiteSpace(c.ToString())).ToList();
                        List<string> OrderplanidArr = inOrder.Select(c => c.Orderplanid.ToString()).Where(c => !string.IsNullOrWhiteSpace(c.ToString())).ToList();
                        List<string> NoMatch = _dbOrderplanidArr.Except(OrderplanidArr).ToList();

                        IEnumerable<Hisorderplan> NoMatchData = _dbData.Where(c => NoMatch.Contains(c.Orderplanid.ToString()));
                        if (NoMatchData.Count() > 0)
                        {
                            foreach (Hisorderplan data in NoMatchData)
                            {
                                DcICDHisOrderPlan(data, Grv, false);
                            }
                        }

                        for (int i = 0; i < inOrderList.Count(); i++)
                        {
                            if (inOrderList[i].Orderplanid == -1)
                            {
                                inOrderList[i].SeqNo = (short)i;
                                insertICDHisOrderPlan(inOrderList[i], Grv, inStatus, false);
                            }
                            else
                            {
                                // Compare data, if modified DC order, insert new order
                                Hisorderplan _data = _dbData.Where(c => c.Orderplanid == inOrderList[i].Orderplanid).FirstOrDefault();

                                _dbDataCompare.Clear();
                                _dbDataCompare.Add(_data.PlanCode.ToString());
                                _dbDataCompare.Add((_data.SeqNo - 1).ToString());

                                inOrderCompare.Clear();
                                inOrderCompare.Add(inOrderList[i].PlanCode.ToString());
                                inOrderCompare.Add(inOrderList[i].SeqNo.ToString());

                                if (_DBStatus == inStatus)
                                {
                                    if (_dbDataCompare.SequenceEqual(inOrderCompare))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        DcICDHisOrderPlan(_data, Grv, false);
                                        insertICDHisOrderPlan(inOrderList[i], Grv, inStatus, true);//2022.12.29 update by 1050325
                                    }
                                }
                                else
                                {
                                    if (_dbDataCompare.SequenceEqual(inOrderCompare))
                                    {
                                        updateICDHisOrderPlan(inOrderList[i], Grv, inStatus, false);
                                    }
                                    else
                                    {
                                        DcICDHisOrderPlan(_data, Grv, false);
                                        insertICDHisOrderPlan(inOrderList[i], Grv, inStatus, true);//2022.12.29 update by 1050325
                                    }
                                }
                            }
                        }

                        result.isSuccess = true;

                        if (result.isSuccess)
                        {
                            _context.SaveChanges();
                            _ContextTransaction.Commit();
                        }
                        else
                        {
                            _ContextTransaction.Rollback();
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                result.isSuccess = false;
                result.Message = "Save Diagnosis Error：\n" + ex.ToString();
            }
            return Json(result);
        }

        public ResultDTO DcICDHisOrderPlan(Hisorderplan inOrder, GlobalVariableDTO inGrv, bool doSaveChange = true)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };

            try
            {
                inOrder.DcStatus = '2';
                inOrder.DcDate = DateTime.Now;
                inOrder.DcUser = inGrv.Login.EMPCODE;

                _context.Hisorderplans.Update(inOrder);

                if (doSaveChange == true)
                {
                    _context.SaveChanges();
                }

                result.isSuccess = true;
            }
            catch (Exception ex)
            {
                result.isSuccess = false;
                result.Message = ex.ToString();
            }

            return result;
        }

        public ResultDTO insertICDHisOrderPlan(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            try
            {

                inOrder.Orderplanid = -1;
                inOrder.Inhospid = inGrv.Patient.Inhospid;
                inOrder.HealthId = inGrv.Patient.RegPatientId;
                inOrder.HplanType = "ICD";
                inOrder.SeqNo++;
                inOrder.PlanDays = 1;
                inOrder.CreateDate = DateTime.Now;
                inOrder.CreateUser = inGrv.Login.EMPCODE;
                inOrder.Status = string.IsNullOrWhiteSpace(inStatus) ? '0' : '2';

                _context.Hisorderplans.Add(inOrder);

                if (doSaveChange)
                {
                    _context.SaveChanges();
                    _context.Entry(inOrder).Reload();
                }
                //result.returnValue = inOrder.Orderplanid != null ? inOrder.Orderplanid.ToString() : null;
                result.returnValue = inOrder.Orderplanid.ToString();
                result.isSuccess = true;
            }
            catch (SqlException sqlexc)
            {
                result.isSuccess = false;
                //_logger.LogError("Sql Exception: {exception} Patient Id: {patientid} Doctor Id: {doctorid} Date: {date}", sqlexc.Message, inGrv.Patient.RegPatientId, inGrv.Login.EMPCODE, DateTime.Now);
                result.Message = sqlexc.ToString();
            }
            catch (Exception ex)
            {
                result.isSuccess = false;
                //_logger.LogError("Exception: {exception} Patient Id: {patientdi} Doctor Id: {doctorid} Date {date}", ex.Message, inGrv.Patient.RegPatientId, inGrv.Login.EMPCODE, DateTime.Now);
                result.Message = ex.ToString();
            }
            return result;
        }

        public ResultDTO updateICDHisOrderPlan(Hisorderplan inOrder, GlobalVariableDTO inGrv, string inStatus, bool doSaveChange = true)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };

            try
            {
                if (string.IsNullOrWhiteSpace(inOrder.Orderplanid.ToString()))
                {
                    ResultDTO insertResult = insertICDHisOrderPlan(inOrder, inGrv, inStatus);

                    if (insertResult != null && insertResult.isSuccess == true && !string.IsNullOrWhiteSpace(insertResult.returnValue))
                    {
                        string NewOrderPlanId = insertResult.returnValue;

                        if (!string.IsNullOrWhiteSpace(NewOrderPlanId))
                        {
                            Hisorderplan data = _context.Hisorderplans.Where(c => c.Orderplanid.ToString() == NewOrderPlanId && c.Inhospid == inGrv.Patient.Inhospid && c.HplanType == "ICD").FirstOrDefault();

                            if (data != null)
                            {
                                data.Status = string.IsNullOrWhiteSpace(inStatus) ? '0' : '2';
                                data.ModifyDate = DateTime.Now;
                                data.ModifyUser = inGrv.Login.EMPCODE;

                                _context.Hisorderplans.Update(data);

                                if (doSaveChange == true)
                                {
                                    _context.SaveChanges();
                                }

                                result.isSuccess = true;
                            }
                            else
                            {
                                result.isSuccess = false;
                                result.Message = "Update Diagnosis Error, Orderplanid not found.\n Orderplanid:" + NewOrderPlanId;
                            }
                        }
                    }
                    else
                    {
                        result.isSuccess = false;
                        result.Message = "Update Diagnosis Error, Orderplanid is null.\n IcdCode:" + inOrder.PlanCode;
                    }
                }
                else
                {
                    Hisorderplan data = _context.Hisorderplans.Where(c => c.Orderplanid == inOrder.Orderplanid && c.Inhospid == inGrv.Patient.Inhospid && c.HplanType == "ICD").FirstOrDefault();

                    if (data != null)
                    {
                        data.Status = string.IsNullOrWhiteSpace(inStatus) ? '0' : '2';
                        data.ModifyDate = DateTime.Now;
                        data.ModifyUser = inGrv.Login.EMPCODE;

                        _context.Hisorderplans.Update(data);

                        if (doSaveChange == true)
                        {
                            _context.SaveChanges();
                        }

                        result.isSuccess = true;
                    }
                    else
                    {
                        result.isSuccess = false;
                        result.Message = "Update Diagnosis Error, Orderplanid not found.\n Orderplanid:" + inOrder.Orderplanid.ToString();
                    }
                }
            }
            catch (SqlException sqlex)
            {
                //_logger.LogError("Sql Exception: {exceptin} Patient Id: {patientid} Doctor Id: {doctorid} Date: {date}", sqlex.Message, inGrv.Patient.RegPatientId, inGrv.Login.EMPCODE, DateTime.Now);
            }
            catch (Exception ex)
            {
                //_logger.LogError("Exception: {exception} Patient Id: {patientid} Doctor Id: {doctorid} Date: {date}", ex.Message, inGrv.Patient.RegPatientId, inGrv.Login.EMPCODE, DateTime.Now);

                result.isSuccess = false;
                result.Message = "Update Diagnosis Error：\n" + ex.ToString();
            }

            return result;
        }
    }
}
