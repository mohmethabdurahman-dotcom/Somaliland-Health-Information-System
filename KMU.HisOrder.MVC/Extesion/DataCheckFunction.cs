using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;
using KMU.HisOrder.MVC.Models;
using System.Text.RegularExpressions;



namespace KMU.HisOrder.MVC.Extesion
{
    public class DataCheckFunction
    {
        #region Medical Record Data Check

        public static EnumClass.ReplyNoCode InitialGenerateChartCheck(
            MRJSONStructure mRCreateStructure,
            string modifyId, string get_user_auth_cookie)
        {
            

          
                if (mRCreateStructure == null)
            {
                return EnumClass.ReplyNoCode.WrongParam;
            }

            #region 不管是否無名式都要檢核的

            if (null == mRCreateStructure.enumGender)
            {
                return EnumClass.ReplyNoCode.EmptySex;
            }

            #endregion


            #region 有病歷號碼的時候先檢核規則是否正確

            if (false == string.IsNullOrEmpty(mRCreateStructure.PATIENTID))
            {
                EnumClass.ReplyNoCode ptResult = EnumClass.ReplyNoCode.ReplyOK;

                ptResult = CheckPatientID(mRCreateStructure.PATIENTID);
                if (EnumClass.ReplyNoCode.ReplyOK != ptResult)
                {
                    return ptResult;
                }
            }

            #endregion

            #region 非無名氏要檢核的資料

            if (EnumClass.EnumAnonymous.Normal == mRCreateStructure.enumAnonymous)
            {
                if (true == string.IsNullOrEmpty(mRCreateStructure.FIRSTNAME) &&
                    true == string.IsNullOrEmpty(mRCreateStructure.MIDNAME) &&
                    true == string.IsNullOrEmpty(mRCreateStructure.LASTNAME))
                {
                    return EnumClass.ReplyNoCode.EmptyName;
                }

                if (null == mRCreateStructure.BIRTHDATE)
                {
                    return EnumClass.ReplyNoCode.WrongOrEmptyBirthDate;
                }

                if (DateTime.Today < mRCreateStructure.BIRTHDATE)
                {
                    return EnumClass.ReplyNoCode.WrongOrEmptyBirthDate;
                }

                if (mRCreateStructure.BIRTHDATE.Value.Month > 12 ||
                   mRCreateStructure.BIRTHDATE.Value.Day > 31)
                {
                    return EnumClass.ReplyNoCode.WrongOrEmptyBirthDate;
                }

                if (DateTime.Today.AddYears(-120) > mRCreateStructure.BIRTHDATE.Value)
                {
                    return EnumClass.ReplyNoCode.WrongOrEmptyBirthDate;
                }

                #region 電話檢核

                if (true == string.IsNullOrEmpty(mRCreateStructure.NATIONALPHONE))
                {
                    return EnumClass.ReplyNoCode.EmptyNationPhoneCode;
                }

                if (true == string.IsNullOrEmpty(mRCreateStructure.MOBILEPHONE))
                {
                    return EnumClass.ReplyNoCode.EmptyPhoneNumber;
                }
                if ((mRCreateStructure.AREAPHONE+mRCreateStructure.MOBILEPHONE).Length != 10)
                {
                    return EnumClass.ReplyNoCode.WrongPhoneLength;
                }
                if (get_user_auth_cookie == "InpatientMedications")

                {
                    if (true == string.IsNullOrEmpty(mRCreateStructure.ADDRESS))
                    {
                        return EnumClass.ReplyNoCode.EmptyAddress;
                    }
                    if (true == string.IsNullOrEmpty(mRCreateStructure.RELATIONSHIP))
                    {
                        return EnumClass.ReplyNoCode.EmptyRelationship;
                    }


                        if (true == string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_F) && string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_M) && string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_L))
                    {
                        return EnumClass.ReplyNoCode.emptyContactName;
                    }
                    if (true == string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE))
                    {
                        return EnumClass.ReplyNoCode.emptyContactPhone;
                    }

                }
                #endregion

                #region 緊急聯絡人電話檢核

                //2023.04.28 如果有輸入緊急聯絡人電話才檢核 add by elain.
                if (false == string.IsNullOrEmpty(mRCreateStructure.EMGNATIONALPHONE) || 
                    false == string.IsNullOrEmpty(mRCreateStructure.EMGAREAPHONE) ||
                    false == string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE))
                {
                    if (true == string.IsNullOrEmpty(mRCreateStructure.EMGNATIONALPHONE))
                    {
                        return EnumClass.ReplyNoCode.EmgEmptyNationPhoneCode;
                    }

                    if (true == string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE))
                    {
                        return EnumClass.ReplyNoCode.EmgEmptyPhoneNumber;
                    }

                    if ((mRCreateStructure.EMGAREAPHONE + mRCreateStructure.EMGMOBILEPHONE).Length != 10)
                    {
                        return EnumClass.ReplyNoCode.EmgWrongPhoneLength;
                    }
                }

                #endregion
            }

            #endregion           

            return EnumClass.ReplyNoCode.ReplyOK;
        }

        /// <summary>
        /// 檢核病歷號碼規則
        /// </summary>
        /// <param name="patientID"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static EnumClass.ReplyNoCode CheckPatientID(string patientID)
        {
            #region 規則說明

            /*
                病歷號規則: 2碼院區碼(英文)+7碼流水號+1碼檢查碼
                英文A~Z 使用ASCII碼數字 65~90 
                    A:65 B:66  C:67   .............  Z:90

                流水號依序  第[1]碼*1 + 第[2]碼*2 + 第[3]碼*3 +  第[4]碼*7 + 第[5]碼*2 + 第[6]碼*3 + 第[7]碼*7 

                檢查碼  (10 - (英文加總+流水號加總 % 10 餘數)) %10

                Example  1 :  HG0000002[3]  = 72+71+0*1+0*2+0*3+0*7+0*2+0*3+2*7 = 157 => (10 - (153 % 10 ) ) % 10 = [3]

                Example  2 :  DT1543274[9]  = 68+84+1*1+5*2+4*3+3*7+2*2+7*3+4*7 = 241 => (10 - (241 % 10 ) ) % 10 = [9]

            */

            #endregion

            if (true == string.IsNullOrWhiteSpace(patientID))
            {
                return EnumClass.ReplyNoCode.WrongParam;
            }

            char[] pidCharArray = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            int[] pidIDInt = { 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

            patientID = patientID.ToUpper();
            int verifyNum = 0;

            var regex = new Regex("^[A-Z][A-Z]\\d{8}$");
            if (true == regex.IsMatch(patientID))
            {
                // 第一碼
                verifyNum += pidIDInt[Array.BinarySearch(pidCharArray, patientID[0])];

                // 第二碼
                verifyNum += pidIDInt[Array.BinarySearch(pidCharArray, patientID[1])];

                int[] digitArray = new int[7];
                for (int i = 2, j = 0; i < 9; i++, j++)
                {
                    digitArray[j] = int.Parse(patientID[i].ToString());
                }

                verifyNum += digitArray[0] + 2 * digitArray[1] + 3 * digitArray[2] + 7 * digitArray[3] + 2 * digitArray[4] + 3 * digitArray[5] + 7 * digitArray[6];

                // 檢查碼
                verifyNum = (10 - verifyNum % 10) % 10;

                return verifyNum == Convert.ToInt32(patientID[9].ToString()) ?
                        EnumClass.ReplyNoCode.ReplyOK :
                        EnumClass.ReplyNoCode.WrongPatientIDFormat;
            }
            else
            {
                return EnumClass.ReplyNoCode.WrongPatientIDFormat;
            }

            throw new NotImplementedException();
        }

        #endregion


        #region Reserve Data Check

        /// <summary>
        /// 掛號參數檢核
        /// </summary>
        /// <param name="appointmentStructure"></param>
        /// <returns></returns>
        public static EnumClass.ReplyNoCode CheckAppointmentData(KMUContext _context, AppointmentStructure appointmentStructure)
        {
            if (appointmentStructure == null)
            {
                return EnumClass.ReplyNoCode.WrongParam;
            }

            // Allowed too access their data all the previous pataints.
            //if (appointmentStructure.reserveDate < DateTime.Today)
            //{
            //    return EnumClass.ReplyNoCode.ReserveBeforeToday;
            //}

            if(!_context.ClinicSchedules.Where(c=>c.ScheRoom == appointmentStructure.reserveRoom).Any())
            {
                return EnumClass.ReplyNoCode.WrongDept;
            }

            if (!_context.KmuAttributes.Where(c => c.AttrCode == appointmentStructure.reserveAttr).Any())
            {
                return EnumClass.ReplyNoCode.WrongDept;
            }

            if (string.IsNullOrEmpty(appointmentStructure.patientID))
            {
                return EnumClass.ReplyNoCode.EmptyData;
            }

            return EnumClass.ReplyNoCode.ReplyOK;
        }

        #endregion
    }
}
