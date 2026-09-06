using System.ComponentModel;

namespace KMU.HisOrder.MVC.Models
{
    public class EnumClass
    {
        /// <summary>
        /// 性別
        /// </summary>
        public enum EnumGender
        {
            /// <summary>
            /// 男性
            /// </summary>
            [Description("M")]
            Male,

            /// <summary>
            /// 女性
            /// </summary>
            [Description("F")]
            Female,
        }

        public enum EnumNoon
        {
            /// <summary>
            /// 
            /// </summary>
            [Description("AM")]
            AM,
            ///// <summary>
            ///// 
            ///// </summary>
            //[Description("PM")]
            //PM,
            ///// <summary>
            ///// 
            ///// </summary>
            //[Description("Night")]
            //Night,
        }
        public enum EnumAnonymous
        {
            /// <summary>
            /// 有證件
            /// </summary>
            [Description("0")]
            Normal,

            /// <summary>
            /// 無證件男性成人
            /// </summary>
            [Description("1")]
            MaleAdult,

            /// <summary>
            /// 無證件女性成人
            /// </summary>
            [Description("2")]
            FemaleAdult,

            /// <summary>
            /// 無證件男性小孩
            /// </summary>
            [Description("3")]
            MaleKid,

            /// <summary>
            /// 無證件女性小孩
            /// </summary>
            [Description("4")]
            FemaleKid,


            /// <summary>
            /// 測試用病歷號
            /// </summary>
            [Description("Z")]
            TestingUse,
        }


        #region Error Message

        public enum ReplyNoCode
        {
            /// <summary>
            /// 正確的資料
            /// </summary>
            ReplyOK = 0,

            /// <summary>
            /// 查無病歷
            /// </summary>
            [Description("Find No Medical Record")]
            ChartNotExist = 1,

            /// <summary>
            /// 證件號碼已經存在於病歷主檔
            /// </summary>
            [Description("Medical Record Already Exist")]
            ExistingPatientID = 2,

            /// <summary>
            /// 輸入空白的姓名
            /// </summary>
            [Description("Empty Name")]
            EmptyName = 3,

            /// <summary>
            /// 錯誤的出生日期
            /// </summary>
            [Description("Empty Or Wrong Birth Date Format")]
            WrongOrEmptyBirthDate = 4,

            /// <summary>
            /// 錯誤的性別
            /// </summary>
            [Description("Wrong Sex")]
            EmptySex = 5,

            /// <summary>
            /// 空白的手機號碼
            /// </summary>
            [Description("Empty Phone Number")]
            EmptyPhoneNumber = 6,

            [Description("Area/Address is required")]
            emptyArea = 101,
            [Description("Address is required")]
            EmptyAddress = 66,

            [Description("Relationship is required")]
            EmptyRelationship = 67,

            [Description("Amergency Contact name is required")]
              emptyContactName = 102,
            [Description("Amergency Contact phone is required")]
             emptyContactPhone = 103,


            /// <summary>
            /// 緊急聯絡人空白的手機號碼
            /// </summary>
            [Description("Emergency contact phone number is empty")]
            EmgEmptyPhoneNumber = 206,

            /// <summary>
            /// 錯誤的病歷號格式
            /// </summary>
            [Description("Wrong Medical Number Format")]
            WrongPatientIDFormat = 7,

            /// <summary>
            /// 病歷號碼已存在
            /// </summary>
            [Description("Medical Number Already Exist")]
            AlreadyExistMedicalRecord = 8,

            /// <summary>
            /// 不符合年齡限制
            /// </summary>
            [Description("Age Not Allow")]
            NotAllowedAge = 9,

            /// <summary>
            /// 掛號今天以前的日期
            /// </summary>
            [Description("Make Appointment Today Before")]
            ReserveBeforeToday = 10,

            /// <summary>
            /// 錯誤的診間資訊
            /// </summary>
            [Description("Wrong Clinic Detail")]
            WrongDept = 11,

            /// <summary>
            /// 錯誤的身分
            /// </summary>
            [Description("Wrong Attribute")]
            WrongAttt = 12,

            /// <summary>
            /// 當日查無該診間號
            /// </summary>
            [Description("Find No Clinic Data Today")]
            NoScheduleToday = 13,

            /// <summary>
            /// 當日門診休診
            /// </summary>
            [Description("This Clinic Off Today")]
            DayOff = 14,

            /// <summary>
            /// 號碼重複
            /// </summary>
            [Description("Ticket Number Occupied")]
            OccupiedSeqNo = 15,

            /// <summary>
            /// 查無掛號資料
            /// </summary>
            [Description("Find No Appointment Data")]
            NoReserveData = 16,

            /// <summary>
            /// 已完成看診不可取消
            /// </summary>
            [Description("Finish Order Can't Cancel Appointment")]
            DoneAlready = 17,

            /// <summary>
            /// 已取消掛號
            /// </summary>
            [Description("Canceled Appointment Already")]
            CancelAlready = 18,

            /// <summary>
            /// 空白的手機國碼
            /// </summary>
            [Description("Empty National Phone Code")]
            EmptyNationPhoneCode =19,

            /// <summary>
            /// 緊急聯絡人空白的手機國碼
            /// </summary>
            [Description("Emergency contact national phone code is empty")]
            EmgEmptyNationPhoneCode = 219,
            
            
            [Description("This department is already exit")]
            DepartmentExist = 28,

            /// <summary>
            /// 手機長度不正確
            /// </summary>
            [Description("Length of Phone Number is not correct")]
            WrongPhoneLength = 20,

            /// <summary>
            /// 緊急聯絡人手機長度不正確
            /// </summary>
            [Description("Emergency contact phone number length is incorrect")]
            EmgWrongPhoneLength = 220,

            /// <summary>
            /// 手機長度不正確
            /// </summary>
            [Description("You Have an Appointment Already")]
            RegisterAlready = 21,

            [Description("something went wrong please try again later")]
            somethingWrong = 27,

            [Description("Faild to registar this patient, please try agin!")]
            FaildRegistration = 29,

            [Description("You Have an Appointment Already, please wait a moment")]
            RegisterAlreadyWait = 212,
            /// <summary>
            /// 手機長度不正確
            /// </summary>
            [Description("KmuCoderef HospCode Missing!")]
            HospCodeMissing = 22,

            /// <summary>
            /// 此掛號無法取消
            /// </summary>
            [Description("Already On Call,Can't Cancel!")]
            CanNotCancel = 23,

            /// <summary>
            /// 醫師代碼不存在
            /// </summary>
            [Description("Doctor Code does not exist!")]
            DoctorIsntExist = 24,

            /// <summary>
            /// 科別代碼不存在
            /// </summary>
            [Description("Department Code does not exist!")]
            DeptIsntExist = 25,

            /// <summary>
            /// 診次已存在
            /// </summary>
            [Description("Clinic already exist!")]
            ClinicExist = 26,

            /// <summary>
            /// 錯誤的使用者代碼 找不到
            /// </summary>
            [Description("Wrong User Code")]
            InvalidModifyID = 71,

            /// <summary>
            /// 使用者已離職的錯誤
            /// </summary>
            [Description("User Has Beed Quited")]
            ExpiredModifyID = 72,

            /// <summary>
            /// 查無資料
            /// </summary>
            [Description("Find No Data")]
            EmptyData = 97,

            /// <summary>
            /// 空白的參數
            /// </summary>
            [Description("Empty Parameter")]
            EmptyParam = 98,

            /// <summary>
            /// 錯誤的參數
            /// </summary>
            [Description("Wrong Parameter")]
            WrongParam = 99,

            /// <summary>
            /// 系統發生錯誤
            /// </summary>
            [Description("System Error")]
            SystemError = 100,
        }

        #endregion

        #region 

        public enum DisplayLanguage
        {
            /// <summary>
            /// 中文
            /// </summary>
            [Description("zh-TW")]
            Chinese,

            /// <summary>
            /// 英文
            /// </summary>
            [Description("en-US")]
            English,

            /// <summary>
            /// 未知
            /// </summary>
            [Description("")]
            Unkmow,
        }

        public enum ActionCode
        {
            /// <summary>
            /// 
            /// </summary>
            [Description("I")]
            Create,

            /// <summary>
            /// 
            /// </summary>
            [Description("U")]
            Update,

            /// <summary>
            /// 
            /// </summary>
            [Description("D")]
            Delete,
        }

        public enum OpenFlag
        {
            /// <summary>
            /// 
            /// </summary>
            [Description("Y")]
            On,

            /// <summary>
            /// 
            /// </summary>
            [Description("N")]
            Off,
        }

        public enum EnumStatus
        {
            /// <summary>
            /// 未看診
            /// </summary>
            [Description("N")]
            None,

            /// <summary>
            /// 暫存
            /// </summary>
            [Description("T")]
            TempSave,

            /// <summary>
            /// 完成看診
            /// </summary>
            [Description("*")]
            Done

        }

        #endregion
    }



}
