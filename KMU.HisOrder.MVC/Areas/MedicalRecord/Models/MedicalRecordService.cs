using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using System.Reflection.Metadata.Ecma335;
using static KMU.HisOrder.MVC.Models.EnumClass;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.Models
{
    public class MedicalRecordService : IDisposable
    {
        private readonly KMUContext _context;


        public MedicalRecordService(KMUContext context)
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
        /// 產生病歷資料
        /// </summary>
        /// <param name="mRCreateStructure"></param>
        /// <param name="ModifyId"></param>
        /// <param name="get_user_auth_cookie"></param>
        /// <returns></returns>
        public MRCreateReutrnMsg GenerateMedicalRecord(
            MRJSONStructure mRCreateStructure,
            EnumClass.DisplayLanguage language,
            string modifyId, string get_user_auth_cookie)
        {
            #region Variable Setting

            MRCreateReutrnMsg result = new MRCreateReutrnMsg();
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;

            #endregion

            #region Step 1. Check Input Variables

            if (null == mRCreateStructure ||
                true == String.IsNullOrWhiteSpace(modifyId))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.WrongParam, language);
                return result;
            }

            #endregion

            try
            {
                #region Data Initial Check

                if (EnumClass.ReplyNoCode.ReplyOK != (replyCode = DataCheckFunction.InitialGenerateChartCheck(mRCreateStructure, modifyId, get_user_auth_cookie)))
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                    return result;
                }

                #endregion

                #region Step 2. Check Already Have Medical Record or not

                if (false == String.IsNullOrWhiteSpace(mRCreateStructure.PATIENTID) &&
                    _context.KmuCharts.Where(c => c.ChrHealthId == mRCreateStructure.PATIENTID).Any())
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.AlreadyExistMedicalRecord, language);
                    return result;
                }

                #endregion

                #region Step3. Create Medical Record Into Database

                string generatePatientID = String.Empty;

                CreateANewMedicalReocrdIntoDatabase(mRCreateStructure, modifyId, ref replyCode, ref generatePatientID);

                if (EnumClass.ReplyNoCode.ReplyOK != replyCode || string.IsNullOrEmpty(generatePatientID))
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                    return result;
                }

                #endregion

                result.PatientID = generatePatientID;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
            }
            catch (Exception ex)
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                return result;
            }


            return result;
        }

        /// <summary>
        /// 更新病歷資料
        /// </summary>
        /// <param name="mRCreateStructure"></param>
        /// <param name="language"></param>
        /// <param name="modifyId"></param>
        /// <returns></returns>
        public MREditReutrnMsg UpdateMedicalRecord(
            MRJSONStructure mRCreateStructure,
            EnumClass.DisplayLanguage language,
            string modifyId, string get_user_auth_cookie)
        {
            #region Variable Setting

            MREditReutrnMsg result = new MREditReutrnMsg();
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;

            #endregion

            #region Step 1. Check Input Variables

            if (null == mRCreateStructure ||
                true == String.IsNullOrWhiteSpace(modifyId))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.WrongParam, language);
                return result;
            }

            #endregion

            try
            {
                #region Data Initial Check

                if (EnumClass.ReplyNoCode.ReplyOK != (replyCode = DataCheckFunction.InitialGenerateChartCheck(mRCreateStructure, modifyId, get_user_auth_cookie)))
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                    return result;
                }

                #endregion

                #region Step 2. Check Already Have Medical Record or not

                if (true == String.IsNullOrWhiteSpace(mRCreateStructure.PATIENTID) &&
                    !_context.KmuCharts.Where(c => c.ChrHealthId == mRCreateStructure.PATIENTID).Any())
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ChartNotExist, language);
                    return result;
                }

                #endregion

                #region Step3. Update Medical Record Database Data

                UpdateMedicalReocrdIntoDatabase(mRCreateStructure, modifyId, ref replyCode);

                if (EnumClass.ReplyNoCode.ReplyOK != replyCode)
                {
                    result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);
                    return result;
                }

                #endregion

                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);

            }
            catch (Exception ex)
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                return result;

            }

            return result;
        }

        /// <summary>
        /// 取得病歷清單
        /// </summary>
        /// <param name="patientID"></param>
        /// <param name="mobilePhone"></param>
        /// <param name="patientName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public PatientListReturn getPatientData(
            string? patientID,
            string? mobilePhone,
            string? patientName,
            EnumClass.DisplayLanguage language)
        {
            #region Variable Setting

            PatientListReturn result = new PatientListReturn();
            List<PatientDataClass> ptList = new List<PatientDataClass>();
            List<KmuChart> chartList = new List<KmuChart>();

            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;
            #endregion

            #region Step 1. Check Input Variables

            if (string.IsNullOrEmpty(patientID) &&
                string.IsNullOrEmpty(mobilePhone) &&
                string.IsNullOrEmpty(patientName))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion


            try
            {
                chartList = _context.KmuCharts.Where(c => 1 == 1).ToList();

                if (!string.IsNullOrEmpty(patientID))
                {
                    chartList = chartList.Where(c => c.ChrHealthId.Contains(patientID.ToUpper().Trim())).ToList();
                }

                if (!string.IsNullOrEmpty(mobilePhone))
                {
                    chartList = chartList.Where(c => c.ChrMobilePhone.Contains(mobilePhone.Trim())).ToList();
                }


                if (!string.IsNullOrEmpty(patientName))
                {
                    chartList = chartList.Where(c => (c.ChrPatientFirstname.ToUpper().Contains(patientName.ToUpper().Trim()) ||
                                                      c.ChrPatientMidname.ToUpper().Contains(patientName.ToUpper().Trim()) ||
                                                      c.ChrPatientLastname.ToUpper().Contains(patientName.ToUpper().Trim()))).ToList();
                }

                if (chartList.Any())
                {
                    foreach (var chart in chartList)
                    {
                        PatientDataClass pt = new PatientDataClass();

                        pt.PATIENTID = chart.ChrHealthId;
                        pt.FIRSTNAME = chart.ChrPatientFirstname;
                        pt.MIDNAME = chart.ChrPatientMidname;
                        pt.LASTNAME = chart.ChrPatientLastname;
                        pt.BIRTHDATE = chart.ChrBirthDate;
                        pt.GENDER = chart.ChrSex;

                        foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
                        {
                            if (gender.EnumToCode() == chart.ChrSex)
                            {
                                pt.enumGender = gender;
                                break;
                            }
                        }

                        pt.NATIONID = chart.ChrNationalId;
                        pt.MOBILEPHONE = chart.ChrMobilePhone;
                        pt.AREACODE = chart.ChrAreaCode;
                        pt.ADDRESS = chart.ChrAddress;

                        ptList.Add(pt);
                    }
                }

                result.ptList = ptList;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.ReplyOK, language);
            }
            catch (Exception ex)
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                result.ptList = new List<PatientDataClass>();

                return result;
            }

            return result;
        }

        public GeneratePhoneReutrnMsg AddNationalPhoneList(string nationalName, string nationalPhoneCode, EnumClass.DisplayLanguage language, string modifyId)
        {
            #region Variable Setting

            GeneratePhoneReutrnMsg result = new GeneratePhoneReutrnMsg();
            List<PhoneCodeList> phoneList = new List<PhoneCodeList>();
            List<KmuCoderef> NationalPhoneList = new List<KmuCoderef>();
            EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.ReplyOK;
            CommonService cService = new CommonService(_context);
            #endregion

            #region Step 1. Check Input Variables

            if (true == String.IsNullOrWhiteSpace(nationalName) ||
                true == String.IsNullOrWhiteSpace(nationalPhoneCode))
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.EmptyParam, language);
                return result;
            }

            #endregion

            NationalPhoneList = cService.GetCodeRef("NationalPhone");

            try
            {
                KmuCoderef newRef = new KmuCoderef()
                {
                    RefCodetype = "NationalPhone",
                    RefCode = nationalName,
                    RefName = nationalName,
                    RefDes = null,
                    RefId = "",
                    RefCasetype = "Y",
                    RefShowseq = NationalPhoneList.Select(c => c.RefShowseq).Max() + 1,
                    ModifyId = modifyId,
                    ModifyTime = DateTime.Now,
                    RefDes2 = null,
                    RefDefaultFlag = nationalPhoneCode
                };

                _context.KmuCoderefs.Add(newRef);
                _context.SaveChanges();
                _context.Entry(newRef).Reload();

                NationalPhoneList = cService.GetCodeRef("NationalPhone");

                foreach (var item in NationalPhoneList.OrderBy(c => c.RefShowseq))
                {
                    PhoneCodeList phone = new PhoneCodeList()
                    {
                        Code = item.RefDefaultFlag,
                        Name = item.RefName
                    };

                    phoneList.Add(phone);
                }

                result.phoneList = phoneList;
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(replyCode, language);

            }
            catch (Exception ex)
            {
                result.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.SystemError, language);
                return result;

            }

            return result;
        }

        #endregion

        #region Private Function

        /// <summary>
        /// 新增病歷主檔 並傳回建立完成後的病歷號碼
        /// </summary>
        /// <param name="mRCreateStructure"></param>
        /// <param name="modifyId"></param>
        /// <param name="replyCode"></param>
        /// <param name="generatePatientID"></param>
        private void CreateANewMedicalReocrdIntoDatabase(
            MRJSONStructure mRCreateStructure,
            string modifyId,
            ref EnumClass.ReplyNoCode replyCode,
            ref string generatePatientID)
        {
            #region Variables Setting

            string newPatientID = String.Empty;   // 產生出來的新病歷號碼
            KmuChart newChart = new KmuChart();
            #endregion

            // 先initialize
            replyCode = EnumClass.ReplyNoCode.ReplyOK;

            if (null == mRCreateStructure ||
                true == String.IsNullOrWhiteSpace(modifyId) ||
                (EnumClass.EnumAnonymous.Normal == mRCreateStructure.enumAnonymous && DateTime.Now <= mRCreateStructure.BIRTHDATE.Value))
            {
                replyCode = EnumClass.ReplyNoCode.WrongParam;
                return;
            }


            #region Create PatientID Number

            List<KmuCoderef> newPatientRef = new List<KmuCoderef>();
            bool isTestUse = false;
            string HospCode = "";

            HospCode = _context.KmuCoderefs.Where(c => c.RefCodetype == "HospitalCode" && c.RefCasetype == "Y").Any() ? _context.KmuCoderefs.Where(c => c.RefCodetype == "HospitalCode" && c.RefCasetype == "Y").FirstOrDefault().RefCode : "";

            if (string.IsNullOrWhiteSpace(HospCode))
            {
                replyCode = EnumClass.ReplyNoCode.HospCodeMissing;
                return;
            }


            if (EnumClass.EnumAnonymous.TestingUse == mRCreateStructure.enumAnonymous)
            {
                newPatientRef = _context.KmuCoderefs.Where(c => c.RefCodetype == "TestChartNoSerialNo" && c.RefCasetype == "Y").ToList();
                isTestUse = true;
            }
            else
            {
                newPatientRef = _context.KmuCoderefs.Where(c => c.RefCodetype == "ChartNoSerialNo" && c.RefCasetype == "Y").ToList();
                isTestUse = false;
            }



            if (!newPatientRef.Any())
            {
                replyCode = EnumClass.ReplyNoCode.SystemError;
                return;
            }

            KmuCoderef ptID = newPatientRef.FirstOrDefault();

            

            if (10 != newPatientRef.FirstOrDefault().RefCode.Trim().Length)
            {
                #region The First Health ID or First Test Health ID

                string FirstChart = "";

                if (isTestUse)
                {
                    FirstChart = "ZZ" + "0000001";
                }
                else
                {
                    FirstChart = HospCode + "0000001";
                }


                string Newdigit9PatientID = FirstChart.Trim().Substring(0, 2) + ((Int32.Parse(FirstChart.Trim().Substring(2, 7))).ToString().PadLeft(7, '0'));
                int[] NewdigitArray = new int[7];
                for (int i = 2, j = 0; i < 9; i++, j++)
                {
                    NewdigitArray[j] = int.Parse(Newdigit9PatientID[i].ToString());
                }


                char[] NewpidCharArray = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
                int[] NewpidIDInt = { 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

                int NewSum =
                NewpidIDInt[Array.BinarySearch(NewpidCharArray, Newdigit9PatientID[0])] + NewpidIDInt[Array.BinarySearch(NewpidCharArray, Newdigit9PatientID[1])] +
                (NewdigitArray[0] + (2 * NewdigitArray[1]) + (3 * NewdigitArray[2]) + (7 * NewdigitArray[3]) + (2 * NewdigitArray[4]) + (3 * NewdigitArray[5]) + (7 * NewdigitArray[6]));

                string NewinputPatientID = Newdigit9PatientID + ((10 - (NewSum % 10)) % 10).ToString();
                
                newChart.ChrHealthId = NewinputPatientID.Trim();

                #endregion
            }
            else
            {
                newChart.ChrHealthId = ptID.RefCode.Trim();
            }



            

            #region Update PatientID Number Into reference

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


            string digit9PatientID = newChart.ChrHealthId.Trim().Substring(0, 2) + ((Int32.Parse(newChart.ChrHealthId.Trim().Substring(2, 7)) + 1).ToString().PadLeft(7, '0'));
            int[] digitArray = new int[7];
            for (int i = 2, j = 0; i < 9; i++, j++)
            {
                digitArray[j] = int.Parse(digit9PatientID[i].ToString());
            }


            char[] pidCharArray = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            int[] pidIDInt = { 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

            int sum =
                pidIDInt[Array.BinarySearch(pidCharArray, digit9PatientID[0])] + pidIDInt[Array.BinarySearch(pidCharArray, digit9PatientID[1])] +
                (digitArray[0] + (2 * digitArray[1]) + (3 * digitArray[2]) + (7 * digitArray[3]) + (2 * digitArray[4]) + (3 * digitArray[5]) + (7 * digitArray[6]));

            string inputPatientID = digit9PatientID + ((10 - (sum % 10)) % 10).ToString();

            KmuCoderef upDate = new KmuCoderef();
            KmuCoderef AnonymousNo = new KmuCoderef();
            if (EnumClass.EnumAnonymous.TestingUse == mRCreateStructure.enumAnonymous)
            {
                upDate = _context.KmuCoderefs.Where(c => c.RefCodetype == "TestChartNoSerialNo" && c.RefCasetype == "Y").First();
            }
            else
            {
                upDate = _context.KmuCoderefs.Where(c => c.RefCodetype == "ChartNoSerialNo" && c.RefCasetype == "Y").First();
            }

            upDate.RefCode = inputPatientID;
            _context.SaveChanges();

            #endregion

            try
            {
                #region For Anonymous

                switch (mRCreateStructure.enumAnonymous)
                {
                    case EnumClass.EnumAnonymous.TestingUse:
                        newChart.ChrPatientFirstname = "Test_" + newChart.ChrHealthId;
                        newChart.ChrPatientMidname = "";
                        newChart.ChrPatientLastname = "";
                        newChart.ChrSex = EnumClass.EnumGender.Male.EnumToCode();
                        newChart.ChrBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
                        break;
                    case EnumClass.EnumAnonymous.MaleAdult:
                    case EnumClass.EnumAnonymous.MaleKid:
                    case EnumClass.EnumAnonymous.FemaleAdult:
                    case EnumClass.EnumAnonymous.FemaleKid:

                        newChart.ChrPatientFirstname = "Anonymous";
                        newChart.ChrPatientMidname = mRCreateStructure.enumAnonymous.EnumToString();
                        AnonymousNo = _context.KmuCoderefs.Where(c => c.RefCodetype == "Anonymous_No").First();
                        if (AnonymousNo.RefName == DateTime.Today.ToString("yyyyMMdd"))
                        {
                            newChart.ChrPatientLastname = DateTime.Today.ToString("MMdd") + "_" + AnonymousNo.RefCode;
                            AnonymousNo.RefCode = (Convert.ToInt16(AnonymousNo.RefCode) + 1).ToString().PadLeft(2, '0');
                            _context.SaveChanges();
                        }
                        else
                        {
                            newChart.ChrPatientLastname = DateTime.Today.ToString("MMdd") + "_01";
                            AnonymousNo.RefCode = "01";
                            AnonymousNo.RefName = DateTime.Today.ToString("yyyyMMdd");
                            _context.SaveChanges();
                        }
                        switch (mRCreateStructure.enumAnonymous)
                        {
                            case EnumClass.EnumAnonymous.MaleAdult:
                                newChart.ChrSex = EnumClass.EnumGender.Male.EnumToCode();
                                newChart.ChrBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
                                break;

                            case EnumClass.EnumAnonymous.MaleKid:
                                newChart.ChrSex = EnumClass.EnumGender.Male.EnumToCode();
                                newChart.ChrBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
                                break;

                            case EnumClass.EnumAnonymous.FemaleAdult:
                                newChart.ChrSex = EnumClass.EnumGender.Female.EnumToCode();
                                newChart.ChrBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
                                break;

                            case EnumClass.EnumAnonymous.FemaleKid:
                                newChart.ChrSex = EnumClass.EnumGender.Female.EnumToCode();
                                newChart.ChrBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
                                break;
                        }
                        break;

                    case EnumClass.EnumAnonymous.Normal:
                    default:
                        newChart.ChrPatientFirstname = string.IsNullOrEmpty(mRCreateStructure.FIRSTNAME) ? "" : mRCreateStructure.FIRSTNAME.Trim();
                        newChart.ChrPatientMidname = string.IsNullOrEmpty(mRCreateStructure.MIDNAME) ? "" : mRCreateStructure.MIDNAME.Trim();
                        newChart.ChrPatientLastname = string.IsNullOrEmpty(mRCreateStructure.LASTNAME) ? "" : mRCreateStructure.LASTNAME.Trim();
                        newChart.ChrSex = mRCreateStructure.enumGender.EnumToCode();
                        newChart.ChrBirthDate = mRCreateStructure.BIRTHDATE.HasValue ? DateOnly.FromDateTime(mRCreateStructure.BIRTHDATE.Value) : null;
                        break;
                }

                #endregion

                newChart.ChrNationalId = string.IsNullOrEmpty(mRCreateStructure.NATIONID) ? "" : mRCreateStructure.NATIONID.Trim();
                newChart.ChrMobilePhone = (string.IsNullOrEmpty(mRCreateStructure.NATIONALPHONE) ? "" : mRCreateStructure.NATIONALPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.AREAPHONE) ? "" : mRCreateStructure.AREAPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.MOBILEPHONE) ? "" : mRCreateStructure.MOBILEPHONE.Trim());
                newChart.ChrAddress = string.IsNullOrEmpty(mRCreateStructure.ADDRESS) ? "" : mRCreateStructure.ADDRESS.Trim();
                newChart.ChrAreaCode = string.IsNullOrEmpty(mRCreateStructure.AREACODE) ? "" : mRCreateStructure.AREACODE.Trim();
                newChart.ChrRefugeeFlag = Convert.ToChar(mRCreateStructure.REFUGEE_FLAG);
                #region 緊急聯絡人姓名

                newChart.ChrEmgContact = (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_F) ? "" : mRCreateStructure.EMGCONTACT_F.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_M) ? "" : mRCreateStructure.EMGCONTACT_M.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_L) ? "" : mRCreateStructure.EMGCONTACT_L.Trim());

                #endregion

                newChart.ChrContactRelation = string.IsNullOrEmpty(mRCreateStructure.RELATIONSHIP) ? "" : mRCreateStructure.RELATIONSHIP.Trim();
                newChart.ChrContactPhone = (string.IsNullOrEmpty(mRCreateStructure.EMGNATIONALPHONE) ? "" : mRCreateStructure.EMGNATIONALPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGAREAPHONE) ? "" : mRCreateStructure.EMGAREAPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE) ? "" : mRCreateStructure.EMGMOBILEPHONE.Trim());

                //newChart.ChrContactPhone = (string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE) ? "" : mRCreateStructure.EMGMOBILEPHONE.Trim());

                newChart.ChrDrugAllergy = string.IsNullOrEmpty(mRCreateStructure.DRUG_ALLERGY) ? "" : mRCreateStructure.DRUG_ALLERGY.Trim();
                newChart.ChrOtherMedicalHistory = string.IsNullOrEmpty(mRCreateStructure.OTHER_MEDICAL_HISTORY) ? "" : mRCreateStructure.OTHER_MEDICAL_HISTORY.Trim();
                newChart.ChrNcdHistory = string.IsNullOrEmpty(mRCreateStructure.NCD_HISTORY) ? "" : mRCreateStructure.NCD_HISTORY.Trim();
                
                newChart.ChrRemark = string.IsNullOrEmpty(mRCreateStructure.REMARK) ? "" : mRCreateStructure.REMARK.Trim();

                newChart.ModifyUser = modifyId;
                newChart.ModifyTime = DateTime.Now;

                _context.KmuCharts.Add(newChart);

                //2023.04.28 kmu_chart_log add by elain.
                ModifyMedicalReocrdLog('I', newChart);

                _context.SaveChanges();

                replyCode = EnumClass.ReplyNoCode.ReplyOK;
                generatePatientID = newChart.ChrHealthId;
            }
            catch (Exception ex)
            {
                replyCode = EnumClass.ReplyNoCode.SystemError;
            }

            #endregion

            return;
        }

        /// <summary>
        /// 更新病歷主檔
        /// </summary>
        /// <param name="mRCreateStructure"></param>
        /// <param name="modifyId"></param>
        /// <param name="replyCode"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateMedicalReocrdIntoDatabase(
            MRJSONStructure mRCreateStructure,
            string modifyId,
            ref EnumClass.ReplyNoCode replyCode)
        {
            #region Variables Setting

            KmuChart updateChart = new KmuChart();
            #endregion

            // 先initialize
            replyCode = EnumClass.ReplyNoCode.ReplyOK;

            if (null == mRCreateStructure ||
                true == String.IsNullOrWhiteSpace(modifyId) ||
                (EnumClass.EnumAnonymous.Normal == mRCreateStructure.enumAnonymous && DateTime.Now <= mRCreateStructure.BIRTHDATE.Value))
            {
                replyCode = EnumClass.ReplyNoCode.WrongParam;
                return;
            }

            var chartDtos = _context.KmuCharts.Where(c => c.ChrHealthId == mRCreateStructure.PATIENTID);

            if (!chartDtos.Any())
            {
                replyCode = EnumClass.ReplyNoCode.ChartNotExist;
                return;
            }

            try
            {
                updateChart = chartDtos.First();

                updateChart.ChrPatientFirstname = string.IsNullOrEmpty(mRCreateStructure.FIRSTNAME) ? "" : mRCreateStructure.FIRSTNAME.Trim();
                updateChart.ChrPatientMidname = string.IsNullOrEmpty(mRCreateStructure.MIDNAME) ? "" : mRCreateStructure.MIDNAME.Trim();
                updateChart.ChrPatientLastname = string.IsNullOrEmpty(mRCreateStructure.LASTNAME) ? "" : mRCreateStructure.LASTNAME.Trim();
                updateChart.ChrSex = mRCreateStructure.enumGender.EnumToCode();
                updateChart.ChrBirthDate = mRCreateStructure.BIRTHDATE.HasValue ? DateOnly.FromDateTime(mRCreateStructure.BIRTHDATE.Value) : null;

                updateChart.ChrNationalId = string.IsNullOrEmpty(mRCreateStructure.NATIONID) ? "" : mRCreateStructure.NATIONID.Trim();
                updateChart.ChrMobilePhone = (string.IsNullOrEmpty(mRCreateStructure.NATIONALPHONE) ? "" : mRCreateStructure.NATIONALPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.AREAPHONE) ? "" : mRCreateStructure.AREAPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.MOBILEPHONE) ? "" : mRCreateStructure.MOBILEPHONE.Trim());
                updateChart.ChrAddress = string.IsNullOrEmpty(mRCreateStructure.ADDRESS) ? "" : mRCreateStructure.ADDRESS.Trim();
                updateChart.ChrAreaCode = string.IsNullOrEmpty(mRCreateStructure.AREACODE) ? "" : mRCreateStructure.AREACODE.Trim();
                updateChart.ChrRefugeeFlag = Convert.ToChar(mRCreateStructure.REFUGEE_FLAG);
                #region 緊急聯絡人姓名

                updateChart.ChrEmgContact = (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_F) ? "" : mRCreateStructure.EMGCONTACT_F.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_M) ? "" : mRCreateStructure.EMGCONTACT_M.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGCONTACT_L) ? "" : mRCreateStructure.EMGCONTACT_L.Trim());

                #endregion
                updateChart.ChrContactRelation = string.IsNullOrEmpty(mRCreateStructure.RELATIONSHIP) ? "" : mRCreateStructure.RELATIONSHIP.Trim();
                updateChart.ChrContactPhone = (string.IsNullOrEmpty(mRCreateStructure.EMGNATIONALPHONE) ? "" : mRCreateStructure.EMGNATIONALPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGAREAPHONE) ? "" : mRCreateStructure.EMGAREAPHONE.Trim()) + " " +
                                             (string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE) ? "" : mRCreateStructure.EMGMOBILEPHONE.Trim());

                //updateChart.ChrContactPhone = (string.IsNullOrEmpty(mRCreateStructure.EMGMOBILEPHONE) ? "" : mRCreateStructure.EMGMOBILEPHONE.Trim());

                updateChart.ChrDrugAllergy = string.IsNullOrEmpty(mRCreateStructure.DRUG_ALLERGY) ? "" : mRCreateStructure.DRUG_ALLERGY.Trim();
                updateChart.ChrOtherMedicalHistory = string.IsNullOrEmpty(mRCreateStructure.OTHER_MEDICAL_HISTORY) ? "" : mRCreateStructure.OTHER_MEDICAL_HISTORY.Trim();
                updateChart.ChrNcdHistory = string.IsNullOrEmpty(mRCreateStructure.NCD_HISTORY) ? "" : mRCreateStructure.NCD_HISTORY.Trim();

                updateChart.ChrRemark = string.IsNullOrEmpty(mRCreateStructure.REMARK) ? "" : mRCreateStructure.REMARK.Trim();

                updateChart.ModifyUser = modifyId;
                updateChart.ModifyTime = DateTime.Now;

                //2023.04.28 kmu_chart_log add by elain.
                ModifyMedicalReocrdLog('U', updateChart);

                _context.SaveChanges();

                replyCode = EnumClass.ReplyNoCode.ReplyOK;
            }
            catch (Exception ex)
            {
                replyCode = EnumClass.ReplyNoCode.SystemError;
            }

            return;
        }

        /// <summary>
        /// 紀錄kmu_chart_log
        /// </summary>
        /// <param name="inMode">I:新增/U:修改/D:刪除</param>
        /// <param name="inChart">KmuChart</param>
        private void ModifyMedicalReocrdLog(char inMode, KmuChart inChart)
        {
            //1. KmuChart Dto to json
            var jsonString = JsonConvert.SerializeObject(inChart);

            //2. json to KmuChartLog Dto
            var logDto = (KmuChartLog)JsonConvert.DeserializeObject(jsonString, typeof(KmuChartLog));
            logDto.LogMode = inMode;
            logDto.LogTime = DateTime.Now;
            logDto.LogUser = inChart.ModifyUser;
            logDto.LogId = "";

            //3. insert
            _context.KmuChartLogs.Add(logDto);
        }

        #endregion
    }    
}
