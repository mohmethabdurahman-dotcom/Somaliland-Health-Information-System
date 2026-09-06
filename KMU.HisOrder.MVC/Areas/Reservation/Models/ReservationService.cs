using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;
using NuGet.RuntimeModel;

namespace KMU.HisOrder.MVC.Areas.Reservation.Models
{
    public class ReservationService : IDisposable
    {
        private readonly KMUContext _context;


        public ReservationService(KMUContext context)
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
        /// 
        /// </summary>
        /// <param name="patientID"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ReserveHistoryList getReserveHistory(
            string? patientID,
            EnumClass.DisplayLanguage language)
        {
            #region Variable Setting

            ReserveHistoryList result = new ReserveHistoryList();
            List<ReserveHistoryClass> rhList = new List<ReserveHistoryClass>();
            List<Registration> regList = new List<Registration>();

            List<KmuCoderef> statusList = new List<KmuCoderef>();
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;
            #endregion

            #region Step 1. Check Input Variables

            if (string.IsNullOrEmpty(patientID))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion
            using (CommonService cService = new CommonService(_context))
            {
                try
                {
                    statusList = _context.KmuCoderefs.Where(c => c.RefCodetype == "Reserve_StatusCode").ToList();

                    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                    regList = _context.Registrations.Where(c => c.RegDate <= today &&
                                                                c.RegHealthId == patientID).ToList();

                    foreach (var reg in regList)
                    {
                        ReserveHistoryClass rh = new ReserveHistoryClass()
                        {
                            REG_DATE = reg.RegDate,
                            WEEK_ID = reg.RegDate.DayOfWeek.ToString("d"),
                            REG_DEPARTMENT = reg.RegDepartment,
                            REG_DEPT_NAME = cService.GetDepartmentName(reg.RegDepartment),
                            REG_NOON = reg.RegNoon,
                            REG_DOCTOR_ID = reg.RegDoctorId,
                            DOCTOR_NAME = cService.GetEmpName(reg.RegDoctorId),
                            PatientID = reg.RegHealthId,
                            InHospID = reg.Inhospid,
                            STATUSCODE = reg.RegStatus,
                            STATUSDESC = statusList.Where(c => c.RefCode == reg.RegStatus).FirstOrDefault().RefName,
                        };


                        rhList.Add(rh);
                    }

                    result.rhList = rhList;
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                }
                catch (Exception ex)
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                    result.rhList = new List<ReserveHistoryClass>();

                    return result;
                }
            }
            return result;
        }

        /// <summary>
        /// Make an appointment
        /// </summary>
        /// <param name="reserveInfo"></param>
        /// <param name="language"></param>
        /// <param name="modifyID"></param>
        /// <returns></returns>
        public AppointmentReturnCollect MakeAppointment(
            string reserveType,
            AppointmentStructure reserveInfo,
            EnumClass.DisplayLanguage language,
            string modifyID)
        {
            #region local variables

            AppointmentReturnCollect result = new AppointmentReturnCollect();
            EnumClass.DisplayLanguage lang = EnumClass.DisplayLanguage.English;   // 預設使用英文
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;

            short seqNo = 0;
            ClinicSchedule clinicInfo = null;
            KmuChart patientInfo = null;
            Registration reg = new Registration();

            #endregion

            #region Step 1. Check Input Variables

            if (null == reserveInfo ||
                true == String.IsNullOrEmpty(modifyID))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            #region Step 2. Initial Data Check

            if (EnumClass.ReplyNoCode.ReplyOK != (replyCode = DataCheckFunction.CheckAppointmentData(_context, reserveInfo)))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            #endregion

            #region Step3. Read Clinic Schedule

            var clinicDto = _context.ClinicSchedules.Where(c => c.ScheWeek == reserveInfo.reserveDate.Value.DayOfWeek.ToString("d") &&
                                                              c.ScheNoon == reserveInfo.reserveNoon &&
                            
                                                              c.ScheRoom == reserveInfo.reserveRoom && c.shift == reserveInfo.reserveShift);
            if (!clinicDto.Any())
            {
                replyCode = EnumClass.ReplyNoCode.NoScheduleToday;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            clinicInfo = clinicDto.FirstOrDefault();

            if (EnumClass.OpenFlag.Off.EnumToCode() == clinicInfo.ScheOpenFlag)
            {
                replyCode = EnumClass.ReplyNoCode.DayOff;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            #endregion

            #region Step4. Read Medical Record 

            var chartDto = _context.KmuCharts.Where(c => c.ChrHealthId == reserveInfo.patientID);

            if (!chartDto.Any())
            {
                replyCode = EnumClass.ReplyNoCode.ChartNotExist;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            patientInfo = chartDto.FirstOrDefault();

            #endregion

            #region Step5. Change Attritube(現階段不用)

            #endregion

            #region Step6. Check Age(現階段不用)

            #endregion

            #region 門診的話要檢核是否有重複掛號

            if (reserveType == "OPD")
            {
                IEnumerable<Registration> appoints = _context.Registrations.Where(c => c.RegDate == DateOnly.FromDateTime(reserveInfo.reserveDate.Value) &&
                                                                  c.RegDepartment == clinicInfo.ScheDptCode &&
                                                                  c.RegNoon == reserveInfo.reserveNoon && c.shift == reserveInfo.reserveShift &&
                                                                  c.RegHealthId == reserveInfo.patientID &&
                                                                  c.RegStatus != "C");      //取消掛號過可以不計
                if (appoints.Any())
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.RegisterAlready, lang);
                    return result;
                }
            }

            #endregion

            #region Step7. Get SeqNo

            ///To avoid Two process Get SeqNo at the same Time
            for (int reGetSeqNo = 0; reGetSeqNo < 3; reGetSeqNo++)
            {
                seqNo = GenerateSeqNo(reserveInfo.reserveDate.Value, clinicInfo, patientInfo, ref replyCode);

                if (EnumClass.ReplyNoCode.ReplyOK != replyCode)
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                    return result;
                }

                #region Insert into DB

                if (EnumClass.ReplyNoCode.ReplyOK != (replyCode = Complete(reserveInfo, clinicInfo, patientInfo, seqNo, modifyID, ref reg)))
                {
                    if (replyCode == EnumClass.ReplyNoCode.OccupiedSeqNo)
                    {
                        continue;
                    }

                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                    return result;
                }
                else
                {
                    break;
                }

                #endregion
            }


            if (EnumClass.ReplyNoCode.OccupiedSeqNo == replyCode)
            {
                replyCode = EnumClass.ReplyNoCode.FaildRegistration;
                _context.Database.RollbackTransaction();

                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            #endregion

            #region Step8. Commit Work

            try
            {
                if (string.IsNullOrWhiteSpace(reg.Inhospid))
                    reg.Inhospid = GenerateUniqueInhospid();

                _context.SaveChanges();
                _context.Entry(reg).Reload();
            }
            catch (DbUpdateException)
            {
                for (int retry = 0; retry < 5; retry++)
                {
                    try
                    {
                        reg.Inhospid = GenerateUniqueInhospid();
                        _context.SaveChanges();
                        _context.Entry(reg).Reload();
                        replyCode = EnumClass.ReplyNoCode.ReplyOK;
                        break;
                    }
                    catch (DbUpdateException)
                    {
                        if (retry == 4)
                        {
                            result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.FaildRegistration, lang);
                            if (result.ReturnT != null)
                                result.ReturnT.StatusMessage = "Could not complete the appointment. Please try again.";
                            return result;
                        }
                    }
                }
            }

            #endregion 

            #region Step9. Setting Return Msg

            AppointmentDetail appointment = new AppointmentDetail()
            {
                patientID = patientInfo.ChrHealthId,
                reserveDate = reserveInfo.reserveDate,
                reserveRoom = clinicInfo.shift,
                reserveShift = clinicInfo.ScheRoom,
                reserveNoon = clinicInfo.ScheNoon,
                
                seqNo = seqNo,
                reserveDpt = clinicInfo.ScheDptCode,
                reserveDptName = clinicInfo.ScheDptName,
                reserveDoctor = clinicInfo.ScheDoctor,
                reserveDoctorName = clinicInfo.ScheDoctorName,
                inHospID = reg.Inhospid,
                clinicRemark = clinicInfo.ScheRemark
            };

            result.Appointment = appointment;
            result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);

            #endregion

            return result;
        }


        public CancelAppointReturnCollect CancelAppointment(string InhospID,
            EnumClass.DisplayLanguage language,
            string modifyID)
        {
            #region local variables
            CancelAppointReturnCollect result = new CancelAppointReturnCollect();
            EnumClass.DisplayLanguage lang = EnumClass.DisplayLanguage.English;   // 預設使用英文
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;

            Registration reg = new Registration();
            CommonService cService = new CommonService(_context);
            #endregion

            #region Step 1. Check Input Variables

            if (true == String.IsNullOrEmpty(InhospID) ||
                true == String.IsNullOrEmpty(modifyID))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            #region Step 2. Read Reservation Data

            var regDtos = _context.Registrations.Where(c => c.Inhospid == InhospID);

            if (!regDtos.Any())
            {
                replyCode = EnumClass.ReplyNoCode.NoReserveData;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            reg = regDtos.FirstOrDefault();

            if (reg.RegStatus == "*")
            {
                replyCode = EnumClass.ReplyNoCode.DoneAlready;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            if (reg.RegStatus == "C")
            {
                replyCode = EnumClass.ReplyNoCode.CancelAlready;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            if (reg.RegStatus != "N")
            {
                replyCode = EnumClass.ReplyNoCode.CanNotCancel;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            #endregion

            #region Step 3.Cancel Reserve Data

            if (EnumClass.ReplyNoCode.ReplyOK != (replyCode = CancelReservation(ref reg, modifyID)))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);
                return result;
            }

            #endregion


            #region Step4. Setting Return Msg

            AppointmentDetail appointment = new AppointmentDetail()
            {
                patientID = reg.RegHealthId,
                reserveDate = reg.RegDate.ToDateTime(TimeOnly.MinValue),
                reserveRoom = reg.RegRoomNo,
                reserveNoon = reg.RegNoon,
                seqNo = reg.RegSeqNo,
                reserveDpt = reg.RegDepartment,
                reserveDptName = cService.GetDepartmentName(reg.RegDepartment),
                reserveDoctor = reg.RegDoctorId,
                reserveDoctorName = cService.GetEmpName(reg.RegDoctorId)
            };

            result.Appointment = appointment;
            result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, lang);

            #endregion

            #region Step9. Commit Work

            _context.SaveChanges();
            _context.Entry(reg).Reload();

            #endregion

            return result;
        }

        #endregion

        #region Private Function

        /// <summary>
        /// 取號
        /// </summary>
        /// <param name="clinicInfo"></param>
        /// <param name="patientInfo"></param>
        /// <param name="replyCode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private short GenerateSeqNo(DateTime reserveDate, ClinicSchedule clinicInfo, KmuChart patientInfo, ref EnumClass.ReplyNoCode replyCode)
        {
            #region local variables

            short seqNo = 0;

            short startSeqNo = 0;

            List<short> availableNoList = new List<short>();

            List<Registration> _regList = new List<Registration>();

            DateOnly rDate = DateOnly.FromDateTime(reserveDate);

            #endregion

            try
            {
                #region Step1. Check variable correct

                if (null == clinicInfo || null == patientInfo)
                {
                    return seqNo;
                }

                #endregion

                #region Step2. Get Available SeqNo List

                _regList = _context.Registrations.Where(c => c.RegDate == rDate &&
                                                           c.RegDepartment == clinicInfo.ScheDptCode &&
                                                           c.RegNoon == clinicInfo.ScheNoon).ToList();

                if (null != _regList && 0 < _regList.Count())
                {
                    startSeqNo = _regList.Max(c => c.RegSeqNo);
                }

                startSeqNo = 1;


                for (; startSeqNo <= short.MaxValue; startSeqNo++)
                {
                    if (null != _regList && 0 < _regList.Count())
                    {
                        // double check號碼是否存在
                        if (0 == _regList.Where(c => c.RegSeqNo == startSeqNo).Count())
                        {
                            // 號碼不存在 可以放進去list做比較
                            availableNoList.Add(startSeqNo);
                            break;
                        }
                    }
                    else
                    {
                        // 號碼不存在 可以放進去list做比較
                        availableNoList.Add(startSeqNo);
                        break;
                    }
                }

                #endregion

                #region Step3. Get the min SeqNo from List

                if (0 < availableNoList.Count())    //可取號個數大於 0 , 由可取號 list 取號
                {
                    seqNo = availableNoList.Min();

                }

                #endregion

                replyCode = EnumClass.ReplyNoCode.ReplyOK;
            }
            catch (Exception ex)
            {
                seqNo = 0;
                replyCode = EnumClass.ReplyNoCode.SystemError;
            }

            return seqNo;
        }

        /// <summary>
        /// 寫入DB動作
        /// </summary>
        /// <param name="reserveInfo"></param>
        /// <param name="clinicInfo"></param>
        /// <param name="patientInfo"></param>
        /// <param name="seqNo"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private EnumClass.ReplyNoCode Complete(AppointmentStructure reserveInfo, ClinicSchedule clinicInfo, KmuChart patientInfo, short seqNo, string ModifyID, ref Registration reg)
        {
            EnumClass.ReplyNoCode result = EnumClass.ReplyNoCode.ReplyOK;

            #region variable check

            if (null == reserveInfo || null == clinicInfo || null == patientInfo || 0 >= seqNo)
            {
                result = EnumClass.ReplyNoCode.WrongParam;
                return result;
            }

            IEnumerable<Registration> appoints = _context.Registrations.Where(c => c.RegDate == DateOnly.FromDateTime(reserveInfo.reserveDate.Value) &&
                                                     c.RegDepartment == clinicInfo.ScheDptCode &&
                                                     c.RegNoon == reserveInfo.reserveNoon && c.shift == reserveInfo.reserveShift &&
                                                     c.RegHealthId == reserveInfo.patientID &&
                                                     c.RegStatus != "*" && c.RegStatus != "T" && c.RegStatus != "C");
            if (appoints.Any())
            {
                result = EnumClass.ReplyNoCode.RegisterAlreadyWait;
                return result;
            }
            #endregion

            try
            {
                var shift = "";

                var ShiftA = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift A").RefDes;
                var ShiftB = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift B").RefDes;
                var ShiftC = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift C").RefDes;

                //var shift = "";
                //string ShiftA = "07:30:00";
                //string ShiftB = "13:30:00";
                //string ShiftC = "19:30:00";

                TimeSpan duration = DateTime.Now.TimeOfDay;


                if (duration > TimeSpan.Parse(ShiftA) && duration < TimeSpan.Parse(ShiftB))
                {
                    shift = "Shif A";
                }
                if (duration > TimeSpan.Parse(ShiftB) && duration < TimeSpan.Parse(ShiftC))
                {
                    shift = "Shift B";
                }
                if (duration > TimeSpan.Parse(ShiftC) && duration < TimeSpan.Parse(ShiftA))
                {
                    shift = "Shift C";
                }

                #region Insert registration

                reg = new Registration()
                {
                    RegDate = DateOnly.FromDateTime(reserveInfo.reserveDate.Value),
                    RegDepartment = clinicInfo.ScheDptCode,
                    RegNoon = clinicInfo.ScheNoon,
                    shift = clinicInfo.shift,
                    RegSeqNo = seqNo,
                    RegHealthId = patientInfo.ChrHealthId,
                    Inhospid = GenerateUniqueInhospid(),
                    RegTriage = reserveInfo.reserveTriage,
                    score = reserveInfo.reserveTriageScore,
                    RegBedNo = null,
                    RegAttribute = reserveInfo.reserveAttr,
                    RegAttrDesc = reserveInfo.reserveAttrDesc,
                    RegDoctorId = clinicInfo.ScheDoctor,
                    RegRoomNo = clinicInfo.ScheRoom,
                    RegStatus = EnumClass.EnumStatus.None.EnumToCode(),
                    ModifyUser = ModifyID,
                    ModifyTime = DateTime.Now,
                    RegCreateTime = DateTime.Now    //2023.04.28 add by elain.
                };
                
                _context.Registrations.Add(reg);

                #endregion
            }
            catch (Exception ex)
            {
                result = EnumClass.ReplyNoCode.OccupiedSeqNo;
            }

            return result;
        }

        private string GenerateUniqueInhospid()
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var candidate = DateTime.Now.ToString("yyMMddHHmmssfff") + Random.Shared.Next(10, 99).ToString();
                if (candidate.Length > 20)
                    candidate = candidate.Substring(0, 20);

                if (!_context.Registrations.AsNoTracking().Any(r => r.Inhospid == candidate))
                    return candidate;
            }

            return Guid.NewGuid().ToString("N").Substring(0, 17);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancelData"></param>
        /// <param name="ModifyID"></param>
        /// <returns></returns>
        private EnumClass.ReplyNoCode CancelReservation(ref Registration cancelData, string ModifyID)
        {
            EnumClass.ReplyNoCode result = EnumClass.ReplyNoCode.ReplyOK;

            #region variable check

            if (null == cancelData)
            {
                result = EnumClass.ReplyNoCode.WrongParam;
                return result;
            }

            #endregion

            try
            {
                #region Update registration Status to C

                cancelData.RegStatus = "C";

                _context.SaveChanges();

                #endregion
            }
            catch (Exception ex)
            {
                result = EnumClass.ReplyNoCode.SystemError;
            }

            return result;
        }

        /// <summary>
        /// 生理資訊寫入DB
        /// </summary>
        /// <param name="physicalInfoList"></param>
        /// <param name="appointment"></param>
        /// <param name="modifyID"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public EnumClass.ReplyNoCode CompletePhysical(List<PhysicalStructure> physicalInfoList, AppointmentDetail appointment, string modifyID)
        {
            EnumClass.ReplyNoCode result = EnumClass.ReplyNoCode.ReplyOK;

            #region variable check

            if (null == physicalInfoList || null == appointment)
            {
                result = EnumClass.ReplyNoCode.WrongParam;
                return result;
            }

            #endregion

            try
            {
                List<Registration> regList = new List<Registration>();
                string InhospID = "";
                DateTime nowTime = DateTime.Now;

                regList = _context.Registrations.Where(c => c.RegDate == DateOnly.FromDateTime(appointment.reserveDate.Value) &&
                                                        c.RegDepartment == appointment.reserveDpt &&
                                                        c.RegNoon == appointment.reserveNoon &&
                                                        c.RegSeqNo == appointment.seqNo &&
                                                        c.RegHealthId == appointment.patientID).ToList();

                if (regList.Any())
                {
                    InhospID = regList.FirstOrDefault().Inhospid;
                }

                #region Insert registration

                foreach (var sign in physicalInfoList)
                {
                    PhysicalSign physical = new PhysicalSign()
                    {
                        PhyId = "",
                        Inhospid = InhospID,
                        PhyType = sign.physicalType,
                        PhyValue = sign.physicalValue,
                        ModifyUser = modifyID,
                        ModifyTime = nowTime
                    };

                    _context.PhysicalSigns.AddAsync(physical);
                }

                #endregion

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                result = EnumClass.ReplyNoCode.SystemError;
            }

            return result;
        }
        #endregion
    }
}
