using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using System.Runtime.Serialization;
using System.Security.AccessControl;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels
{
    public class MedicalRecordClass
    {
    }

    #region MRFind

    public class PatientListReturn
    {
        private List<PatientDataClass>? _ptList;
        /// <summary>
        /// 
        /// </summary>
        public List<PatientDataClass>? ptList
        {
            get { return _ptList; }
            set { _ptList = value; }
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

    public partial class PatientDataClass
    {
        private string? _PATIENTID;
        /// <summary>
        /// 
        /// </summary>
        public string? PATIENTID
        {
            get { return _PATIENTID; }
            set { _PATIENTID = value; }
        }

        private string? _FIRSTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? FIRSTNAME
        {
            get { return _FIRSTNAME; }
            set { _FIRSTNAME = value; }
        }

        private string? _MIDNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? MIDNAME
        {
            get { return _MIDNAME; }
            set { _MIDNAME = value; }
        }

        private string? _LASTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? LASTNAME
        {
            get { return _LASTNAME; }
            set { _LASTNAME = value; }
        }

        private DateOnly? _BIRTHDATE;
        /// <summary>
        /// 
        /// </summary>
        public DateOnly? BIRTHDATE
        {
            get { return _BIRTHDATE; }
            set { _BIRTHDATE = value; }
        }

        private string? _GENDER;
        /// <summary>
        /// 
        /// </summary>
        public string? GENDER
        {
            get { return _GENDER; }
            set { _GENDER = value; }
        }

        private EnumClass.EnumGender? _enumGender;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.EnumGender? enumGender
        {
            get { return _enumGender; }
            set { _enumGender = value; }
        }

        private string? _MOBILEPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? MOBILEPHONE
        {
            get { return _MOBILEPHONE; }
            set { _MOBILEPHONE = value; }
        }

        private string? _NATIONID;
        /// <summary>
        /// 
        /// </summary>
        public string? NATIONID
        {
            get { return _NATIONID; }
            set { _NATIONID = value; }
        }

        private string? _ADDRESS;
        /// <summary>
        /// 
        /// </summary>
        public string? ADDRESS
        {
            get { return _ADDRESS; }
            set { _ADDRESS = value; }
        }

        private string? _AREACODE;
        /// <summary>
        /// 
        /// </summary>
        public string? AREACODE
        {
            get { return _AREACODE; }
            set { _AREACODE = value; }
        }
    }

    #endregion

    #region MRCreate

    public class MRCreateReutrnMsg
    {
        private string? _PatientID;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? PatientID
        {
            get { return _PatientID.NullToString(); }
            set { _PatientID = value; }
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

    public partial class MRCreateClass
    {
        private List<EnumClass.EnumGender>? _GenderList;
        /// <summary>
        /// 性別
        /// </summary>
        public List<EnumClass.EnumGender>? GendetList
        {
            get { return _GenderList; }
            set { _GenderList = value; }
        }

        private List<EnumClass.EnumAnonymous>? _AnonymousList;
        /// <summary>
        /// 無名氏
        /// </summary>
        public List<EnumClass.EnumAnonymous>? AnonymousList
        {
            get { return _AnonymousList; }
            set { _AnonymousList = value; }
        }

        private List<KmuCoderef>? _AreaList;
        /// <summary>
        /// 地區碼
        /// </summary>
        public List<KmuCoderef>? AreaList
        {
            get { return _AreaList; }
            set { _AreaList = value; }
        }

        private List<KmuCoderef>? _RelationList;
        /// <summary>
        /// 關係
        /// </summary>
        public List<KmuCoderef>? RelationList
        {
            get { return _RelationList; }
            set { _RelationList = value; }
        }

        private List<KmuCoderef>? _NationPhoneList;
        /// <summary>
        /// 電話國碼
        /// </summary>
        public List<KmuCoderef>? NationPhoneList
        {
            get { return _NationPhoneList; }
            set { _NationPhoneList = value; }
        }
    }

    #endregion

    #region MREdit

    public class MREditReutrnMsg
    {
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

    #endregion

    #region CreatePhoneCode

    public class GeneratePhoneReutrnMsg
    {
        private List<PhoneCodeList> _phoneList;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public List<PhoneCodeList> phoneList
        {
            get { return _phoneList; }
            set { _phoneList = value; }
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

    public class PhoneCodeList
    {
        private string? _Name;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? Name
        {
            get { return _Name.NullToString(); }
            set { _Name = value; }
        }

        private string? _Code;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? Code
        {
            get { return _Code; }
            set { _Code = value; }
        }
    }

    #endregion

    #region PrintDto

    public class PersonalCardClass
    {
        private string? _PATIENTID;
        /// <summary>
        /// 
        /// </summary>
        public string? PATIENTID
        {
            get { return _PATIENTID; }
            set { _PATIENTID = value; }
        }

        private string? _FIRSTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? FIRSTNAME
        {
            get { return _FIRSTNAME; }
            set { _FIRSTNAME = value; }
        }

        private string? _MIDNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? MIDNAME
        {
            get { return _MIDNAME; }
            set { _MIDNAME = value; }
        }

        private string? _LASTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? LASTNAME
        {
            get { return _LASTNAME; }
            set { _LASTNAME = value; }
        }

        private DateOnly? _BIRTHDATE;
        /// <summary>
        /// 
        /// </summary>
        public DateOnly? BIRTHDATE
        {
            get { return _BIRTHDATE; }
            set { _BIRTHDATE = value; }
        }

        private string? _GENDER;
        /// <summary>
        /// 
        /// </summary>
        public string? GENDER
        {
            get { return _GENDER; }
            set { _GENDER = value; }
        }

        private EnumClass.EnumGender? _enumGender;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.EnumGender? enumGender
        {
            get { return _enumGender; }
            set { _enumGender = value; }
        }
    }

    #endregion

    #region Common 

    public partial class MRJSONStructure
    {
        private string? _PATIENTID;
        /// <summary>
        /// 
        /// </summary>
        public string? PATIENTID
        {
            get { return _PATIENTID; }
            set { _PATIENTID = value; }
        }

        private string? _FIRSTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? FIRSTNAME
        {
            get { return _FIRSTNAME; }
            set { _FIRSTNAME = value; }
        }

        private string? _MIDNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? MIDNAME
        {
            get { return _MIDNAME; }
            set { _MIDNAME = value; }
        }

        private string? _LASTNAME;
        /// <summary>
        /// 
        /// </summary>
        public string? LASTNAME
        {
            get { return _LASTNAME; }
            set { _LASTNAME = value; }
        }

        private DateTime? _BIRTHDATE;
        /// <summary>
        /// 
        /// </summary>
        public DateTime? BIRTHDATE
        {
            get { return _BIRTHDATE; }
            set { _BIRTHDATE = value; }
        }

        private string? _GENDER;
        /// <summary>
        /// 
        /// </summary>
        public string? GENDER
        {
            get { return _GENDER; }
            set { _GENDER = value; }
        }

        private EnumClass.EnumGender? _enumGender;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.EnumGender? enumGender
        {
            get { return _enumGender; }
            set { _enumGender = value; }
        }

        private string? _NATIONALPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? NATIONALPHONE
        {
            get { return _NATIONALPHONE; }
            set { _NATIONALPHONE = value; }
        }

        private string? _AREAPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? AREAPHONE
        {
            get { return _AREAPHONE; }
            set { _AREAPHONE = value; }
        }

        private string? _MOBILEPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? MOBILEPHONE
        {
            get { return _MOBILEPHONE; }
            set { _MOBILEPHONE = value; }
        }

        private string? _NATIONID;
        /// <summary>
        /// 
        /// </summary>
        public string? NATIONID
        {
            get { return _NATIONID; }
            set { _NATIONID = value; }
        }

        private string? _ADDRESS;
        /// <summary>
        /// 
        /// </summary>
        public string? ADDRESS
        {
            get { return _ADDRESS; }
            set { _ADDRESS = value; }
        }

        private string? _REFUGEE_FLAG;
        /// <summary>
        /// 
        /// </summary>
        public string? REFUGEE_FLAG
        {
            get { return _REFUGEE_FLAG; }
            set { _REFUGEE_FLAG = value; }
        }

        private string? _AREACODE;
        /// <summary>
        /// 
        /// </summary>
        public string? AREACODE
        {
            get { return _AREACODE; }
            set { _AREACODE = value; }
        }

        private string? _EMGCONTACT_F;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGCONTACT_F
        {
            get { return _EMGCONTACT_F; }
            set { _EMGCONTACT_F = value; }
        }

        private string? _EMGCONTACT_M;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGCONTACT_M
        {
            get { return _EMGCONTACT_M; }
            set { _EMGCONTACT_M = value; }
        }

        private string? _EMGCONTACT_L;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGCONTACT_L
        {
            get { return _EMGCONTACT_L; }
            set { _EMGCONTACT_L = value; }
        }

        private string? _RELATIONSHIP;
        /// <summary>
        /// 
        /// </summary>
        public string? RELATIONSHIP
        {
            get { return _RELATIONSHIP; }
            set { _RELATIONSHIP = value; }
        }

        private string? _EMGNATIONALPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGNATIONALPHONE
        {
            get { return _EMGNATIONALPHONE; }
            set { _EMGNATIONALPHONE = value; }
        }

        private string? _EMGAREAPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGAREAPHONE
        {
            get { return _EMGAREAPHONE; }
            set { _EMGAREAPHONE = value; }
        }

        private string? _EMGMOBILEPHONE;
        /// <summary>
        /// 
        /// </summary>
        public string? EMGMOBILEPHONE
        {
            get { return _EMGMOBILEPHONE; }
            set { _EMGMOBILEPHONE = value; }
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

        public string? DRUG_ALLERGY { get; set; }
        public string? OTHER_MEDICAL_HISTORY { get; set; }
        public string? NCD_HISTORY { get; set; }

        private string? _ANONYMOUSTYPE;
        /// <summary>
        /// 
        /// </summary>
        public string? ANONYMOUSTYPE
        {
            get { return _ANONYMOUSTYPE; }
            set { _ANONYMOUSTYPE = value; }
        }

        private EnumClass.EnumAnonymous? _enumAnonymous;
        /// <summary>
        /// 
        /// </summary>
        public EnumClass.EnumAnonymous? enumAnonymous
        {
            get { return _enumAnonymous; }
            set { _enumAnonymous = value; }
        }
    }

    #endregion
}
