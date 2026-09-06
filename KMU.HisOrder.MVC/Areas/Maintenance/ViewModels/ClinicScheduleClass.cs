using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Models;
using System.Runtime.Serialization;

namespace KMU.HisOrder.MVC.Areas.Maintenance.ViewModels
{
    public class ClinicScheduleClass
    {
    }

    public partial class ClinicScheduleItem
    {
        private string? _CLINIC_TYPE;
        /// <summary>
        /// 
        /// </summary>
        public string? CLINIC_TYPE
        {
            get { return _CLINIC_TYPE; }
            set { _CLINIC_TYPE = value; }
        }

        private string? _SCHE_WEEK;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_WEEK
        {
            get { return _SCHE_WEEK; }
            set { _SCHE_WEEK = value; }
        }

        private DateOnly? _SCHE_DATE;
        /// <summary>
        /// 
        /// </summary>
        public DateOnly? SCHE_DATE
        {
            get { return _SCHE_DATE; }
            set { _SCHE_DATE = value; }
        }

        private string? _SCHE_ROOM;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_ROOM
        {
            get { return _SCHE_ROOM; }
            set { _SCHE_ROOM = value; }
        }

        private string? _SCHE_NOON;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_NOON
        {
            get { return _SCHE_NOON; }
            set { _SCHE_NOON = value; }
        }

        private string? _SCHE_DPT;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DPT
        {
            get { return _SCHE_DPT; }
            set { _SCHE_DPT = value; }
        }

        private string? _SCHE_DPT_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DPT_NAME
        {
            get { return _SCHE_DPT_NAME; }
            set { _SCHE_DPT_NAME = value; }
        }

        private string? _SCHE_DOCTOR_CODE;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DOCTOR_CODE
        {
            get { return _SCHE_DOCTOR_CODE; }
            set { _SCHE_DOCTOR_CODE = value; }
        }

        private string? _SCHE_DOCTOR_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DOCTOR_NAME
        {
            get { return _SCHE_DOCTOR_NAME; }
            set { _SCHE_DOCTOR_NAME = value; }
        }

        private EnumClass.OpenFlag? _enumOpenFlag;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.OpenFlag? enumOpenFlag
        {
            get { return _enumOpenFlag; }
            set { _enumOpenFlag = value; }
        }
    }

    public partial class OpenFlagReturn
    {
        private string? _NewFlag;
        /// <summary>
        /// 
        /// </summary>
        public string? NewFlag
        {
            get { return _NewFlag; }
            set { _NewFlag = value; }
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

    public partial class ScheduleListReturn
    {
        private List<ScheduleData>? _sdList;
        /// <summary>
        /// 
        /// </summary>
        public List<ScheduleData>? sdList
        {
            get { return _sdList; }
            set { _sdList = value; }
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

    public partial class ScheduleModal
    {
        private ScheduleData? _scheduleData;
        /// <summary>
        /// 
        /// </summary>
        public ScheduleData? scheduleData
        {
            get { return _scheduleData; }
            set { _scheduleData = value; }
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

    public partial class ScheduleData
    {
        private string? _SCHE_WEEK;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_WEEK
        {
            get { return _SCHE_WEEK; }
            set { _SCHE_WEEK = value; }
        }

        private string? _SCHE_Shift;
        public string? SCHE_Shift
        {
            get { return _SCHE_Shift; }
            set { _SCHE_Shift = value; }
        }

        private string? _SCHE_NOON;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_NOON
        {
            get { return _SCHE_NOON; }
            set { _SCHE_NOON = value; }
        }

        private string? _SCHE_ROOM;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_ROOM
        {
            get { return _SCHE_ROOM; }
            set { _SCHE_ROOM = value; }
        }

        private string? _SCHE_DPT;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DPT
        {
            get { return _SCHE_DPT; }
            set { _SCHE_DPT = value; }
        }

        private string? _SCHE_DPT_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DPT_NAME
        {
            get { return _SCHE_DPT_NAME; }
            set { _SCHE_DPT_NAME = value; }
        }

        private string? _SCHE_PARENT_DPT;
        /// <summary>
        /// 
        /// </summary>
        public string?  SCHE_PARENT_DPT
        {
            get { return _SCHE_PARENT_DPT; }
            set { _SCHE_PARENT_DPT = value; }
        }

        private string? _SCHE_PARENT_DPT_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_PARENT_DPT_NAME
        {
            get { return _SCHE_PARENT_DPT_NAME; }
            set { _SCHE_PARENT_DPT_NAME = value; }
        }

        private string? _DPT_CATEGORY;
        /// <summary>
        /// 
        /// </summary>
        public string? DPT_CATEGORY
        {
            get { return _DPT_CATEGORY; }
            set { _DPT_CATEGORY = value; }
        }

        private string? _SCHE_DOCTOR_CODE;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DOCTOR_CODE
        {
            get { return _SCHE_DOCTOR_CODE; }
            set { _SCHE_DOCTOR_CODE = value; }
        }

        private string? _SCHE_DOCTOR_NAME;
        /// <summary>
        /// 
        /// </summary>
        public string? SCHE_DOCTOR_NAME
        {
            get { return _SCHE_DOCTOR_NAME; }
            set { _SCHE_DOCTOR_NAME = value; }
        }

        private string? _DEFAULT_ATTR;
        /// <summary>
        /// 
        /// </summary>
        public string? DEFAULT_ATTR
        {
            get { return _DEFAULT_ATTR; }
            set { _DEFAULT_ATTR = value; }
        }

        private string? _REMARK;
        /// <summary>
        /// 
        /// </summary>
        public string? REMARK
        {
            get { return _REMARK; }
            set { _REMARK = value; }
        }

        private EnumClass.OpenFlag? _enumOpenFlag;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.OpenFlag? enumOpenFlag
        {
            get { return _enumOpenFlag; }
            set { _enumOpenFlag = value; }
        }
    }



    #region Common

    //public partial class LoginDTO
    //{
    //    private string? _EMPCODE;
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public string? EMPCODE
    //    {
    //        get { return _EMPCODE; }
    //        set { _EMPCODE = value; }
    //    }
    //}

    #endregion
}
