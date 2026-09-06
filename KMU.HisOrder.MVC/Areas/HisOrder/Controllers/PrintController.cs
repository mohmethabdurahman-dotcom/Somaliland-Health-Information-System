using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using NLog.Time;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    [Authorize(Roles = "HisOrder")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class PrintController : Controller
    {
        private readonly KMUContext _context;
        private PrintClass GlobalPrintData = new PrintClass();

        public PrintController(KMUContext context)
        {
            _context = context;
        }

        private void SetGlobalPrintData(string inHospID = null, string inOrderplanids = null)
        {
            try
            {
                using (HisOrderController col = new HisOrderController(_context))
                {
                    #region Get GlobalVariable Data

                    GlobalVariableDTO grv = new GlobalVariableDTO();

                    using (HisOrderService service = new HisOrderService(_context))
                    {
                        if (inHospID == null)
                        {
                            // ClinicOrder Examing、Confirm Print and RePrint
                            grv = col.getGlobalVariablDTO(HttpContext);
                            grv = service.getGlobalVariableDTO(grv.Patient.Inhospid, HttpContext);
                        }
                        else
                        {
                            // Other Reprint
                            grv = service.getGlobalVariableDTO(inHospID, HttpContext);
                        }
                    }

                    #endregion

                    #region Set GlobalPrint Data

                    GlobalPrintData.HospName = _context.KmuCoderefs.Where(c => c.RefCodetype == "HospitalCode").Select(c => c.RefName).FirstOrDefault().ToString();
                    GlobalPrintData.GlobalVariable = grv;
                    GlobalPrintData.OrderData = _context.Hisorderplans.Where(c => c.Inhospid == grv.Patient.Inhospid && (c.Status == '0' || c.Status == '2') && c.DcDate == null).OrderBy(c => c.SeqNo).ToList();

                    /// Add Print All.  2023.06.20 add by 1100079
                    if (inHospID == null && inOrderplanids == null)
                    {
                        // 中文：只列印還沒印過的
                        // English：Confirm or Pending Click,Med and NonMed only print unprinted.
                        GlobalPrintData.OrderData = GlobalPrintData.OrderData.Where(c => c.HplanType == "ICD" || (c.HplanType != "ICD" && string.IsNullOrWhiteSpace(c.PrintDate.ToString()))).ToList();
                    }
                    else
                    {
                        if (inOrderplanids != null)
                        {
                            // Reprint：Print only selected items (ClinicOrder RePrint)
                            GlobalPrintData.OrderData = GlobalPrintData.OrderData.Where(c => inOrderplanids.Split(',').Contains(c.Orderplanid.ToString().Trim()) || (c.Inhospid == grv.Patient.Inhospid && c.HplanType == "ICD")).ToList();
                        }
                        // Reprint：Other (Print All)
                    }

                    List<KmuMedpathway> DosePathData = _context.KmuMedpathways.ToList();

                    foreach (Hisorderplan item in GlobalPrintData.OrderData)
                    {
                        // DosePath holds blood type for Blood orders — do not remap as med pathway.
                        if (item.HplanType == "Blood")
                        {
                            continue;
                        }

                        // DosePath full name
                        item.DosePath = DosePathData.Where(c => c.PathCode == item.DosePath).Select(c => c.PathDesc).FirstOrDefault();
                    }
                    List<Hisordersoa> Soalist = _context.Hisordersoas
                        .Where(c => c.Inhospid == grv.Patient.Inhospid &&
                            (
                                (grv.Clinic.InhospType == "OPD" && c.SourceType == "OPD") ||
                                (grv.Clinic.InhospType == "EMG" && c.SourceType == "EMG") ||
                                (grv.Clinic.InhospType != "OPD" && grv.Clinic.InhospType != "EMG" && c.SourceType == "WARD")
                            ))
                        .OrderBy(c => c.VersionCode)
                        .ToList();

                    int VersionCount = Soalist.GroupBy(c => c.VersionCode).Count();
                    string ShowFontStyle = _context.KmuCoderefs.Where(c => c.RefCodetype == "PrintMedicalStyle").Select(c => c.RefCode).FirstOrDefault();

                    GlobalPrintData.Ordersoa = new List<PrintHisordersoa>();
                    for (int i = 0; i < VersionCount; i++)
                    {
                        PrintHisordersoa Ordersoa = new PrintHisordersoa();
                        Ordersoa.ClinicRemarks = Soalist.Where(c => c.Kind == "CM" && c.VersionCode == i + 1).Select(c => c.Context).LastOrDefault();
                        Ordersoa.ClinicRemarks = Ordersoa.ClinicRemarks ?? "";
                        Ordersoa.Management = Soalist.Where(c => c.Kind == "MG" && c.VersionCode == i + 1).Select(c => c.Context).LastOrDefault();
                        Ordersoa.Management = Ordersoa.Management ?? "";
                        Ordersoa.Transfer = _context.Registrations.Where(c => c.Inhospid == grv.Patient.Inhospid && c.RegFollowCode == "*").Select(c => c.RegFollowDesc).FirstOrDefault();
                        Ordersoa.Version = (i + 1).ToString();
                        Ordersoa.CreateDate = Soalist.Where(c => c.VersionCode == i + 1).Select(c => c.CreateDate).FirstOrDefault();

                        if (ShowFontStyle != "Y")
                        {
                            // Decode html tag
                            // GlobalPrintData.ClinicRemarks = WebUtility.HtmlDecode(GlobalPrintData.ClinicRemarks);
                            // GlobalPrintData.Management = WebUtility.HtmlDecode(GlobalPrintData.Management);
                            //GlobalPrintData.ClinicRemarks = Regex.Replace(GlobalPrintData.ClinicRemarks, @"<(.|\n)*?>", string.Empty);
                            //GlobalPrintData.Management = Regex.Replace(GlobalPrintData.Management, @"<(.|\n)*?>", string.Empty);
                            //GlobalPrintData.ClinicRemarks = Regex.Replace(GlobalPrintData.ClinicRemarks, @"<[^>]+>|&nbsp;", string.Empty);
                            //GlobalPrintData.Management = Regex.Replace(GlobalPrintData.Management, @"<[^>]+>|&nbsp;", string.Empty);

                            // Remove text style
                            Ordersoa.ClinicRemarks = Regex.Replace(Ordersoa.ClinicRemarks, @"<.*?>", string.Empty);
                            Ordersoa.Management = Regex.Replace(Ordersoa.Management, @"<.*?>", string.Empty);
                        }

                        Ordersoa.ClinicRemarks = Ordersoa.ClinicRemarks.Replace("\n\n", "\n");
                        Ordersoa.Management = Ordersoa.Management.Replace("\n\n", "\n");


                        GlobalPrintData.Ordersoa.Add(Ordersoa);
                    }

                    ViewData["Locations"] = _context.KmuCoderefs.Where(c => c.RefCodetype == "NonMedLocation").ToList();
                    ViewData["Title"] = grv.Clinic.RegDate.ToString() + "_" + grv.Clinic.DeptName + "_" + grv.Patient.RegPatientId;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public PartialViewResult PrintForm(string inStatus, string RePrint = "N", string inHospID = null, string inOrderplanids = null)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };

            try
            {
                if (RePrint == "N")
                {
                    SetGlobalPrintData();
                    // Display or not the print page nodes.
                    GlobalPrintData.ShowPrint = new List<string>
                    {

                        CheckBeforePrint_Prescription(GlobalPrintData, inStatus),
                        CheckBeforePrint_Laboratory(GlobalPrintData, inStatus),
                        CheckBeforePrint_LaboratoryER(GlobalPrintData, inStatus),
                        CheckBeforePrint_Examination(GlobalPrintData, inStatus),
                        CheckBeforePrint_Pathology(GlobalPrintData, inStatus),
                        CheckBeforePrint_Supply(GlobalPrintData, inStatus),
                        CheckBeforePrint_Blood(GlobalPrintData, inStatus),
                           CheckBeforePrint_MedicalRecord(GlobalPrintData, inStatus)
                    };
                    GlobalPrintData.ShowPrint.RemoveAll(c => string.IsNullOrWhiteSpace(c));
                }
                else
                {
                    SetGlobalPrintData(inHospID, inOrderplanids);
                    GlobalPrintData.ShowPrint = new List<string>
                    {
                        CheckBeforePrint_Prescription(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_Laboratory(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_LaboratoryER(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_Examination(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_Pathology(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_Blood(GlobalPrintData, "RePrint"),
                        CheckBeforePrint_Supply(GlobalPrintData, "RePrint")
                    };
                    GlobalPrintData.ShowPrint.RemoveAll(c => string.IsNullOrWhiteSpace(c));
                    GlobalPrintData.RePrintInHospID = inHospID;
                    GlobalPrintData.RePrintOrderplanids = inOrderplanids;
                }

                result.isSuccess = true;
            }
            catch (Exception ex)
            {
                result.isSuccess = false;

                ViewData["ErrorMessage"] = "Order print error：" + ex.ToString();
                return PartialView("Views/Shared/ErrorPage.cshtml");
            }

            return PartialView("Areas/HisOrder/Views/Print/_PrintMenu.cshtml", GlobalPrintData);
        }

        public ActionResult generateOrderPrint(string inPages, string inHospID, string RePrintOrderplanids)
        {
            if (string.IsNullOrWhiteSpace(RePrintOrderplanids) && string.IsNullOrWhiteSpace(inHospID))
            {
                SetGlobalPrintData();
            }
            else
            {
                SetGlobalPrintData(inHospID, RePrintOrderplanids);
            }

            GlobalPrintData.ShowPrint = inPages.Split(",").ToList();

            using (HisOrderController col = new HisOrderController(_context))
            {
                GlobalVariableDTO grv = col.getGlobalVariablDTO(HttpContext);

                foreach (Hisorderplan data in GlobalPrintData.OrderData) // All Print Data.
                {
                    bool UpdatePrintData = false;
                    switch (data.HplanType)
                    {
                        case "Med":
                            if (GlobalPrintData.ShowPrint.Contains("Prescription"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                        case "Lab":
                            if (GlobalPrintData.ShowPrint.Contains("Laboratory") || GlobalPrintData.ShowPrint.Contains("Emergency Laboratory"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                        case "Exam":
                            if (GlobalPrintData.ShowPrint.Contains("Radiology"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                        case "Path":
                            if (GlobalPrintData.ShowPrint.Contains("Pathology"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                        case "Blood":
                            if (GlobalPrintData.ShowPrint.Contains("Blood"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                        default:
                            if (!GlobalPrintData.ShowPrint.Contains("Prescription") &&
                                !GlobalPrintData.ShowPrint.Contains("Laboratory") &&
                                !GlobalPrintData.ShowPrint.Contains("Emergency Laboratory") &&
                                !GlobalPrintData.ShowPrint.Contains("Radiology") &&
                                !GlobalPrintData.ShowPrint.Contains("Blood") &&
                                !GlobalPrintData.ShowPrint.Contains("Pathology"))
                            {
                                UpdatePrintData = true;
                            }
                            break;
                    }

                    if (UpdatePrintData == true)
                    {
                        UpdatePrintDate(data, grv);
                    }
                }
            }

            return PartialView("Areas/HisOrder/Views/Print/_PrintTemplate.cshtml", GlobalPrintData);
        }

        private ResultDTO UpdatePrintDate(Hisorderplan inOrder, GlobalVariableDTO inGrv, bool doSaveChange = true)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };

            try
            {
                Hisorderplan data = _context.Hisorderplans.Where(c => c.Orderplanid.ToString() == inOrder.Orderplanid.ToString()).FirstOrDefault();

                if (data != null)
                {
                    data.PrintDate = DateTime.Now;
                    data.PrintUser = inGrv.Login.EMPCODE;
                    _context.Hisorderplans.Update(data);

                    if (doSaveChange == true)
                    {
                        _context.SaveChanges();
                    }
                    result.isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                result.isSuccess = false;
                result.Message = "Update PrintDate Error.\n Message:" + ex.ToString();
            }

            return result;
        }

        private string CheckBeforePrint_MedicalRecord(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "ICD").Count() > 0)
            {
                isPrint = true;
            }

            if (inPrintData.Ordersoa != null && inPrintData.Ordersoa.Count > 0)
            {
                foreach (PrintHisordersoa soa in inPrintData.Ordersoa)
                {
                    if (!string.IsNullOrWhiteSpace(soa.ClinicRemarks) || !string.IsNullOrWhiteSpace(soa.Management))
                    {
                        isPrint = true;
                        break;
                    }
                }
            }

            //if (!string.IsNullOrWhiteSpace(inPrintData.ClinicRemarks))
            //{
            //    isPrint = true;
            //}

            //if (!string.IsNullOrWhiteSpace(inPrintData.Management))
            //{
            //    isPrint = true;
            //}

            if (isPrint == true)
            {
                return "MedicalRecord";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_Prescription(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "Med").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Prescription";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_Laboratory(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != null && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "Lab").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Laboratory";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_LaboratoryER(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != null && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "Lab").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Emergency Laboratory";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_Examination(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != null && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "Exam").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Radiology";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_Pathology(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != null && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType == "Path").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Pathology";
            }
            else
            {
                return "";
            }
        }

        private string CheckBeforePrint_Blood(PrintClass inPrintData, string inStatus)
        {
            // Blood can print after save (status 0) — Fin/confirm not required.
            if (inPrintData.OrderData.Where(c => c.HplanType == "Blood").Count() > 0)
            {
                return "Blood";
            }

            return "";
        }

        private string CheckBeforePrint_Supply(PrintClass inPrintData, string inStatus)
        {
            if (inStatus != "confirm" && inStatus != null && inStatus != "RePrint") { return ""; }

            bool isPrint = false;

            if (inPrintData.OrderData.Where(c => c.HplanType != "Lab" && c.HplanType != "Exam" && c.HplanType != "Path" && c.HplanType != "ICD" && c.HplanType != "Med" && c.HplanType != "Blood").Count() > 0)
            {
                isPrint = true;
            }

            if (isPrint == true)
            {
                return "Material";
            }
            else
            {
                return "";
            }
        }
    }
}
