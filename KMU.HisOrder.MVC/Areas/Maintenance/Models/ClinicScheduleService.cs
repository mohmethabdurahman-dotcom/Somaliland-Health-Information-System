using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Extesion;
//using KMU.HisOrder.MVC.Migrations;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Linq;
using static KMU.HisOrder.MVC.Models.EnumClass;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Models
{
    public class ClinicScheduleService : IDisposable
    {
        private readonly KMUContext _context;


        public ClinicScheduleService(KMUContext context)
        {
            _context = context;
        }


    

        public void Dispose() => Dispose(disposing: true);

        /// <summary>
        /// Releases all resources currently used by this <see cref="Controller"/> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="Dispose()"/> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        #region Public Function

        /// <summary>
        /// 取得診間清單
        /// </summary>
        /// <param name="week"></param>
        /// <param name="room"></param>
        /// <param name="type"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ScheduleListReturn GetScheduleDataForMaintain(string week, string room, string type, EnumClass.DisplayLanguage language)
        {
            #region Variable Setting
            
            ScheduleListReturn result = new ScheduleListReturn();
            List<ScheduleData> sdList = new List<ScheduleData>();
            List<ClinicSchedule> clinicList = new List<ClinicSchedule>();

            var listsche = _context.ClinicSchedules.Where(c => c.ScheWeek == week).ToList();
            #endregion

            #region Step 1. Check Input Variables

            if (string.IsNullOrEmpty(week) &&
                string.IsNullOrEmpty(room) &&
                string.IsNullOrEmpty(type))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            using (DepartmentService deptService = new DepartmentService(_context))
            {
                try
                {
                    switch (type)
                    {
                        case "Week":
                            clinicList = _context.ClinicSchedules.Where(c => c.ScheWeek == week).ToList();
                            break;
                        case "Room":
                            if (!string.IsNullOrEmpty(room))
                            {
                                clinicList = _context.ClinicSchedules.Where(c => c.ScheRoom == room).ToList();
                            }
                            else
                            {
                                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                                return result;
                            }
                            break;
                    }

                    if (clinicList.Any())
                    {
                        foreach (var item in clinicList)
                        {
                            ScheduleData sd = new ScheduleData()
                            {
                                SCHE_WEEK = item.ScheWeek,
                                SCHE_Shift = item.shift,
                                SCHE_NOON = item.ScheNoon,
                                SCHE_ROOM = item.ScheRoom,
                                SCHE_DPT = item.ScheDptCode,
                                SCHE_DPT_NAME = item.ScheDptName,
                                SCHE_DOCTOR_CODE = item.ScheDoctor,
                                SCHE_DOCTOR_NAME = item.ScheDoctorName,
                                DEFAULT_ATTR = deptService.getDefaultAttribute(item.ScheDptCode),
                                REMARK = item.ScheRemark,
                                enumOpenFlag = item.ScheOpenFlag == EnumClass.OpenFlag.On.EnumToCode() ? EnumClass.OpenFlag.On : EnumClass.OpenFlag.Off,
                                SCHE_PARENT_DPT = deptService.getParentDpt(item.ScheDptCode),
                                DPT_CATEGORY = deptService.getCategory(item.ScheDptCode)
                            };

                            sd.SCHE_PARENT_DPT_NAME = deptService.getDeptName(sd.SCHE_PARENT_DPT);

                            sdList.Add(sd);
                        }

                        sdList = sdList.OrderBy(c => c.SCHE_WEEK).ThenBy(c => c.SCHE_ROOM).ToList();

                        result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ReplyOK, language);
                        result.sdList = sdList;
                    }
                    else
                    {
                        result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ReplyOK, language);
                        result.sdList = sdList;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                    result.sdList = new List<ScheduleData>();

                    return result;
                }
            }

            return result;

        }

        /// <summary>
        /// 更新開診狀態
        /// </summary>
        /// <param name="week"></param>
        /// <param name="noon"></param>
        /// <param name="room"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public OpenFlagReturn UpdateOpenFlag(string week, string shift, string noon, string room, string ModifyID, EnumClass.DisplayLanguage language)
        {
            #region Variable Setting

            OpenFlagReturn result = new OpenFlagReturn();
            List<ClinicSchedule> clinicList = new List<ClinicSchedule>();
            string? originalFlag = "";
            #endregion

            #region Step 1. Check Input Variables

            if (string.IsNullOrEmpty(week) &&
                string.IsNullOrEmpty(noon) &&
                string.IsNullOrEmpty(room))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            try
            {
                clinicList = _context.ClinicSchedules.Where(c => c.ScheWeek == week &&
                                                               c.ScheNoon == noon &&
                                                               c.ScheRoom == room && c.shift == shift).ToList();
                if (clinicList.Any())
                {
                    ClinicSchedule clinic = clinicList.First();
                    clinic = clinicList.First();

                    originalFlag = clinic.ScheOpenFlag;

                    if (originalFlag == "N")
                    {
                        clinic.ScheOpenFlag = EnumClass.OpenFlag.On.EnumToCode();
                    }
                    else
                    {
                        clinic.ScheOpenFlag = EnumClass.OpenFlag.Off.EnumToCode();
                    }

                    clinic.ModifyUser = ModifyID;
                    clinic.ModifyTime = DateTime.Now;
                    _context.SaveChanges();

                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ReplyOK, language);
                    result.NewFlag = clinic.ScheOpenFlag;
                }
                else
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                    return result;
                }
            }
            catch (Exception ex)
            {

                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);

                return result;
            }

            return result;
        }

        public ScheduleData GetScheduleModal(string week, string noon, string room, string shift)
        {
            #region Variable Setting

            ScheduleData result = new ScheduleData();

            #endregion

            try
            {
                var Dtos = _context.ClinicSchedules.Where(c => c.ScheWeek == week &&
                                                             c.ScheNoon == noon &&
                                                             c.ScheRoom == room && c.shift == shift);

                if (Dtos.Any())
                {
                    using (DepartmentService deptService = new DepartmentService(_context))
                    {
                        ClinicSchedule clinic = Dtos.First();
                        clinic = Dtos.First();

                        result.SCHE_WEEK = clinic.ScheWeek;
                        result.SCHE_Shift = clinic.shift;
                        result.SCHE_NOON = clinic.ScheNoon;
                        result.SCHE_ROOM = clinic.ScheRoom;
                        result.SCHE_DPT = clinic.ScheDptCode;
                        result.SCHE_DPT_NAME = clinic.ScheDptName;
                        result.SCHE_DOCTOR_CODE = clinic.ScheDoctor;
                        result.SCHE_DOCTOR_NAME = clinic.ScheDoctorName;
                        result.REMARK = clinic.ScheRemark;
                        foreach (EnumClass.OpenFlag open in Enum.GetValues(typeof(OpenFlag)))
                        {
                            if (open.EnumToCode() == clinic.ScheOpenFlag)
                            {
                                result.enumOpenFlag = open;
                                break;
                            }
                        }
                        result.SCHE_PARENT_DPT = deptService.getParentDpt(clinic.ScheDptCode);
                        result.DPT_CATEGORY = deptService.getCategory(clinic.ScheDptCode);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        public ReturnMsg ModifyScheduleData(string week,
            string Shift,
            string noon,
            string room,
            string dpt,
            string dptName,
            string doctor,
            string doctorName,
            string openFlag,
            string remark,
            string ModifyID,
            string action,
            string oldweek, string oldshift, string oldnoon, string oldroom, string olddepartment, string olddoctor,
            //string CurdptId,
            //string CurdptName,
            DisplayLanguage language)
        {
            #region Variable Setting

            ReturnMsg result = new ReturnMsg();
            List<ClinicSchedule> clinicList = new List<ClinicSchedule>();
            List<KmuUser> userList = new List<KmuUser>();
            List<KmuDepartment> deptList = new List<KmuDepartment>();
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;
            #endregion

            #region Step 1. Check Input Variables

            if (string.IsNullOrEmpty(week) ||
                string.IsNullOrEmpty(Shift) ||
                string.IsNullOrEmpty(noon) ||
                string.IsNullOrEmpty(room) ||
                string.IsNullOrEmpty(dpt) ||
                //string.IsNullOrEmpty(doctor) || //急診醫師非必填 2023.05.03 modify by elain
                string.IsNullOrEmpty(ModifyID))
            {
                result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            #region Step 2. Check Doctor ID & Department Code            

            deptList = _context.KmuDepartments.Where(c => c.DptCode == dpt).ToList();
            var deptDuplicate = _context.ClinicSchedules.Where(c=> c.ScheWeek == week && c.shift == Shift && c.ScheRoom == room || (c.ScheDptCode == dpt && c.ScheWeek == week && c.ScheRoom != room)).ToList();

            // Checking if there is duplicate deparment Schedule
            if (deptDuplicate.Any() && action == "Create")
            {
                replyCode = ReplyNoCode.DepartmentExist;
                result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                return result;
            }



            if (!deptList.Any())
            {
                result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.DeptIsntExist, language);

                return result;
            }
            else
            {
                //急診醫師非必填 2023.05.03 modify by elain
                if (deptList.First().DptCategory != "EMG" && string.IsNullOrEmpty(doctor) == true)
                {
                    result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                    return result;
                }
            }

            if (string.IsNullOrEmpty(doctor) == false)
            {
                userList = _context.KmuUsers.Where(c => c.UserIdno == doctor).ToList();

                if (!userList.Any())
                {
                    result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.DoctorIsntExist, language);

                    return result;
                }
            }

            #endregion

            try
            {
                clinicList = _context.ClinicSchedules.Where(c => c.ScheWeek == oldweek &&
                                                               c.ScheNoon == oldnoon &&
                                                               c.ScheRoom == oldroom && c.shift == oldshift).ToList();

                if (clinicList.Any() && action == "Create")
                {
                    replyCode = ReplyNoCode.ClinicExist;
                    result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                    return result;
                }

                if (!clinicList.Any() && action == "Create")
                {
                    //Create
                    replyCode = CreateClinicSchedule(week,Shift, noon, room, dpt, dptName, doctor, doctorName, openFlag, remark, ModifyID);
                    result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                }
                else
                {
                    //Update
                    ClinicSchedule clinic = clinicList.FirstOrDefault();

                  
                    try
                    {
                         var updatedeptDuplicate = _context.ClinicSchedules.Where(c=> c.ScheWeek == week && c.shift == Shift && c.ScheRoom == room).ToList();

                        bool changeweek = oldweek != week || oldnoon != noon || oldroom != room || oldshift != Shift;
                        bool changedpt = olddoctor != doctor || olddepartment != dpt;

                        if (changedpt == true && changeweek == true && action == "Update") 
                        {
                            if (updatedeptDuplicate.Any())
                            {
                                replyCode = ReplyNoCode.DepartmentExist;
                                result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                                return result;
                            }
                        }

                        if(changedpt == false && changeweek == false && action == "Update")
                        {
                            replyCode = ReplyNoCode.DepartmentExist;
                            result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                            return result;
                        }

                        if(changedpt == false && changeweek == true  && action == "Update")
                        {
                            if (updatedeptDuplicate.Any())
                            {
                                replyCode = ReplyNoCode.DepartmentExist;
                                result = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                                return result;
                            }
                        }

                        
                        
                        // Checking if there is duplicate deparment Schedule
                        //if (clinic.ScheDptCode != dpt)
                        //{
                      
                        //}
                        var finddoctor = _context.Registrations.Where(r => r.RegDepartment == dpt && r.RegDate.Month == DateTime.Today.Month && r.RegDate.Day == DateTime.Today.Day);
                    
                        foreach(var item in finddoctor)
                        {
                            item.RegDoctorId = doctor;
                            item.ModifyTime = DateTime.Now;
                            item.ModifyUser = ModifyID;
                            _context.Update(item);
                        }

                        _context.ClinicSchedules.Remove(clinic);
                        _context.SaveChanges();
                        clinic.ScheWeek = week;
                        clinic.ScheRoom = room;                
                        clinic.ScheNoon = noon;
                        clinic.ScheDptCode = dpt;
                        clinic.shift = Shift;
                        clinic.ScheDptName = dptName;
                        clinic.ScheDoctor = doctor;
                        clinic.ScheDoctorName = doctorName;
                        clinic.ScheOpenFlag = openFlag;
                        clinic.ScheRemark = remark;

                        clinic.ModifyUser = ModifyID;
                        clinic.ModifyTime = DateTime.Now;
                        _context.Add(clinic);
                        _context.SaveChanges();

                        result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ReplyOK, language);



                    }
                    catch (Exception ex)
                    {
                        result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);

                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
                result = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);

                return result;
            }

            return result;
        }

        public List<ScheduleData> getCanReserveClinicByDate(DateTime date, string reserveType)
        {
            #region Variable Setting

            List<ClinicSchedule> clinicList = new List<ClinicSchedule>();
            List<ScheduleData> scheduleList = new List<ScheduleData>();

            #endregion

            using (DepartmentService deptService = new DepartmentService(_context))
            {

                try
                {
                    var shift = "";
                    var ShiftA = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift A").RefDes;
                    var ShiftB = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift B").RefDes;
                    var ShiftC = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift C").RefDes;
                
                    //string ShiftA = "07:30:00";
                    //string ShiftB = "13:30:00";
                    //string ShiftC = "19:30:00";

                    TimeSpan duration = DateTime.Now.TimeOfDay;


                    if (duration > TimeSpan.Parse(ShiftA) && duration < TimeSpan.Parse(ShiftB))
                    {
                        shift = "Shift A";
                    }
                    else if (duration > TimeSpan.Parse(ShiftB) && duration < TimeSpan.Parse(ShiftC))
                    {
                        shift = "Shift B";
                    }
                   else
                    {
                        shift = "Shift C";
                    }

        

                    clinicList = (from c in _context.ClinicSchedules.Where(c => c.ScheWeek.Equals(date.DayOfWeek.ToString("d")) && (c.shift == shift || c.shift == "All Time") &&
                                                                     c.ScheOpenFlag.Equals(EnumClass.OpenFlag.On.EnumToCode()))
                                  join d in _context.KmuDepartments.Where(c => c.DptCategory == reserveType)
                                  on c.ScheDptCode equals d.DptCode
                                  select c).ToList();


                    foreach (var item in clinicList)
                    {
                        ScheduleData schedule = new ScheduleData();
                        schedule.SCHE_Shift = item.shift.ToString();
                        schedule.SCHE_WEEK = item.ScheWeek.ToString();
                        schedule.SCHE_ROOM = item.ScheRoom.ToString();
                        schedule.SCHE_NOON = item.ScheNoon.ToString();
                        schedule.SCHE_DOCTOR_CODE = item.ScheDoctor;
                        schedule.SCHE_DOCTOR_NAME = item.ScheDoctorName;
                        schedule.SCHE_DPT = item.ScheDptCode;
                        schedule.SCHE_DPT_NAME = item.ScheDptName;
                        schedule.DEFAULT_ATTR = deptService.getDefaultAttribute(item.ScheDptCode);
                        schedule.SCHE_PARENT_DPT = deptService.getParentDpt(item.ScheDptCode);
                        schedule.SCHE_PARENT_DPT_NAME = deptService.getDeptName(schedule.SCHE_PARENT_DPT);
                        foreach (EnumClass.OpenFlag open in Enum.GetValues(typeof(OpenFlag)))
                        {
                            if (open.EnumToCode() == item.ScheOpenFlag)
                            {
                                schedule.SCHE_DPT_NAME = item.ScheDptName;
                                break;
                            }
                        }

                        scheduleList.Add(schedule);
                    }

                    scheduleList = scheduleList
                        .OrderBy(s => IsPrisonClinic(s.SCHE_DPT_NAME, s.SCHE_PARENT_DPT_NAME) ? 1 : 0)
                        .ThenBy(s => s.SCHE_PARENT_DPT_NAME)
                        .ThenBy(s => s.SCHE_DPT_NAME)
                        .ThenBy(s => s.SCHE_ROOM)
                        .ToList();
                }
                catch (Exception ex)
                {

                }
            }
            return scheduleList;
        }

        public List<ClinicScheduleItem> GetScheduleDataForHisOrder(string[] SoucreType, DateOnly ClinicDate, string? DoctorCode)
        {
            List<ClinicScheduleItem> scheduleList = new List<ClinicScheduleItem>();
            //var Schedule = (from c in _context.ClinicSchedules.Where(c=>c.)

            if (ClinicDate >= DateOnly.FromDateTime(DateTime.Today))
            {
                var clinicData = _context.ClinicSchedules.Where(c => c.ScheWeek == ClinicDate.DayOfWeek.ToString("d") &&
                                                                     c.ScheOpenFlag == EnumClass.OpenFlag.On.EnumToCode());

                var Data = clinicData.Select(c => new { c.ScheWeek, c.ScheDptCode, c.ScheNoon, c.ScheDoctor, c.ScheRoom }).Distinct().OrderBy(c => c.ScheRoom).ToList();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                foreach (var clinic in Data)
                {
                    //KMUContext _inContext = new KMUContext();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                    var dept = _context.KmuDepartments.Where(c => c.DptCode == clinic.ScheDptCode).FirstOrDefault();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                    if (dept.DptCategory != "EMG")
                    {
                        if (!string.IsNullOrEmpty(DoctorCode) && clinic.ScheDoctor != DoctorCode)
                        {
                            continue;
                        }
                    }
                    if (SoucreType.Contains(dept.DptCategory))
                    {
                        using (CommonService cService = new CommonService(_context))
                        {
                            ClinicScheduleItem input = new ClinicScheduleItem()
                            {
                                CLINIC_TYPE = dept.DptCategory,
                                SCHE_WEEK = clinic.ScheWeek,
                                SCHE_DATE = ClinicDate,
                                SCHE_DPT = clinic.ScheDptCode,
                                SCHE_DPT_NAME = dept.DptName,
                                SCHE_NOON = clinic.ScheNoon,
                                SCHE_DOCTOR_CODE = clinic.ScheDoctor,
                                SCHE_DOCTOR_NAME = cService.GetEmpName(clinic.ScheDoctor),
                                SCHE_ROOM = clinic.ScheRoom
                            };
                            scheduleList.Add(input);
                        }
                    }
                    //_inContext.Dispose();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                }
            }
            else
            {
                /*依據registration去抓*/

                var regData = _context.Registrations.Where(c => c.RegDate == ClinicDate);

                var Data = regData.Select(c => new { c.RegDate, c.RegDepartment, c.RegNoon, c.RegDoctorId, c.RegRoomNo }).Distinct().OrderBy(c => c.RegRoomNo).ToList();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                foreach (var reg in Data)
                {
                    //KMUContext _inContext = new KMUContext();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                    var dept = _context.KmuDepartments.Where(c => c.DptCode == reg.RegDepartment).FirstOrDefault();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串

                    if (dept.DptCategory != "EMG")
                    {
                        if (!string.IsNullOrEmpty(DoctorCode) && reg.RegDoctorId != DoctorCode)
                        {
                            continue;
                        }
                    }

                    if (SoucreType.Contains(dept.DptCategory))
                    {
                        using (CommonService cService = new CommonService(_context))
                        {
                            ClinicScheduleItem input = new ClinicScheduleItem()
                            {
                                CLINIC_TYPE = dept.DptCategory,
                                SCHE_WEEK = reg.RegDate.DayOfWeek.ToString("d"),
                                SCHE_DATE = reg.RegDate,
                                SCHE_DPT = reg.RegDepartment,
                                SCHE_DPT_NAME = dept.DptName,
                                SCHE_NOON = reg.RegNoon,
                                SCHE_DOCTOR_CODE = reg.RegDoctorId,
                                SCHE_DOCTOR_NAME = cService.GetEmpName(reg.RegDoctorId),
                                SCHE_ROOM = reg.RegRoomNo
                            };

                            scheduleList.Add(input);
                        }
                    }
                    //_inContext.Dispose();//2023.04.19 毓凱 協助修改 茲因 KMUContext 移除 OnConfiguring 統一走加密連線字串
                }
            }

            return scheduleList;
        }

        #endregion


        #region Private Function

        private EnumClass.ReplyNoCode CreateClinicSchedule(
            string week,
            string Shift,
            string noon,
            string room,
            string dpt,
            string dptName,
            string doctor,
            string doctorName,
            string openFlag,
            string remark,
            string modifyID)
        {
            #region Variable Setting

            EnumClass.ReplyNoCode replyCode = ReplyNoCode.ReplyOK;

            #endregion

            try
            {
                ClinicSchedule clinic = new ClinicSchedule();

                clinic.ScheWeek = week;
                clinic.shift = Shift;
                clinic.ScheNoon = noon;
                clinic.ScheRoom = room;
                clinic.ScheDptCode = dpt;
                clinic.ScheDptName = dptName;
                clinic.ScheDoctor = doctor;
                clinic.ScheDoctorName = doctorName;
                clinic.ScheOpenFlag = openFlag;
                clinic.ScheRemark = remark;

                clinic.ModifyUser = modifyID;
                clinic.ModifyTime = DateTime.Now;

                _context.ClinicSchedules.Add(clinic);
                _context.SaveChanges();

                replyCode = ReplyNoCode.ReplyOK;
            }
            catch (Exception ex)
            {
                replyCode = EnumClass.ReplyNoCode.SystemError;
            }

            return replyCode;
        }

        private static bool IsPrisonClinic(string dptName, string parentName)
        {
            var text = (dptName ?? "") + " " + (parentName ?? "");
            return text.Contains("prison", StringComparison.OrdinalIgnoreCase)
                || text.Contains("inmate", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
