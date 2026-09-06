
using AutoMapper;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class SoapController : Controller
    {
        private readonly KMUContext _context;
        private readonly IMapper _mapper;

        public SoapController(KMUContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public string ModifySoap(string inhospid, string inKind)
        {
            return null;
        }

        [HttpGet]
        public string getSoapData(string inhospid, string inVersion)
        {
            using (SoapService service = new SoapService(_context))
            {
                ResultDTO result = new ResultDTO() { isSuccess = false };


                var regdata = _context.Registrations.Where(c => c.Inhospid == inhospid).FirstOrDefault();
                var souretype = (regdata != null && regdata.RegDepartment.Substring(0, 2) != "16") ? "OPD" : "EMG";
                int? targetVar = string.IsNullOrWhiteSpace(inVersion) ? null : Convert.ToInt32(inVersion);
                var data = service.getSoapData(inhospid, souretype, targetVar);
                if (data != null)
                {
                    result.isSuccess = true;
                    result.returnValue = JsonConvert.SerializeObject(data);
                    return JsonConvert.SerializeObject(result);
                }
                else
                {
                    return "";
                }

            }
        }

        [HttpPost]
        public string deleteSoapData(string inhospid, string inTargetVersion)
        {
            ResultDTO result = new ResultDTO() { isSuccess = false };
            GlobalVariableDTO grv = null;
            int parseTargetVersion = 0;
            if (string.IsNullOrWhiteSpace(inhospid))
            {
                return JsonConvert.SerializeObject(result);
            }

            if (string.IsNullOrWhiteSpace(inTargetVersion))
            {
                return JsonConvert.SerializeObject(result);
            }

            using (HisOrderController col = new HisOrderController(_context))
            {
                var _sysdate = DateTime.Now;
                grv = col.getGlobalVariablDTO(HttpContext);
                Int32.TryParse(inTargetVersion, out parseTargetVersion);
                var dataList = _context.Hisordersoas.Where(c => c.Inhospid == inhospid && c.VersionCode == parseTargetVersion).ToList();
                foreach (var obj in dataList)
                {
                    obj.Status = 'X';
                    obj.DcUser = grv.Login.EMPCODE;
                    obj.DcDate = _sysdate;
                }

                _context.UpdateRange(dataList);
                _context.SaveChanges();
                result.isSuccess = true;
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string saveSoapData(string inhospid, string inData)
        {
            try
            {
                ResultDTO result = new ResultDTO() { isSuccess = false };
                GlobalVariableDTO grv = null;

                using (HisOrderController col = new HisOrderController(_context))
                {
                    grv = col.getGlobalVariablDTO(HttpContext);
                    var data = (List<Hisordersoa>)JsonConvert.DeserializeObject(inData, typeof(List<Hisordersoa>));
                    if (data != null)
                    {
                        int lastVersion = 0;
                        //門診版本
                        if (data.FirstOrDefault().SourceType == "OPD")
                        {
                            //OPD只會有一個版本取畫面上的即可
                            lastVersion = (int)(data.Select(c => c.VersionCode).Max() == null ? 0 : data.Select(c => c.VersionCode).Max());
                        }
                        else
                        {
                            //ER
                            var dbMaxVersion = _context.Hisordersoas.Where(c => c.Inhospid == inhospid).Max(d => d.VersionCode).GetValueOrDefault();
                            var uiMaxVersion = (int)(data.Select(c => c.VersionCode).Max() == null ? 0 : data.Select(c => c.VersionCode).Max());
                            //代表是空的
                            if (uiMaxVersion == 0)
                            {
                                //代表db有存檔過
                                if (dbMaxVersion > 0)
                                {
                                    lastVersion = dbMaxVersion;
                                }
                                else
                                {
                                    lastVersion = 0;
                                }
                            }
                            else
                            {
                                lastVersion = (int)(data.Select(c => c.VersionCode).Max() == null ? 0 : data.Select(c => c.VersionCode).Max());
                            }
                        }


                        DateTime system = DateTime.Now;
                        List<Hisordersoa> oldDataList = new List<Hisordersoa>();

                        foreach (var obj in data)
                        {
                            if (obj.Soaid == null || obj.Soaid == -1)
                            {
                                obj.Soaid = -1;
                                obj.CreateDate = system;
                                obj.CreateUser = grv.Login.EMPCODE;
                                obj.VersionCode = lastVersion + 1;
                                obj.Status = 'V';
                                _context.Hisordersoas.Add(obj);
                                _context.SaveChanges();
                                _context.Entry(obj).Reload();
                            }
                            else
                            {
                                var oldData = _context.Hisordersoas
                           .FirstOrDefault(c => c.Soaid == obj.Soaid
                                             && c.Inhospid == obj.Inhospid);
                                if (oldData != null)
                                {
                                    oldData.Context = obj.Context;

                                    // ← ai suggestion
                                    oldData.Aigeneratedcontent = obj.Aigeneratedcontent;

                                    // ←ai modified column
                                    oldData.Ai_modified = obj.Ai_modified;
                                    // original text preserved
                                    oldData.OriginalRemark = obj.OriginalRemark;

                                    oldDataList.Add(oldData);
                                }
                            }
                        }

                        if (oldDataList.Count > 0)
                        {
                            _context.Hisordersoas.UpdateRange(oldDataList);

                        }

                        _context.SaveChanges();
                        result.isSuccess = true;
                    }
                    return JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false });
            }
        }


        [HttpPost]
        public string getSoapVerList(string inhospid)
        {
            var result = new ResultDTO() { isSuccess = false };
            var dicValue = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(inhospid))
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false });
            }
            else
            {
                var queryData = querySoapVer(inhospid);
                result.isSuccess = true;
                result.returnValue = JsonConvert.SerializeObject(queryData);

                return JsonConvert.SerializeObject(result);
            }
        }

        public List<hisordersoa_version> querySoapVer(string inhospid)
        {
            if (string.IsNullOrWhiteSpace(inhospid))
            {
                return new List<hisordersoa_version>();
            }
            else
            {
                var queryData = _context.Hisordersoas
                  .Where(c => c.Inhospid == inhospid && c.Status != 'X')
                  .GroupBy(d => new { d.VersionCode, d.CreateDate }).Select(d => new hisordersoa_version
                  {
                      VersionCode = Convert.ToInt32(d.Key.VersionCode),
                      CreateDate = d.Key.CreateDate,
                      Des = String.Format("ver{0}. {1}", d.Key.VersionCode.ToString(), d.Key.CreateDate.ToString("dd/MM/yyyy HH:mm"))
                  })
                  .ToList();

                return queryData.OrderByDescending(d => d.VersionCode).ToList();
            }

        }


        public List<KmuCoderef> getMgInfo()
        {
            try
            {
                return _context.KmuCoderefs.Where(c => c.RefCodetype == "MG_INFO").OrderBy(d => d.RefShowseq).ToList();
            }
            catch
            {
                return null;
            }

        }
    }
}

