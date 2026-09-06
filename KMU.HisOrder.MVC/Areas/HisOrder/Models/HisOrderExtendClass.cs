using AutoMapper;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Models
{


    public partial class PatientDTO
    {
        /// <summary>
        /// 病歷流水號
        /// </summary>
        public string Inhospid { get; set; }
        public short RegSeqNo { get; set; }

        public DateTime? RegDate { get; set; }

        public string bed { get; set; }
        public string RegDept { get; set; } 

        public string RegNoon { get; set; }
        /// <summary>
        /// 病歷號
        /// </summary>
        public string RegPatientId { get; set; } = null!;

        public string? RegStatus { get; set; }
        /// <summary>
        /// 身分證
        /// </summary>
        public string NationalId { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string MobilePhone { get; set; }

        public DateTime? BirthDate { get; set; }

        public int Age { get; set; }

        public string Address { get; set; }

        public string transfer_code { get; set; }

        public string transfer_des { get; set; }

        public string remark { get; set; }

        public bool canVisit { get; set; }
    }





    public class GlobalVariableDTO
    {
        public PatientDTO Patient { get; set; }

        public ClinicDTO Clinic { get; set; }

        public LoginDTO Login { get; set; }
    }



    //public class PatientDTO
    //{
    //    public string ChartNO { get; set; }

    //    public DateTime Birthday { get; set; }

    //    public string PatientName { get; set; }

    //    public string Sex { get; set; }

    //    public int Age { get; set; }

    //    public string IDNO { get; set; }
    //}


    public class ClinicDTO
    {

        public DateTime RegDate { get; set; }
        public string WardId { get; set; }
        public string NoonNO { get; set; }
        /// <summary>
        /// 科別 : OPD-看診科別, IPD-住院科別
        /// </summary>
        public string DeptCode { get; set; }
        /// <summary>
        /// 科別名稱
        /// </summary>
        public string DeptName { get; set; }
        /// <summary>
        /// 看診醫師代碼
        /// </summary>
        public string DoctorCode { get; set; }
        /// <summary>
        /// 看診醫師名稱
        /// </summary>
        public string DoctorName { get; set; }
        /// <summary>
        /// 住院號
        /// </summary>
        //public string Inhospid { get; set; }
        /// <summary>
        /// 判斷目前是門急住的哪一個類別(OPD/IPD/ER)
        /// </summary>
        public string InhospType { get; set; }

        ///// <summary>
        ///// 目前病患的看診狀態
        ///// </summary>
        //public EnumClassPatientVisitStatus PatientVisitStatus { get; set; }

        ///// <summary>
        ///// 其他身分
        ///// </summary>
        //public string ATTRIBUTE { get; set; }

        /// <summary>
        /// 診間房號
        /// </summary>
        public string RoomNumber { get; set; }

    }


    public class LoginDTO
    {
        public string EMPCODE { get; set; }
        public string EMPNAME { get; set; }

        //授權看診的VS
        public string AuthDoctorCode { get; set; }
        public string AuthDoctorName { get; set; }
    }


    public class ResultDTO
    {
        /// <summary>
        /// 狀態(ex:00,66等...)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 訊息(ex:Exception...)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 此作業是否完成(true/false)
        /// </summary>
        public bool isSuccess { get; set; }

        /// <summary>
        /// 回傳值(ex:HealthPlanID...)
        /// </summary>
        public string returnValue { get; set; }

        /// <summary>
        /// 該診次中目前最新的檢核結果
        /// </summary>
        public string VerifyResult { get; set; }



    }


    public class ExtendClinicSchedule : ClinicSchedule
    {
        public bool defaultFocus = false;
    }

    public class ExtendHisorderplan : Hisorderplan
    {
        public string? ModifyType { get; set; }
    }

    public class ExtendHisrdersoa : Hisordersoa
    {
        public string pure_context { get; set; }
    }

    public class ExtendKmuNonMedicine : KmuNonMedicine
    {
        public string? RefName { get; set; }
        public int? RefShowseq { get; set; }
        public string? PlanDes { get; set; }
    }

    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Hisorderplan, ExtendHisorderplan>();
        }
    }


    public class hisordersoa_version
    {
        public int VersionCode { get; set; }
        public DateTime CreateDate { get; set; }
        public string Des { get; set; }

    }



    public class HistoryRecordDto
    {
        public string inhospid { get; set; }
        public string clinicCode { get; set; }
        public string clinicName { get; set; }
        public string wardid { get; set; }

        public DateTime regDate { get; set; }
        public string sourceType { get; set; }
        public string doctorCode { get; set; }
        public string doctorName { get; set; }

        public string regFollowCode { get; set; }
    }


    public class HistoryRecordDetail
    {
        public string inhospid { get; set; }
        public List<ExtendHisrdersoa> soapContext { get; set; }
        public List<Hisorderplan> DiagnosisContext { get; set; }
        public List<Hisorderplan> MedicineContext { get; set; }
        public List<Hisorderplan> NonMedContext { get; set; }
        public List<KmuCoderef> MG_INFO { get; set; }
        public string RegFollowCode { get; set; }
    }





}
