using KMU.HisOrder.MVC.Models;
using System.Runtime.Serialization;

namespace KMU.HisOrder.MVC.Areas.Reservation.ViewModels
{
    public class ReservationClass
    {
    }

    public partial class ReserveHistoryList
    {
        private List<ReserveHistoryClass>? _rhList;
        /// <summary>
        /// 
        /// </summary>
        public List<ReserveHistoryClass>? rhList
        {
            get { return _rhList; }
            set { _rhList = value; }
        }

        private ReturnMsg? _ReturnT;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public ReturnMsg? ReturnT
        {
            get { return _ReturnT; }
            set { _ReturnT = value; }
        }
    }

    public partial class ReserveHistoryClass
    {
        private DateOnly? _REG_DATE;
        /// <summary>
        /// 看診日
        /// </summary>
        public DateOnly? REG_DATE
        {
            get { return _REG_DATE; }
            set { _REG_DATE = value; }
        }

        private string? _WEEK_ID;
        /// <summary>
        /// 星期別
        /// </summary>
        public string? WEEK_ID
        {
            get { return _WEEK_ID; }
            set { _WEEK_ID = value; }
        }

        private string? _REG_DEPARTMENT;
        /// <summary>
        /// 看診科別
        /// </summary>
        public string? REG_DEPARTMENT
        {
            get { return _REG_DEPARTMENT; }
            set { _REG_DEPARTMENT = value; }
        }

        private string? _REG_DEPT_NAME;
        /// <summary>
        /// 看診科別
        /// </summary>
        public string? REG_DEPT_NAME
        {
            get { return _REG_DEPT_NAME; }
            set { _REG_DEPT_NAME = value; }
        }

        private string? _REG_NOON;
        /// <summary>
        /// 看診午別
        /// </summary>
        public string? REG_NOON
        {
            get { return _REG_NOON; }
            set { _REG_NOON = value; }
        }

        private string? _PatientID;
        /// <summary>
        /// 病歷號
        /// </summary>
        public string? PatientID
        {
            get { return _PatientID; }
            set { _PatientID = value; }
        }

        private string? _InHospID;
        /// <summary>
        /// 就醫序號
        /// </summary>
        public string? InHospID
        {
            get { return _InHospID; }
            set { _InHospID = value; }
        }

        private string? _REG_DOCTOR_ID;
        /// <summary>
        /// 
        /// </summary>
        public string? REG_DOCTOR_ID
        {
            get { return _REG_DOCTOR_ID; }
            set { _REG_DOCTOR_ID = value; }
        }

        private string? _DOCTOR_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? DOCTOR_NAME
        {
            get { return _DOCTOR_NAME; }
            set { _DOCTOR_NAME = value; }
        }

        private string? _STATUSCODE;
        /// <summary>
        /// 
        /// </summary>
        public string? STATUSCODE
        {
            get { return _STATUSCODE; }
            set { _STATUSCODE = value; }
        }

        private string? _STATUSDESC;
        /// <summary>
        /// 
        /// </summary>
        public string? STATUSDESC
        {
            get { return _STATUSDESC; }
            set { _STATUSDESC = value; }
        }
    }

    public partial class AppointmentStructure
    {
        private DateTime? _reserveDate;
        /// <summary>
        /// 看診日
        /// </summary>
        public DateTime? reserveDate
        {
            get { return _reserveDate; }
            set { _reserveDate = value; }
        }

        private string? _reserveRoom;
        /// <summary>
        /// 房間號
        /// </summary>
        public string? reserveRoom
        {
            get { return _reserveRoom; }
            set { _reserveRoom = value; }
        }

        private string? _reserveNoon;
        /// <summary>
        /// 午別
        /// </summary>
        public string? reserveNoon
        {
            get { return _reserveNoon; }
            set { _reserveNoon = value; }
        }

        private string? _reserveShift;
        /// <summary>
        /// 午別
        /// </summary>
        public string? reserveShift
        {
            get { return _reserveShift; }
            set { _reserveShift = value; }
        }

        private string? _patientID;
        /// <summary>
        /// 病歷號
        /// </summary>
        public string? patientID
        {
            get { return _patientID; }
            set { _patientID = value; }
        }

        private string? _reserveTriage;
        /// <summary>
        /// 檢傷
        /// </summary>
        public string? reserveTriage
        {
            get { return _reserveTriage; }
            set { _reserveTriage = value; }
        }


        private string? _reserveTriageScore;
        /// <summary>
        /// 檢傷
        /// </summary>
        public string? reserveTriageScore
        {
            get { return _reserveTriageScore; }
            set { _reserveTriageScore = value; }
        }

        private string? _reserveAttr;
        /// <summary>
        /// 身分
        /// </summary>
        public string? reserveAttr
        {
            get { return _reserveAttr; }
            set { _reserveAttr = value; }
        }

        private string? _reserveAttrDesc;
        /// <summary>
        /// 身分說明
        /// </summary>
        public string? reserveAttrDesc
        {
            get { return _reserveAttrDesc; }
            set { _reserveAttrDesc = value; }
        }
    }

    public partial class PhysicalStructure
    {
        private string? _physicalType;
        /// <summary>
        /// 類型
        /// </summary>
        public string? physicalType
        {
            get { return _physicalType; }
            set { _physicalType = value; }
        }

        private string? _physicalValue;
        /// <summary>
        /// 值
        /// </summary>
        public string? physicalValue
        {
            get { return _physicalValue; }
            set { _physicalValue = value; }
        }
    }

    public partial class AppointmentReturnCollect
    {

        private AppointmentDetail _Appointment;
        /// <summary>
        /// 
        /// </summary>
        public AppointmentDetail Appointment
        {
            get { return _Appointment; }
            set { _Appointment = value; }
        }

        private ReturnMsg? _ReturnT;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public ReturnMsg? ReturnT
        {
            get { return _ReturnT; }
            set { _ReturnT = value; }
        }
    }

    public partial class CancelAppointReturnCollect
    {

        private AppointmentDetail _Appointment;
        /// <summary>
        /// 
        /// </summary>
        public AppointmentDetail Appointment
        {
            get { return _Appointment; }
            set { _Appointment = value; }
        }

        private ReturnMsg? _ReturnT;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public ReturnMsg? ReturnT
        {
            get { return _ReturnT; }
            set { _ReturnT = value; }
        }
    }


    public partial class AppointmentDetail
    {
        private DateTime? _reserveDate;
        /// <summary>
        /// 看診日
        /// </summary>
        public DateTime? reserveDate
        {
            get { return _reserveDate; }
            set { _reserveDate = value; }
        }

        private string? _reserveRoom;
        /// <summary>
        /// 房間號
        /// </summary>
        public string? reserveRoom
        {
            get { return _reserveRoom; }
            set { _reserveRoom = value; }
        }

        private string? _reserveShift;
        /// <summary>
        /// 房間號
        /// </summary>
        public string? reserveShift
        {
            get { return _reserveShift; }
            set { _reserveShift = value; }
        }

        private string? _reserveDpt;
        /// <summary>
        /// 科別
        /// </summary>
        public string? reserveDpt
        {
            get { return _reserveDpt; }
            set { _reserveDpt = value; }
        }

        private string? _reserveDptName;
        /// <summary>
        /// 科別名稱
        /// </summary>
        public string? reserveDptName
        {
            get { return _reserveDptName; }
            set { _reserveDptName = value; }
        }

        private string? _reserveNoon;
        /// <summary>
        /// 午別
        /// </summary>
        public string? reserveNoon
        {
            get { return _reserveNoon; }
            set { _reserveNoon = value; }
        }

        private string? _inHospID;
        /// <summary>
        /// InHospID
        /// </summary>
        public string? inHospID
        {
            get { return _inHospID; }
            set { _inHospID = value; }
        }

        private string? _patientID;
        /// <summary>
        /// 病歷號
        /// </summary>
        public string? patientID
        {
            get { return _patientID; }
            set { _patientID = value; }
        }

        private short? _seqNo;
        /// <summary>
        /// 看診序號
        /// </summary>
        public short? seqNo
        {
            get { return _seqNo; }
            set { _seqNo = value; }
        }

        private string? _reserveDoctor;
        /// <summary>
        /// 看診醫師
        /// </summary>
        public string? reserveDoctor
        {
            get { return _reserveDoctor; }
            set { _reserveDoctor = value; }
        }

        private string? _reserveDoctorName;
        /// <summary>
        /// 看診醫師名稱
        /// </summary>
        public string? reserveDoctorName
        {
            get { return _reserveDoctorName; }
            set { _reserveDoctorName = value; }
        }

        private string? _triageLevel;
        /// <summary>
        /// 檢傷等級
        /// </summary>
        public string? triageLevel
        {
            get { return _triageLevel; }
            set { _triageLevel = value; }
        }

        private string? _clinicRemark;
        /// <summary>
        /// 診間備註
        /// </summary>
        public string? clinicRemark
        {
            get { return _clinicRemark; }
            set { _clinicRemark = value; }
        }
    }
}
