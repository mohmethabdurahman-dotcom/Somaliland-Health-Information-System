using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;

using KMU.HisOrder.MVC.Extesion;
using static KMU.HisOrder.MVC.Models.EnumClass;
using System.Security.Cryptography;
using System.Text;

namespace KMU.HisOrder.MVC.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _config;
        private readonly KMUContext _db;
        private readonly int Account_ID_maximum_length = 7;
        private readonly int Mobile_Phone_Number_minimum_length = 7;//2023.02.15 電話號碼最少7碼
        private readonly int password_minimum_length = 8;
        private readonly string UserCategory_Coderefs_RefCodetype = "User_Category";
		
        //帳戶鎖定機制
		private readonly int Login_Failed_Lockout___error_count = 5;//身分驗證失敗達「X」次後，
        private readonly int Login_Failed_Lockout___lock_second = 60 * 15;//至少「Y」秒內不允許該帳號繼續嘗試登入

        public LoginController(KMUContext db, IConfiguration config)
        {
            _db = db;
            _config = config; //參考資料  (Get connectionstring from appSettings json c# - Google 搜尋)  [NET Core] 如何讀取 AppSettings.json 組態設定檔 | 余小章 @ 大內殿堂 - 點部落    https://dotblogs.com.tw/yc421206/2020/06/28/how_to_read_config_appsettings_json_via_net_core_31
        }

        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public IActionResult Index()
        {
            if (get_login_useridno() == null) 
            {
                return View("index");
            }            
            else 
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [AllowAnonymous]
        public IActionResult version_histoy()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public ActionResult doLogin(string username, string password)
        {
            ViewBag.notice = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.notice = "User Name cannot be null";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.notice = "Password cannot be null";
            }

            string error_login_message = null;

            if (ViewBag.notice == null)
            {
                LoginPost loginPostContext = new LoginPost();
                loginPostContext.User_IDNo = username;
                loginPostContext.User_Password = password;
                error_login_message = Login(loginPostContext);
            }
            else 
            {
                error_login_message = ViewBag.notice;
            }
			
            if (error_login_message == null)
            {
                password = null;//登入成功清除密碼
                //error_login_message = AccountLoginLockout(username, Login_Failed_Lockout___error_count, Login_Failed_Lockout___lock_second);
                //帳戶鎖定機制
            }

            write_user_log(username, "doLogin", error_login_message == null, error_login_message == null?null: password, error_login_message);//2023.03.03 增加 登入成功與失敗的log

            if (error_login_message == null)
            {// login success
                return RedirectToAction("Index", "Home");
            }
            else
            {// login fail
                TempData["User_Login_Fail"] = "Login failed";//error_login_message;
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public IActionResult NotLogin()
        {
            TempData["User_Login_Fail"] = "not signed in！Please login first";
            return RedirectToAction("Index");
        }

        public IActionResult NotAuth()//當選擇的頁面「沒有權限瀏覽時」經由此路由返回首頁
        {
            TempData["Error_Messages"] = "Your account does not have permission to use the item you clicked";//您的帳號沒有權限使用您點選的項目
            return RedirectToAction("Index", "Home");//返回首頁
        }

        //加入下方會自動檢查是否有認證
        //[Authorize]//改成設定在Program.cs全專案都適用登入驗證
        public IActionResult logout(string message)
        {//登出 
            string get_now_login_useridno = get_login_useridno();
            get_now_login_useridno = get_now_login_useridno == null ? "[Not Found User Login Information]" : get_now_login_useridno;
            write_user_log(get_now_login_useridno, "logout", true, null, message);//2023.03.03 增加 執行登出的log

            run_logout();
            TempData["User_Login_Fail"] = message;//"Successfully logged out, please log in again";
            return RedirectToAction("Index");
        }

        private void run_login(string User_ID,bool Frist_Login_Must_Change_Password=false)
        {
            var check_pwok = _db.KmuUsers.Where(x =>
                                                x.UserIdno == User_ID//obj.User_IDNo
                                                                     //        &&
                                                                     //x.UserPassword == obj.User_Password
                                        );

if (check_pwok.Count() == 1)
{

            //依據2022.10.11會議內容將姓名同步開3欄
            //(比照病歷基本檔 TABLE NAME: SOMACHART)
            string user_name_firstname = check_pwok.FirstOrDefault().UserNameFirstname;
            string user_name_midname = check_pwok.FirstOrDefault().UserNameMidname;
            string user_name_lastname = check_pwok.FirstOrDefault().UserNameLastname;
            //依據2022.10.24會議內容將姓名之間用空白鍵區隔
            string newUserName = user_name_firstname + "　" + user_name_midname + "　" + user_name_lastname;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, User_ID),
                new Claim(ClaimTypes.Name, newUserName)
            };

            //2022.10.27將使用者登入資訊同步存取至 Session 方便大家一次取用減少重工
            var _loginDto = new LoginDTO() { EMPCODE = User_ID, EMPNAME = newUserName };
            HttpContext.Session.SetObject("LoginDTO", _loginDto);
            // Radiology proxy middleware contract requires this key.
            HttpContext.Session.SetString("user_idno", User_ID);

            if (Frist_Login_Must_Change_Password == true)
            {
                string Hard_Code_ProjectId = "Frist_Login";
                //將每一筆權限加入(支援多筆)
                claims.Add(new Claim(ClaimTypes.Role, Hard_Code_ProjectId));
                claims.Add(new Claim("User_auth_page_Name「" + Hard_Code_ProjectId + "」", Hard_Code_ProjectId));
            }
            else
            {

                //讀取該使用者可瀏覽的權限
                var get_this_user_can_view_page_auth = _db.KmuAuths.Where(x =>
                                        x.UserIdno == User_ID
                                );

                foreach (var claim in get_this_user_can_view_page_auth)
                {
                    //將每一筆權限加入(支援多筆)
                    claims.Add(new Claim(ClaimTypes.Role, claim.ProjectId));
                    claims.Add(new Claim("User_auth_page_Name「" + claim.ProjectId + "」", claim.ProjectId));
                }
            }


            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)).GetAwaiter().GetResult();
}//check_can_find_Account ID
        }

        private void run_logout() 
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();
            const string Session_Key = "LoginDTO";
            HttpContext.Session.SetObject(Session_Key, null);
        }

        public ActionResult AdminView()
        {

            return View("/Home/index");
        }

        private string Login(LoginPost obj)
        {
            //ModelState.Remove("Creator");//移除驗證
            //ModelState.Remove("User_Name");//移除驗證

            bool Frist_Login_Must_Change_Password = false;

            if (ModelState.IsValid)
            {
                //確認帳號是否存在
                var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == obj.User_IDNo);
                if (check_id_existed.Count() == 0)
                {
                    return "This Username is not found";
                    //ModelState.AddModelError("User_IDNo", "This User_IDNo is not found");
                }
                else
                {
                    //確認是不是第一次登入
                    var account_info = check_id_existed.FirstOrDefault();

                    //確認狀態是否可以登入
                    if (account_info.AccountStatus != "1") 
                    {
                        return "This Account status does not allow login.";
                    }


                    //第一次登入判斷(或是 經由 忘記密碼 被重置 也是)
                    if (account_info.UserMobilePhone == account_info.UserPassword) 
                    {
                        Frist_Login_Must_Change_Password = true;
                        //驗證 F
                        //2023.02.21 因電話號碼欄位 擴增(虛擬擴增因為超級瑪莉說話不算話是慣性)， 因此調整 初始化重置密碼  改由  「+886國碼」「7區域號碼」「3121101用戶電話號碼」中的  【用戶電話號碼】作為判斷依據
                        string[] new_frist_array = account_info.UserMobilePhone.Split(" ");
                        string new_frist_login_pw = new_frist_array[new_frist_array.Length-1];//取最後一段
                        if (new_frist_login_pw == obj.User_Password)//比對輸入有無一致
                        {//成功
                            obj.User_Password = account_info.UserMobilePhone;
                        }
                        else 
                        {//失敗
                            obj.User_Password = obj.User_Password;
                        }
                    }
                    else
                    {                    
                    //驗證 
                    obj.User_Password = CreateMD5(obj.User_Password);
                    }

                    //確認密碼是否正確
                    var check_pwok = _db.KmuUsers.Where(x =>
                                                x.UserIdno == obj.User_IDNo
                                                        &&
                                                x.UserPassword == obj.User_Password
                                        );

                    if (check_pwok.Count() == 1)
                    {
                        run_login(obj.User_IDNo, Frist_Login_Must_Change_Password);
                        //TempData["User_Login_ID"] = obj.User_IDNo;
                        //TempData["User_Login_Name"] = check_pwok.FirstOrDefault().User_Name;
                        //return RedirectToAction("User_List");
                        //                        return obj.User_IDNo + "登入成功！" + "「" + check_pwok.FirstOrDefault().User_Name + "」" + "您好";
                        return null;
                    }
                    else
                    {
                        //ModelState.AddModelError("User_Password", "This User_Password is not match");
                        return "This Username_Password is not match";
                    }
                }
            }

            //return View(obj);
            return "傳入的內容有缺少資料";
        }
        public class LoginPost
        {
            public string User_IDNo { get; set; }
            public string User_Password { get; set; }
        }

        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public IActionResult Encryption_AES256(string Connection_Strings, string Key, string IV, bool Encryption)
        {
            const int key_check_Length = 16;
            const int iv_check_Length = 8;
            ViewBag.key_equal_Length = key_check_Length.ToString();
            ViewBag.iv_equal_Length = iv_check_Length.ToString();
            if (string.IsNullOrWhiteSpace(Connection_Strings))
            {
                // 未傳入要加密的字串【什麼事都不做】只顯示範例加密用Key金鑰 與 Iv
                ViewData["GetKeyStrings"] = "我是金鑰我是機密別和人說我是金鑰";
                ViewData["GetIvStrings"] = "台塩高級精鹽加碘";
            }
            else
            {
                //參考資料
                //Get connectionstring from appSettings json c# - Google 搜尋
                //[NET Core] 如何讀取 AppSettings.json 組態設定檔 | 余小章 @ 大內殿堂 - 點部落
                //https://dotblogs.com.tw/yc421206/2020/06/28/how_to_read_config_appsettings_json_via_net_core_31
                
                string run_String,run_Key,run_IV;
                run_String = Connection_Strings;
                ViewData["GetConnectionStrings"] = run_String;

                if (string.IsNullOrWhiteSpace(Key))
                {
                    var Key_SystemTitle = _config.GetSection("ConnectionStrings")["Key"];//加密金鑰(32 Byte)
                    run_Key = Key_SystemTitle;
                }
                else 
                {
                    run_Key = Key;
                    ViewData["GetKeyStrings"] = run_Key;
                }


                if (string.IsNullOrWhiteSpace(IV))
                {
                    var IV_HtmlTitle = _config.GetSection("ConnectionStrings")["IV"];//初始向量(Initial Vector, iv) 類似雜湊演算法中的加密鹽(16 Byte)
                    run_IV = IV_HtmlTitle;
                }
                else
                {
                    run_IV = IV;
                    ViewData["GetIvStrings"] = run_IV;
                }

                //驗證key和iv都必須為128bits或192bits或256bits
                //List<int> LegalSizes = new List<int>() { 128, 192, 256 };
                //int key_check_size = 32;
                //int iv_check_size = 16;
                int keyBitLength = run_Key.Length;//int keyBitSize = Encoding.Unicode.GetBytes(run_Key).Length;//Encoding.UTF8.GetBytes(run_Key).Length * 8;
                int ivBitLength = run_IV.Length;//int ivBitSize = Encoding.Unicode.GetBytes(run_IV).Length;//Encoding.UTF8.GetBytes(run_IV).Length * 8;
                //參考資料 aes 256 key iv length - Google 搜尋
                //[.Net] 對稱式加解密，使用AES演算法的Sample Code | 高級打字員的技術雲 - 點部落
                //https://dotblogs.com.tw/shadow/2019/03/20/232821
                if ((keyBitLength != key_check_Length) || (ivBitLength != iv_check_Length))//((keyBitSize != key_check_size) || (ivBitSize != iv_check_size))//(!LegalSizes.Contains(keyBitSize) || !LegalSizes.Contains(ivBitSize))
                {
                    //throw new Exception($@"key或iv的長度不在128bits、192bits、256bits其中一個，輸入的key bits:{keyBitSize},iv bits:{ivBitSize}");
                    TempData["Show_User_Messages"] = "【Error】" + "Key Length：" + keyBitLength + "（conditions must：" + key_check_Length + "）" + "IV Length：" + ivBitLength + "（conditions must：" + iv_check_Length + "）";//TempData["Show_User_Messages"] = "【Error】" + "Key Byte：" + keyBitSize + "（conditions must：" + key_check_size + "）" + "IV Byte：" + ivBitSize + "（conditions must：" + iv_check_size + "）";
                }
                else 
                { 

                TempData["Show_User_Messages"] = "OK";

                TempData["Show_User_Result"] = AES256(run_String, run_Key, run_IV, Encryption);//Do AES256 Encryption true / false Decryption

                }

            }
            return View("AES256");
        }

        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public IActionResult forgot_password()
        {
            return View("ForgotPassword");
        }

        [HttpPost]//2022.11.18上午10點37分與淑文組長討論後，改成先將密碼設定成手機號碼
        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public ActionResult forgotPasswordDoSettingMobileNumberAsDefaultPassword(string username, string PhoneNumber)
        {
            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                check_input_error_message += "Mobile Phone Number cannot be null.";
            }

            

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("forgot_password");
            }

            string error_message = null;
            error_message = ResetPasswordFromUserMobileNumberViaForgotPassword(username, PhoneNumber);

            write_user_log(username, "forgot_password", error_message == null, error_message == null?null: PhoneNumber, error_message);//2023.03.03 增加 忘記密碼的log
            

            if (error_message == null)
            {// ResetPassword success
                TempData["Show_User_Messages"] = "Setting Mobile Number as default password success！！";//" Please Reset password to Activate account！";
                run_login(username,true);//自動登入，並且開啟第一次登入權限，限制一定要改密碼才行
                return RedirectToAction("change_password");
            }
            else
            {// ResetPassword fail
                TempData["Show_User_Messages"] = error_message;
                return RedirectToAction("forgot_password");
            }
        }


        private string ResetPasswordFromUserMobileNumberViaForgotPassword(string User_ID, string User_mobile_phone)
        {
            string ErrorMessage = null;

            //驗證 
            //User_Password = CreateMD5(User_Password);

            //2023.02.21 因電話號碼欄位 擴增(虛擬擴增因為超級瑪莉說話不算話是慣性)， 因此調整 初始化重置密碼  改由  「+886國碼」「7區域號碼」「3121101用戶電話號碼」中的  【用戶電話號碼】作為判斷依據
            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == User_ID);
            if (check_id_existed.Count() == 0)
            {
                return "This  ID and Mobile Phone Number  is not match";
                //隱藏實際原因//return "This Username is not found";
            }
            //確認是不是第一次登入
            var account_info = check_id_existed.FirstOrDefault();
            string[] new_frist_array = account_info.UserMobilePhone.Split(" ");
            string new_frist_login_pw = new_frist_array[new_frist_array.Length - 1];//取最後一段
            if (new_frist_login_pw == User_mobile_phone)//比對輸入有無一致
            {//成功
                User_mobile_phone = account_info.UserMobilePhone;
            }
            else
            {//失敗
                User_mobile_phone = User_mobile_phone;
            }

            //確認密碼是否正確
            var check_Nowpwok = _db.KmuUsers.Where(x =>
                                        x.UserIdno == User_ID
                                                &&
                                        x.UserMobilePhone == User_mobile_phone
                                );

            if (check_Nowpwok.Count() == 1)
            {//執行密碼變更
                var update_obj = check_Nowpwok.FirstOrDefault();
                update_obj.UserPassword = User_mobile_phone;//初始化成第一次登入【不加密】【不加密】【不加密】

                _db.KmuUsers.Update(update_obj);
                _db.SaveChanges();
            }
            else
            {
                ErrorMessage = "This  ID and Mobile Phone Number  is not match";
            }

            return ErrorMessage;
        }

        [HttpPost]
        [AllowAnonymous]//如須例外排除不需要驗證，請加上[AllowAnonymous]
        public ActionResult forgotPasswordDoResetPassword(string username, string PhoneNumber, string NewPassword, string NewPassword2)
        {
            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                check_input_error_message += "Mobile Phone Number cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                check_input_error_message += "New Password cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(NewPassword2))
            {
                check_input_error_message += "New Password Again cannot be null.";
            }

            if (check_input_error_message != null)
            {
                TempData["User_ResetPassword_Fail"] = check_input_error_message;
                return RedirectToAction("forgot_password");
            }

            if (NewPassword.Length < password_minimum_length)
            {//密碼最小限制
                check_input_error_message += "New Password minimum length" + password_minimum_length;
            }

            if (NewPassword != NewPassword2)
            {
                check_input_error_message += "Two input password must be consistent.";//两次输入的密码必须一致 英语怎么说？_百度知道  https://zhidao.baidu.com/question/286212624.html
            }

            if (check_input_error_message != null)
            {
                TempData["User_ResetPassword_Fail"] = check_input_error_message;
                return RedirectToAction("forgot_password");
            }





            string error_ResetPassword_message = null;
            error_ResetPassword_message = ResetPasswordViaForgotPassword(username,  PhoneNumber, NewPassword);


            if (error_ResetPassword_message == null)
            {// ResetPassword success
                TempData["User_Login_Fail"] = "ResetPassword success！！ Please use reset password to login！";
                return RedirectToAction("Index");
            }
            else
            {// ResetPassword fail
                TempData["User_ResetPassword_Fail"] = error_ResetPassword_message;
                return RedirectToAction("forgot_password");
            }
        }


        private string ResetPasswordViaForgotPassword(string User_ID, string User_mobile_phone, string New_User_Password)
        {
            string ErrorMessage=null;

            //驗證 
            //User_Password = CreateMD5(User_Password);

            //確認密碼是否正確
            var check_Nowpwok = _db.KmuUsers.Where(x =>
                                        x.UserIdno == User_ID
                                                &&
                                        x.UserMobilePhone == User_mobile_phone
                                );

            if (check_Nowpwok.Count() == 1)
            {//執行密碼變更
                var update_obj = check_Nowpwok.FirstOrDefault();
                update_obj.UserPassword = CreateMD5(New_User_Password);

                _db.KmuUsers.Update(update_obj);
                _db.SaveChanges();
            }
            else
            {
                ErrorMessage = "This  ID and Mobile Phone Number  is not match";
            }

            return ErrorMessage;
        }

        public IActionResult change_password()
        {
            ViewBag.PasswordChangeRuleMinLength = password_minimum_length.ToString();
            return View("ChangePassword");
        }

        [HttpPost]
        public ActionResult DoChangePassword(string Password, string NewPassword, string NewPassword2)
        {
            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(Password))
            {
                check_input_error_message += "Password cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                check_input_error_message += "New Password cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(NewPassword2))
            {
                check_input_error_message += "New Password Again cannot be null.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_password");
            }

            if (NewPassword != NewPassword2)
            {
                check_input_error_message += "Two input  New  password must be consistent.";//两次输入的密码必须一致 英语怎么说？_百度知道  https://zhidao.baidu.com/question/286212624.html
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_password");
            }

            if (NewPassword.Length < password_minimum_length)
            {//密碼最小限制
                check_input_error_message += "New Password minimum length" + password_minimum_length;
            }

            if ((NewPassword.Any(char.IsUpper)==false) || (NewPassword.Any(char.IsLower) == false))
            {//密碼必須同時包含大小寫//參考資料 https://stackoverflow.com/a/20032476
                check_input_error_message += "New Password must Contain both upper and lower case characters (e.g., a-z, A-Z).";
            }

            if (Password ==  NewPassword)
            {//新舊密碼一樣
                check_input_error_message += "Your Password not Change.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_password");
            }


            string get_user_id = get_login_useridno();

            string error_ChangePassword_message = null;
            error_ChangePassword_message = ResetPasswordViaChangePassword(get_user_id, Password, NewPassword);

            write_user_log(get_user_id, "change_password", error_ChangePassword_message == null, error_ChangePassword_message == null ? null : Password, error_ChangePassword_message);//2023.03.03 增加 修改密碼的log

            if (error_ChangePassword_message == null)
            {// ChangePassword success
                run_logout();
                TempData["User_Login_Fail"] = "ChangePassword success！！ Please use reset password to login！";
                return RedirectToAction("Index");
            }
            else
            {// ChangePassword fail
                TempData["Show_User_Messages"] = error_ChangePassword_message;
                return RedirectToAction("change_password");
            }
        }


        private string ResetPasswordViaChangePassword(string User_ID, string Old_User_Password, string New_User_Password)
        {
            string ErrorMessage = null;
            //驗證 
            string User_Password = null;

            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == User_ID);
            if (check_id_existed.Count() == 0)
            {
                return "This Username 「"+ User_ID + "」 is not found";
            }
            else
            {
                //確認是不是第一次登入
                var account_info = check_id_existed.FirstOrDefault();
                if (account_info.UserMobilePhone == account_info.UserPassword)
                {//第一次登入  密碼未加密
                    //驗證 
                    //2023.02.21 因電話號碼欄位 擴增(虛擬擴增因為超級瑪莉說話不算話是慣性)， 因此調整 初始化重置密碼  改由  「+886國碼」「7區域號碼」「3121101用戶電話號碼」中的  【用戶電話號碼】作為判斷依據
                    string[] new_frist_array = account_info.UserMobilePhone.Split(" ");
                    string new_frist_login_pw = new_frist_array[new_frist_array.Length - 1];//取最後一段
                    if (new_frist_login_pw == Old_User_Password)//比對輸入有無一致
                    {//成功
                        User_Password = account_info.UserMobilePhone;
                    }
                    else
                    {//失敗
                        User_Password = Old_User_Password;
                    }
                }
                else
                {
                    //驗證 
                    User_Password = CreateMD5(Old_User_Password);
                }
            }


            //確認密碼是否正確
            var check_Nowpwok = _db.KmuUsers.Where(x =>
                                        x.UserIdno == User_ID
                                                &&
                                        x.UserPassword == User_Password
                                );

            if (check_Nowpwok.Count() == 1)
            {//執行密碼變更
                var update_obj = check_Nowpwok.FirstOrDefault();
                update_obj.UserPassword = CreateMD5(New_User_Password);

                _db.KmuUsers.Update(update_obj);
                _db.SaveChanges();
            }
            else
            {
                ErrorMessage = "This  Password  is not match";
            }

            return ErrorMessage;
        }

        public IActionResult change_mobile_phone()
        {
            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == get_login_useridno());
            if (check_id_existed.Count() == 0)
            {
                TempData["Error_Messages"] = "This Account ID 「" + get_login_useridno() + "」 is not found";
                return RedirectToAction("Index", "Home");//返回首頁
            }

            var account_info=check_id_existed.FirstOrDefault();
            //2023.02.21 因電話號碼欄位 擴增(虛擬擴增因為超級瑪莉說話不算話是慣性)， 因此調整 初始化重置密碼  改由  「+886國碼」「7區域號碼」「3121101用戶電話號碼」中的  【用戶電話號碼】作為判斷依據
            string[] new_MobilePhone_array = account_info.UserMobilePhone.Split(" ");
            string MobilePhone_Country = new_MobilePhone_array[0];//取第1段

            string MobilePhone_Area = "";
            if (new_MobilePhone_array.Length == 3)
            {
                MobilePhone_Area = new_MobilePhone_array[1];//取第中間段
            }
            else if (new_MobilePhone_array.Length == 2)
            {
                MobilePhone_Area = new_MobilePhone_array[0];//取第1段
            }
            else //只剩1段中間清空
            {
                MobilePhone_Area = "";
            }

            string MobilePhone_Number = new_MobilePhone_array[new_MobilePhone_array.Length - 1];//取最後一段

            ViewData["Phone_Country"] = MobilePhone_Country;
            ViewData["Phone_Area"] = MobilePhone_Area;
            ViewData["Phone_MobilePhone"] = MobilePhone_Number;

            //2023.02.24 電話號碼格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            ViewData["NationPhoneList"] = _db.KmuCoderefs.Where(c => c.RefCodetype == "NationalPhone" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();

            ViewBag.MobilePhoneNumberminlength = Mobile_Phone_Number_minimum_length.ToString();
            return View("ChangeMobilePhone");
        }

        [HttpPost]
        public ActionResult DoChangeMobilePhone(string CountryPhone, string AreaPhone, string MobilePhone)//(string MobilePhone, string NewMobilePhone, string NewMobilePhone2)
        {
            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(CountryPhone))
            {   //2023.03.02 追加檢查 國碼必須選擇
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.EmptyNationPhoneCode;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }

            if (string.IsNullOrWhiteSpace(MobilePhone))
            {
                //2023.03.02 未填寫  電話號碼  統一錯誤訊息
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.EmptyPhoneNumber;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }

            if ((AreaPhone + MobilePhone).Length != 10)
            {   //2023.03.01 補檢查 區域號碼+手機號碼 需要剛好10碼
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.WrongPhoneLength;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }
            /*
            //2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對
            if (string.IsNullOrWhiteSpace(NewMobilePhone))
            {
                check_input_error_message += "New Mobile Phone cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(NewMobilePhone2))
            {
                check_input_error_message += "New Mobile Phone Again cannot be null.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_mobile_phone");
            }

            if (NewMobilePhone != NewMobilePhone2)
            {
                check_input_error_message += "Two input  New Mobile Phone must be consistent.";//两次输入的密码必须一致 英语怎么说？_百度知道  https://zhidao.baidu.com/question/286212624.html
            }
            */

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_mobile_phone");
            }

            string NewMobilePhone = MobilePhone;  //2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對

            if (NewMobilePhone.Length < Mobile_Phone_Number_minimum_length)
            {//最小限制(2022.11.19解除原先比照密碼，基於初始化密碼為手機號碼)
                check_input_error_message += "New Mobile Phone minimum length" + Mobile_Phone_Number_minimum_length;
            }

            //if (MobilePhone == NewMobilePhone) //2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對
            //{//新舊一樣
            //    check_input_error_message += "Your Mobile Phone not Change.";
            //}

            int StringToInt;
            if (int.TryParse(NewMobilePhone,out StringToInt) == false)//參考資料  https://learn.microsoft.com/zh-tw/dotnet/api/system.int32.tryparse?view=net-6.0
            {//Type Check
                check_input_error_message += "Your New Mobile Phone not numbers.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("change_mobile_phone");
            }

            //2023.02.24 電話號碼格式  比照  新增病歷資料  增加國碼與區域號碼 (參考：「+886國碼」「7區域號碼」「3121101用戶電話號碼」)
            NewMobilePhone = CountryPhone + " " + AreaPhone + " " + NewMobilePhone;

            string get_user_id = get_login_useridno();

            string error_ChangeMobilePhone_message = null;
            error_ChangeMobilePhone_message = ResetMobilePhone(get_user_id, MobilePhone, NewMobilePhone);

            write_user_log(get_user_id, "change_mobile_phone", error_ChangeMobilePhone_message == null, error_ChangeMobilePhone_message == null ? null : NewMobilePhone, error_ChangeMobilePhone_message);//2023.03.03 增加 修改電話號碼的log

            if (error_ChangeMobilePhone_message == null)
            {// Change success                
                TempData["Show_User_Messages"] = "Change MobilePhone success！！";
                return RedirectToAction("change_mobile_phone");
            }
            else
            {// Change fail
                TempData["Show_User_Messages"] = error_ChangeMobilePhone_message;
                return RedirectToAction("change_mobile_phone");
            }
        }


        private string ResetMobilePhone(string User_ID, string Old_User_MobilePhone, string New_User_MobilePhone)
        {
            string ResetErrorMessage = null;
            //驗證 //2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對
           // string User_MobilePhone = null;

            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == User_ID);
            if (check_id_existed.Count() == 0)
            {
                return "This Username 「" + User_ID + "」 is not found";
            }
            else
            {
                //驗證 //2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對
             //   User_MobilePhone = Old_User_MobilePhone;
            }


            //確認是否正確
            var check_Nowpwok = _db.KmuUsers.Where(x =>
                                        x.UserIdno == User_ID
                                 //             &&//2023.02.24 新規則電話號碼變成明碼修改 不用比照 舊電話號碼核對
                                 //       x.UserMobilePhone == User_MobilePhone
                                );

            if (check_Nowpwok.Count() == 1)
            {//執行變更
                var update_obj = check_Nowpwok.FirstOrDefault();
                if (update_obj.UserPassword == update_obj.UserMobilePhone)
                {//在執行變更前，先檢查確認是否[第一次登入]尚未變更過密碼
                    return "【change failed】You must have changed your password first.";
                }
                update_obj.UserMobilePhone = New_User_MobilePhone;

                _db.KmuUsers.Update(update_obj);
                _db.SaveChanges();
            }
            else
            {
                ResetErrorMessage = "This  MobilePhone  is not match";
            }

            return ResetErrorMessage;
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult user_list(string search_string)
        {
            ViewData["GetSeatchString"] = search_string;
            IEnumerable<KmuUser> objKmuUserList = null;
                if (string.IsNullOrWhiteSpace(search_string))
                {
                    objKmuUserList = _db.KmuUsers;//.ToList();
                }
                else 
                {
                    objKmuUserList = _db.KmuUsers
                    .Where(s=>s.UserIdno.ToUpper().Contains(search_string.ToUpper())
                    || s.UserNameFirstname.ToUpper().Contains(search_string.ToUpper())
                    || s.UserNameMidname.ToUpper().Contains(search_string.ToUpper())
                    || s.UserNameLastname.ToUpper().Contains(search_string.ToUpper()));//.ToList();
                    TempData["Show_User_Messages"] += "Searching for \""+ search_string + "\" keyword found "+ objKmuUserList .Count()+ " records";
                }
            objKmuUserList = objKmuUserList.OrderBy(o => o.UserIdno);

            //2023.03.09 add show UserCategory
            var get_UserCategory_list = _db.KmuCoderefs.Where(c => c.RefCodetype == UserCategory_Coderefs_RefCodetype);
            //2023.04.14 前端選單讓管理者「可變更 Category」格式  比照  新增使用者帳號  (參考：new_account)
            ViewData["select_CategoryList"] = get_UserCategory_list;

            //2023.03.09 性別格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            List<EnumGender> genderList = new List<EnumGender>();
            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }
            //2023.04.14 前端選單讓管理者「可變更性別」格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            ViewData["mrCreate_GendetList"] = genderList;

            objKmuUserList.ToList().ForEach
                (x => 
                    {
                        x.UserCategory = get_UserCategory_list.Where(c => c.RefCode == x.UserCategory).FirstOrDefault().RefName;
                        x.UserSex= genderList.Where(c => c.EnumToCode() == x.UserSex).FirstOrDefault().EnumToString();
                    }
                );

            return View("UserList", objKmuUserList);
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult auth_setting(string account_id)
        {
            IEnumerable<KmuUser> objKmuUserList = _db.KmuUsers.Where(x => x.UserIdno == account_id).OrderByDescending(o => o.CreateTime);//.ToList(); 

            var check_id_existed = objKmuUserList;
            if (check_id_existed.Count() == 0)
            {
                TempData["Show_User_Messages"] = "This Account ID 「" + account_id + "」 is not found";
                return RedirectToAction("user_list");//找不到這個帳號跳回清單
            }
            else 
            {
                //TempData["Show_User_Messages"] = "20221129【施工中：借User List的View測試】您選擇此 This Account ID 「" + account_id + "」 Auth Setting 設定權限";
                ShowUserAuths showUserAuthsView = new ShowUserAuths();
                showUserAuthsView.user_detail = objKmuUserList.FirstOrDefault();

                var get_user_have_auth = _db.KmuAuths.Where(x => x.UserIdno == account_id).OrderByDescending(o => o.CreateTime);
                showUserAuthsView.user_have_auth = get_user_have_auth;

                var get_all_project_list = _db.KmuProjects
                    .Where(p => p.ProjectId != "BloodBank"
                        && p.ProjectId != "BloodBank_Reception"
                        && p.ProjectId != "BloodBank_Lab")
                    .OrderBy(o => o.CreateTime);

                List<View_KmuProject> get_all_project_list_to_View = new List<View_KmuProject>();
                foreach (KmuProject one_Project in get_all_project_list)
                {
                    get_all_project_list_to_View.Add(new View_KmuProject(one_Project));
                }

                get_all_project_list_to_View.ToList().ForEach
                (x=> {
                        var check_have_auth = get_user_have_auth.Where(c => c.ProjectId == x.db_KmuProject.ProjectId);
                            if (check_have_auth.Count() != 0)
                            {
                                x.auth_create_time = check_have_auth.FirstOrDefault().CreateTime;
                                x.auth_create_user = check_have_auth.FirstOrDefault().Creator;
                            }
                            else 
                            {
                                x.auth_create_time = null;
                                x.auth_create_user = null;
                            }
                     }                
                );

                showUserAuthsView.all_project_list = get_all_project_list_to_View;
                //showUserAuthsView.all_project_list
                //    .Join(showUserAuthsView.user_have_auth, all_project => all_project.ProjectId, user_have => user_have.ProjectId, (all_project, user_have) => new { all_project , user_have })
                //         .ToList()
                //         .ForEach(x =>
                //         {
                //             x.all_project.Url = x.user_have.CreateTime.ToString();
                //             x.all_project.Creator = x.all_project.ProjectId;
                //         });
                return View("UserAuthSetting", showUserAuthsView);
            }
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult account_status_change(string account_id,string login_do)
        {
            IEnumerable<KmuUser> objKmuUserList = _db.KmuUsers.Where(x => x.UserIdno == account_id).OrderByDescending(o => o.CreateTime);//.ToList(); 

            var check_id_existed = objKmuUserList;
            if (check_id_existed.Count() == 0)
            {
                TempData["Show_User_Messages"] = "This Account ID 「" + account_id + "」 is not found";
                return RedirectToAction("user_list");//找不到這個帳號跳回清單
            }
            else
            {
                //確認是否正確
                var check_Nowpwok = check_id_existed;

                string login_do_message = null;

                if (check_Nowpwok.Count() == 1)
                {//執行變更
                    var update_obj = check_Nowpwok.FirstOrDefault();

                    switch (login_do) 
                    {
                        case "Allow_Login":
                            update_obj.AccountStatus = "1";
                            login_do_message = "Can Login";
                            break;
                        case "Close_Login":
                            update_obj.AccountStatus = "0";
                            login_do_message = "Can Not Login";
                            break;
                        default:
                            login_do_message = "do nothing";
                            break;
                    }

                    _db.KmuUsers.Update(update_obj);
                    _db.SaveChanges();
                }

                string Show_User_Messages = "This Account ID 「" + account_id + "」 is "+ login_do_message;
                TempData["Show_User_Messages"] = Show_User_Messages;

                write_user_log(account_id, "account_status_change", true, null, Show_User_Messages);//2023.03.03 增加 開啟或關閉使用者登入權限的log

                return RedirectToAction("user_list");
            }
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult account_BirthDate_change(string? account_id, DateTime? Birth_Date)
        {
            object[] runToDo = DoAccountBirthDateChange(account_id, Birth_Date);

            string Show_User_Messages = (string)runToDo[1];
            TempData["Show_User_Messages"] = Show_User_Messages;

            write_user_log(account_id, "account_BirthDate_change", (bool)runToDo[0], null, Show_User_Messages);//2023.03.03 增加 開啟或關閉使用者登入權限的log

            return RedirectToAction("user_list");
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        private object[] DoAccountBirthDateChange(string? Account,  DateTime? Birthdate)
        {
            object[] result_obj = new object[2];
            result_obj[0] = false;//結果是否完成
            result_obj[1] = "";//回傳訊息


            string check_input_error_message = null;
                      
            if (string.IsNullOrWhiteSpace(Account))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if (Birthdate.HasValue == false)
            {
                check_input_error_message += "Birth Date cannot be null.";
            }

            if (check_input_error_message != null)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = check_input_error_message;//回傳訊息
                return result_obj;
            }



            //------------檢查傳入項目，都有填寫完成後--------------
            IEnumerable<KmuUser> objKmuUserList = _db.KmuUsers.Where(x => x.UserIdno == Account).OrderByDescending(o => o.CreateTime);//.ToList(); 

            var check_id_existed = objKmuUserList;
            if (check_id_existed.Count() == 0)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = "This Account ID 「" + Account + "」 is not found";//回傳訊息
                return result_obj;
            }
            else
            {
                //確認是否正確
                var check_Nowpwok = check_id_existed;

                if (check_Nowpwok.Count() == 1)
                {//執行變更
                    var update_obj = check_Nowpwok.FirstOrDefault();

                    string before_the_change_Value = update_obj.UserBirthDate.Value.ToShortDateString();
                    update_obj.UserBirthDate = Birthdate;

                    _db.KmuUsers.Update(update_obj);
                    _db.SaveChanges();
                    result_obj[0] = true;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is changed Birth Date from 「"+ before_the_change_Value + "」 to 「"+ Birthdate.Value.ToShortDateString() + "」";//回傳訊息
                }
                else 
                {//出現2筆以上
                    result_obj[0] = false;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is found" + check_Nowpwok.Count() + " records" + "「unique constraint violated」";//回傳訊息
                }
            }
            return result_obj;
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult account_Sex_change(string? account_id, string? Sex)
        {
            object[] runToDo = DoAccountSexChange(account_id, Sex);

            string Show_User_Messages = (string)runToDo[1];
            TempData["Show_User_Messages"] = Show_User_Messages;

            write_user_log(account_id, "account_Sex_change", (bool)runToDo[0], null, Show_User_Messages);//2023.03.03 增加 開啟或關閉使用者登入權限的log

            return RedirectToAction("user_list");
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        private object[] DoAccountSexChange(string? Account, string? Sex)
        {
            object[] result_obj = new object[2];
            result_obj[0] = false;//結果是否完成
            result_obj[1] = "";//回傳訊息


            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(Account))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if(string.IsNullOrWhiteSpace(Sex))
            {
                check_input_error_message += "Sex cannot be null.";
            }

            if (check_input_error_message != null)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = check_input_error_message;//回傳訊息
                return result_obj;
            }



            //------------檢查傳入項目，都有填寫完成後【檢查變更項目是否符合選項內項目】--------------




            //2023.04.14 性別格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            List<EnumGender> genderList = new List<EnumGender>();
            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }

            if (genderList.Where(c => c.EnumToString() == Sex).Count() == 0)
            {
                check_input_error_message += "Sex must select a valid item.";
            }


            


            if (check_input_error_message != null)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = check_input_error_message;//回傳訊息
                return result_obj;
            }


            //------------檢查變更項目「符合選項內項目」後--------------
            IEnumerable<KmuUser> objKmuUserList = _db.KmuUsers.Where(x => x.UserIdno == Account).OrderByDescending(o => o.CreateTime);//.ToList(); 

            var check_id_existed = objKmuUserList;
            if (check_id_existed.Count() == 0)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = "This Account ID 「" + Account + "」 is not found";//回傳訊息
                return result_obj;
            }
            else
            {
                //確認是否正確
                var check_Nowpwok = check_id_existed;

                if (check_Nowpwok.Count() == 1)
                {//執行變更
                    var update_obj = check_Nowpwok.FirstOrDefault();

                    string before_the_change_Value = genderList.Where(c => c.EnumToCode() == update_obj.UserSex).FirstOrDefault().EnumToString();
                    update_obj.UserSex = genderList.Where(c => c.EnumToString() == Sex).FirstOrDefault().EnumToCode();

                    _db.KmuUsers.Update(update_obj);
                    _db.SaveChanges();
                    result_obj[0] = true;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is changed Sex from 「" + before_the_change_Value + "」 to 「" + Sex + "」";//回傳訊息
                }
                else
                {//出現2筆以上
                    result_obj[0] = false;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is found" + check_Nowpwok.Count() + " records" + "「unique constraint violated」";//回傳訊息
                }
            }
            return result_obj;
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult account_Category_change(string? account_id, string? Category)
        {
            object[] runToDo = DoAccountCategoryChange(account_id, Category);

            string Show_User_Messages = (string)runToDo[1];
            TempData["Show_User_Messages"] = Show_User_Messages;

            write_user_log(account_id, "account_Category_change", (bool)runToDo[0], null, Show_User_Messages);//2023.03.03 增加 開啟或關閉使用者登入權限的log

            return RedirectToAction("user_list");
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        private object[] DoAccountCategoryChange(string? Account, string? Category)
        {
            object[] result_obj = new object[2];
            result_obj[0] = false;//結果是否完成
            result_obj[1] = "";//回傳訊息


            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(Account))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(Category))
            {
                check_input_error_message += "Category cannot be null.";
            }

            if (check_input_error_message != null)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = check_input_error_message;//回傳訊息
                return result_obj;
            }



            //------------檢查傳入項目，都有填寫完成後【檢查變更項目是否符合選項內項目】--------------




            //2023.04.14 管理者「可變更 Category」格式  比照  新增使用者帳號  (參考：new_account)
            var get_UserCategory_list = _db.KmuCoderefs.Where(c => c.RefCodetype == UserCategory_Coderefs_RefCodetype);

            if (get_UserCategory_list.Where(c => c.RefName == Category ).Count() == 0)
            {
                check_input_error_message += "Category must select a valid item.";
            }





            if (check_input_error_message != null)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = check_input_error_message;//回傳訊息
                return result_obj;
            }


            //------------檢查變更項目「符合選項內項目」後--------------
            IEnumerable<KmuUser> objKmuUserList = _db.KmuUsers.Where(x => x.UserIdno == Account).OrderByDescending(o => o.CreateTime);//.ToList(); 

            var check_id_existed = objKmuUserList;
            if (check_id_existed.Count() == 0)
            {
                result_obj[0] = false;//結果是否完成
                result_obj[1] = "This Account ID 「" + Account + "」 is not found";//回傳訊息
                return result_obj;
            }
            else
            {
                //確認是否正確
                var check_Nowpwok = check_id_existed;

                if (check_Nowpwok.Count() == 1)
                {//執行變更
                    var update_obj = check_Nowpwok.FirstOrDefault();

                    string before_the_change_Value = get_UserCategory_list.Where(c => c.RefCode == update_obj.UserCategory).FirstOrDefault().RefName;
                    update_obj.UserCategory = get_UserCategory_list.Where(c => c.RefName == Category).FirstOrDefault().RefCode;

                    _db.KmuUsers.Update(update_obj);
                    _db.SaveChanges();
                    result_obj[0] = true;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is changed Category from 「" + before_the_change_Value + "」 to 「" + Category + "」";//回傳訊息
                }
                else
                {//出現2筆以上
                    result_obj[0] = false;//結果是否完成
                    result_obj[1] = "This Account ID 「" + Account + "」 is found" + check_Nowpwok.Count() + " records" + "「unique constraint violated」";//回傳訊息
                }
            }
            return result_obj;
        }

        [HttpPost]
        [Authorize(Roles = "UserManagement")] //登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public ActionResult DoUserAuthSetting(List<string> Setting_Auth_Project_ID,string Setting_User_ID)
        {
            string get_admin_id = get_login_useridno();

            string error_message = null;
            error_message = ChangeUserAuthorization(get_admin_id, Setting_Auth_Project_ID, Setting_User_ID);

            if (error_message == null)
            {// Change success                
                TempData["Show_User_Messages"] = "Change Authorization success！！";
                return RedirectToAction("auth_setting", new { account_id = Setting_User_ID });
            }
            else
            {// Change fail
                TempData["Show_User_Messages"] = error_message;
                return RedirectToAction("auth_setting", new { account_id = Setting_User_ID });
            }
        }


        private string ChangeUserAuthorization(string admin_ID, List<string> Auth_Project_ID_string, string User_ID)
        {
            string ResetErrorMessage = null;
            bool change_record = false;

            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == User_ID);
            if (check_id_existed.Count() == 0)
            {
                return "This Username 「" + User_ID + "」 is not found";
            }

            DateTime Change_time = DateTime.Now;//同一動作，統一時間點(當下時間))

            //取得使用者目前的權限
            var get_user_have_auth = _db.KmuAuths.Where(x => x.UserIdno == User_ID).OrderByDescending(o => o.CreateTime);
            var get_all_project_list = _db.KmuProjects.OrderBy(o => o.CreateTime);
            //取得全部可設定的權限

            get_all_project_list.ToList().ForEach
            (x => {
                var check_have_auth = get_user_have_auth.Where(c => c.ProjectId == x.ProjectId);
                if (check_have_auth.Count() != 0)
                {//原本就有
                    if (Auth_Project_ID_string.Contains(x.ProjectId) == false) 
                    { //Delete

                        //確認是否正確
                        var check_Nowpwok = _db.KmuAuths.Where(x_get_check =>
                                                    x_get_check.UserIdno == User_ID
                                                            &&
                                                    x_get_check.ProjectId == x.ProjectId
                                            );

                        if (check_Nowpwok.Count() == 1)
                        {//執行變更
                            var delete_obj = check_Nowpwok.FirstOrDefault();
                            _db.KmuAuths.Remove(delete_obj);
                            _db.SaveChanges();
                            change_record = true;

                            //write log
                            KmuAuthsLog log_obj = new KmuAuthsLog();
                            log_obj.UserIdno = User_ID;
                            log_obj.EditType = "ReMove";
                            log_obj.ProjectId = x.ProjectId;

                            log_obj.EditTime = Change_time;
                            log_obj.EditUser = admin_ID;//建立人

                            _db.KmuAuthsLogs.Add(log_obj);
                            _db.SaveChanges();
                        }
                        else
                        {
                            ResetErrorMessage = "This 「"+ User_ID + "」 User Not Have 「"+ x.ProjectId + "」  Auth";
                        }
                    }
                }
                else
                {//原本沒有
                    if (Auth_Project_ID_string.Contains(x.ProjectId) == true)
                    {//Add
                        KmuAuth obj = new KmuAuth();
                        //初始資訊
                        obj.Creator = admin_ID;//建立人
                        obj.CreateTime = Change_time;

                        obj.UserIdno = User_ID;
                        obj.ProjectId = x.ProjectId;

                        _db.KmuAuths.Add(obj);
                        _db.SaveChanges();
                        change_record = true;

                        //write log
                        KmuAuthsLog log_obj = new KmuAuthsLog();
                        log_obj.UserIdno = User_ID;
                        log_obj.EditType = "add";
                        log_obj.ProjectId = x.ProjectId;

                        log_obj.EditTime = Change_time;
                        log_obj.EditUser = admin_ID;//建立人

                        _db.KmuAuthsLogs.Add(log_obj);
                        _db.SaveChanges();
                    }
                }
            }
            );

            //return "admin_ID"+ admin_ID+ "Auth_Project_ID_string" + Auth_Project_ID_string + "User_ID" + User_ID;

            if ((change_record == false) && (ResetErrorMessage == null))
            {
                ResetErrorMessage = "You Choose Auth 「not Change」.";
            }

            return ResetErrorMessage;
        }

        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public IActionResult new_account()
        {
            ViewBag.AccountIDmaxlength = Account_ID_maximum_length.ToString();
            ViewBag.MobilePhoneNumberminlength = Mobile_Phone_Number_minimum_length.ToString();

            //2023.02.24 電話號碼格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            ViewData["NationPhoneList"] = _db.KmuCoderefs.Where(c => c.RefCodetype == "NationalPhone" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();

            //2023.03.09 性別格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            List<EnumGender> genderList = new List<EnumGender>();
            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }
            ViewData["mrCreate_GendetList"] = genderList;

            var get_UserCategory_list = _db.KmuCoderefs.Where(c => c.RefCodetype == UserCategory_Coderefs_RefCodetype);
            return View("NewAccount", get_UserCategory_list);
        }

        [HttpPost]
        [Authorize(Roles = "UserManagement")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
        public ActionResult DoAddAccount(string Account, string FirstName, string MidName, string LastName, string CountryPhone, string AreaPhone, string MobilePhone, string MobilePhone2,string Category, string Sex, string mail, DateTime Birthdate)
        {
            string check_input_error_message = null;
            //string , string , string , string , string , string MobilePhone2
            if (string.IsNullOrWhiteSpace(Account))
            {
                check_input_error_message += "Account ID cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                check_input_error_message += "First Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(MidName))
            {
                check_input_error_message += "Mid Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                check_input_error_message += "Last Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(CountryPhone))
            {   //2023.03.02 追加檢查 國碼必須選擇
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.EmptyNationPhoneCode;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }

            if (string.IsNullOrWhiteSpace(MobilePhone))
            {
                //2023.03.02 未填寫  電話號碼  統一錯誤訊息
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.EmptyPhoneNumber;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }

            //if (string.IsNullOrWhiteSpace(MobilePhone2))
            //{
            //    check_input_error_message += "Mobile Phone Number Again cannot be null.";
            //}

            if (string.IsNullOrWhiteSpace(Sex))
            {
                check_input_error_message += "Sex cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(mail))
            {
                check_input_error_message += "E-mail cannot be null.";
            }
            else
            {
                string check_email_Format_sign = "@";
                if (mail.IndexOf(check_email_Format_sign) == -1)
                {
                    check_input_error_message += "Email " + check_email_Format_sign + " symbol must be filled in.";
                }
            }

            if (Birthdate == null)
            {
                check_input_error_message += "Birth Date cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(Category))
            {
                check_input_error_message += "Category cannot be null.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("new_account");
            }

            //if (MobilePhone != MobilePhone2)
            //{
            //    check_input_error_message += "Two input  Mobile Phone Number must be consistent.";//两次输入的密码必须一致 英语怎么说？_百度知道  https://zhidao.baidu.com/question/286212624.html
            //}

            if (_db.KmuCoderefs.Where(c => c.RefCode == Category && c.RefCodetype == UserCategory_Coderefs_RefCodetype).Count() == 0) 
            {
                check_input_error_message += "Category must select a valid item."; 
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("new_account");
            }

            if (Account.Length > Account_ID_maximum_length)
            {//帳號最大長度限制
                check_input_error_message += "Account ID maximum length" + Account_ID_maximum_length;
            }

            if (MobilePhone.Length < Mobile_Phone_Number_minimum_length)
            {//手機號碼最小限制(2022.11.19解除原先比照密碼最小限制)
                check_input_error_message += "Mobile Phone minimum length"+ Mobile_Phone_Number_minimum_length;
            }

            if ((AreaPhone + MobilePhone).Length != 10)
            {   //2023.03.01 補檢查 區域號碼+手機號碼 需要剛好10碼
                EnumClass.ReplyNoCode replyCode = EnumClass.ReplyNoCode.WrongPhoneLength;
                EnumClass.DisplayLanguage language = EnumClass.DisplayLanguage.English;
                check_input_error_message += MessageFunction.GetFullReplyNoMsg(replyCode, language).StatusMessage;
            }

            //2023.02.24 電話號碼格式  比照  新增病歷資料  增加國碼與區域號碼 (參考：「+886國碼」「7區域號碼」「3121101用戶電話號碼」)
            MobilePhone = CountryPhone+ " " + AreaPhone + " " + MobilePhone;

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("new_account");
            }


            string get_user_id = get_login_useridno();

            string error_CreateAccount_message = null;
            error_CreateAccount_message = CreateAccount(get_user_id, Account,  FirstName,  MidName,  LastName,  MobilePhone,  Category,  Sex,  mail,  Birthdate);

            write_user_log(Account, "CreateAccount", error_CreateAccount_message == null, error_CreateAccount_message == null ? null : Account, error_CreateAccount_message);//2023.03.03 增加 建立新帳號的log
            

            if (error_CreateAccount_message == null)
            {// ChangePassword success
                
                TempData["Show_User_Messages"] = "Create 「"+ Account + "」 Account success！！ ";
                return RedirectToAction("user_list", new { search_string = Account });//2022.11.25帶創立的新帳號參數//2022.11.21已可跳到帳號清單//未來可跳到帳號清單
            }
            else
            {// ChangePassword fail
                TempData["Show_User_Messages"] = error_CreateAccount_message;
                return RedirectToAction("new_account");
            }
        }


        private string CreateAccount(string User_ID, string Account_ID, string FirstName, string MidName, string LastName, string MobilePhone, string UserCategory, string Sex_Only_M_or_F, string Email, DateTime Birth_date)
        {
            string CreateAccountErrorMessage = null;           

            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == Account_ID);
            if (check_id_existed.Count() != 0)
            {
                CreateAccountErrorMessage= "This Account ID 「" + Account_ID + "」 is already used";
            }
            else
            {
                KmuUser obj = new KmuUser();
                //初始資訊
                obj.Creator = User_ID;//建立人
                obj.CreateTime = DateTime.Now;//當下時間



                obj.UserIdno = Account_ID;
                obj.UserPassword = MobilePhone;
                obj.UserMobilePhone = MobilePhone;

                obj.UserNameFirstname = FirstName;
                obj.UserNameMidname = MidName;
                obj.UserNameLastname = LastName;

                obj.UserCategory = UserCategory;

                obj.UserSex = Sex_Only_M_or_F;
                obj.UserEmail = Email;
                obj.UserBirthDate = Birth_date;

                _db.KmuUsers.Add(obj);
                _db.SaveChanges();
            }          

            return CreateAccountErrorMessage;
        }

        public IActionResult show_account()
        {
            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == get_login_useridno());
            if (check_id_existed.Count() == 0)
            {
                TempData["Error_Messages"] = "This Account ID 「" + get_login_useridno() + "」 is not found";
                return RedirectToAction("Index", "Home");//返回首頁
            }

            var chose_one_account = check_id_existed.FirstOrDefault();

            ViewBag.UserCategory = _db.KmuCoderefs.Where(c => c.RefCode == chose_one_account.UserCategory && c.RefCodetype == UserCategory_Coderefs_RefCodetype).First().RefName;

            return View("ShowAccount", chose_one_account);
        }        

        public IActionResult edit_account()
        {
            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == get_login_useridno());
            if (check_id_existed.Count() == 0)
            {
                TempData["Error_Messages"] = "This Account ID 「" + get_login_useridno() + "」 is not found";
                return RedirectToAction("Index", "Home");//返回首頁
            }


            List<EnumGender> genderList = new List<EnumGender>();// 性別格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }
            //2023.04.27 前端選單讓使用者「可變更性別」自己的性別。格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
            ViewData["mrCreate_GendetList"] = genderList;


            return View("EditAccount", check_id_existed.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult DoEditProfile(string Firstname, string Midname, string Lastname, string mail, string Sex, DateTime? Birthdate)
        {
            string check_input_error_message = null;

            if (string.IsNullOrWhiteSpace(Firstname))
            {
                check_input_error_message += "First Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(Midname))
            {
                check_input_error_message += "Father's Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(Lastname))
            {
                check_input_error_message += "Grandfather's Name cannot be null.";
            }

            if (string.IsNullOrWhiteSpace(mail) == false)
            {
                string check_email_Format_sign = "@";
                if (mail.IndexOf(check_email_Format_sign) == -1)
                {
                    check_input_error_message += "Email " + check_email_Format_sign + " symbol must be filled in.";
                }
            }
            else 
            {
                check_input_error_message += "Email cannot be null.";
            }

            if (check_input_error_message != null)
            {
                TempData["Show_User_Messages"] = check_input_error_message;
                return RedirectToAction("edit_account");
            }

            string get_user_id = get_login_useridno();

            string error_Change_message = null;
            error_Change_message = EditUserProfile(get_user_id,  Firstname,  Midname,  Lastname,  mail, Sex, Birthdate);

            write_user_log(get_user_id, "edit_account", error_Change_message == null, error_Change_message == null ? null : get_user_id, error_Change_message);//2023.03.03 增加 編輯帳號個人資訊的log

            if (error_Change_message == null)
            {// Change success                
                TempData["Show_User_Messages"] = "Edit Profile success！！";
                run_login(get_user_id);//重跑登入程序載入因為變更的基本資料
                return RedirectToAction("edit_account");
            }
            else
            {// Change fail
                TempData["Show_User_Messages"] = error_Change_message;
                return RedirectToAction("edit_account");
            }
        }


        private string EditUserProfile(string User_ID, string Firstname, string Midname, string Lastname, string mail, string Sex, DateTime? Birthdate)
        {
            string ErrorMessage = null;            

            //確認帳號是否存在
            var check_id_existed = _db.KmuUsers.Where(x => x.UserIdno == User_ID);
            if (check_id_existed.Count() == 0)
            {
                return "This Username 「" + User_ID + "」 is not found";
            }

            //確認是否正確
            var check_Nowpwok = check_id_existed;

            if (check_Nowpwok.Count() == 1)
            {//執行變更
                var update_obj = check_Nowpwok.FirstOrDefault();
                if (update_obj.UserPassword == update_obj.UserMobilePhone)
                {//在執行變更前，先檢查確認是否[第一次登入]尚未變更過密碼
                    return "【change failed】You must have changed your password first.";
                }

                //比對 使用者性別 是否有變更
                if (update_obj.UserSex != Sex)
                {//依據2023.04.27 02. S-HIS資訊需求申請_0424 分類 6.2 帳號- Edit Profile 目的 ： 帳號 Birth Date, Sex 還是希望讓使用者可自行修改，減少維護資料工作。
                    
                    List<EnumGender> genderList = new List<EnumGender>();//性別格式  比照  新增病歷資料  (參考：MedRecordController.cs-->MRCreate)
                    foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
                    {
                        genderList.Add(gender);
                    }
                    string after_the_change_Value = genderList.Where(c => c.EnumToCode() == Sex).FirstOrDefault().EnumToString();
                    account_Sex_change(User_ID, after_the_change_Value); //執行變更性別
                }

                //比對 使用者生日 是否有變更
                if (update_obj.UserBirthDate != Birthdate)
                {//依據2023.04.27 02. S-HIS資訊需求申請_0424 分類 6.2 帳號- Edit Profile 目的 ： 帳號 Birth Date, Sex 還是希望讓使用者可自行修改，減少維護資料工作。
                    account_BirthDate_change(User_ID, Birthdate); //執行變更生日
                }

                update_obj.UserNameFirstname = Firstname;
                update_obj.UserNameMidname = Midname;
                update_obj.UserNameLastname = Lastname;
                update_obj.UserEmail = mail;

                _db.KmuUsers.Update(update_obj);
                _db.SaveChanges();
            }
            else
            {
                ErrorMessage = "Error found multiple accounts with the Account ID";//錯誤找到多筆同樣帳號ID
            }

            return ErrorMessage;
        }

        public string get_login_useridno() 
        {
            //從登入資訊取得帳號
         //var get_user_auth_cookie = HttpContext.User.Claims.ToList();
         //string get_user_id = get_user_auth_cookie.Where(t => t.Type == "User_ID").First().Value;
            //從登入資訊取得帳號

            //2022.10.27將使用者登入資訊同步存取至 Session 方便大家一次取用減少重工

            const string Session_Key = "LoginDTO";
            var get_session = HttpContext.Session.GetObject<KMU.HisOrder.MVC.Areas.HisOrder.Models.LoginDTO>(Session_Key);
            if (get_session == null) 
            {
                return null;
            }

            return get_session.EMPCODE;
        }
        public IActionResult change_session(string name,string idno)
        {//This method is testing session change.
            const string Session_Key = "LoginDTO";
            var get_session = HttpContext.Session.GetObject<KMU.HisOrder.MVC.Areas.HisOrder.Models.LoginDTO>(Session_Key);

            string showMessage = "change_session";

            if (string.IsNullOrWhiteSpace(name) == false)
            {
                get_session.EMPNAME = name;
                showMessage += "、name["+ name + "]";
            }

            if (string.IsNullOrWhiteSpace(idno) == false)
            {
                get_session.EMPCODE = idno;
                showMessage += "、idno["+ idno + "]";
            }

            //HttpContext.Session.SetObject(Session_Key, get_session);

            TempData["Error_Messages"] = showMessage;
            return RedirectToAction("Index", "Home");//返回首頁
        }

        //參考資料
        //c# string tomd5 - Google 搜尋
        //c# - Calculate a MD5 hash from a string - Stack Overflow
        //https://stackoverflow.com/a/24031467
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes); // .NET 5 +

                // Convert the byte array to hexadecimal string prior to .NET 5
                // StringBuilder sb = new System.Text.StringBuilder();
                // for (int i = 0; i < hashBytes.Length; i++)
                // {
                //     sb.Append(hashBytes[i].ToString("X2"));
                // }
                // return sb.ToString();
            }
        }
        //

        /// <summary>
        /// 2023.05.04 整併AES256加解密演算法 add by 陳毓凱
        /// </summary>
        /// <param name="Input">輸入字串</param>
        /// <param name="KeyText">加密金鑰(32 Byte)</param>
        /// <param name="IvText">初始向量(Initial Vector, iv) 類似雜湊演算法中的加密鹽(16 Byte)</param>
        /// <param name="Encryption">是否執行加密(Encryption)</param>
        /// <returns></returns>
        public static string AES256(string Input, string KeyText, string IvText, bool Encryption)
        {
            string Output = null;
            //參考資料 [Day14] 資料使用安全(保護連接字串)上 - iT 邦幫忙::一起幫忙解決難題，拯救 IT 人的一天 https://ithelp.ithome.com.tw/articles/10187947

            using (Aes aesAlg = Aes.Create())
            {
                //加密金鑰(32 Byte)
                aesAlg.Key = Encoding.Unicode.GetBytes(KeyText);//"我是金鑰我是機密別和人說我是金鑰"
                //初始向量(Initial Vector, iv) 類似雜湊演算法中的加密鹽(16 Byte)
                aesAlg.IV = Encoding.Unicode.GetBytes(IvText);//"台塩高級精鹽加碘"

                //上方為事前準備 加密解密 相同合併處理

                if (Encryption == true) 
                {
                    //Encryption
                    //加密器
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                    //執行加密
                    byte[] encrypted = encryptor.TransformFinalBlock(Encoding.Unicode.GetBytes(Input), 0, Encoding.Unicode.GetBytes(Input).Length);

                    Output = Convert.ToBase64String(encrypted);
                }
                else 
                {
                    //Decryption 
                    //解密器
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    //執行解密
                    byte[] decrypted = decryptor.TransformFinalBlock(Convert.FromBase64String(Input), 0, Convert.FromBase64String(Input).Length);

                    Output = Encoding.Unicode.GetString(decrypted);
                }                
            }
            return Output;
        }

        private void write_user_log(string User_ID, string event_type, bool isSuccess, string event_error_input = null, string message = null)
        {
            DateTime get_now_time = DateTime.Now;//同一動作，統一時間點(當下時間))

            //write log
            KmuUsersLog log_obj = new KmuUsersLog();
            log_obj.UserIdno = User_ID == null ? "[Not Found User Login Information]" : User_ID;
            log_obj.EventType = event_type;
            log_obj.IsSuccess = isSuccess;
            log_obj.EventErrorInput = event_error_input;
            log_obj.Message = message;


            log_obj.EventTime = get_now_time;//當下時間
            log_obj.Ip = HttpContextExtension.GetUserIPAddress(this.HttpContext);//取得使用者 ip 位置
            log_obj.EventLoggingUser = get_login_useridno();//如果是登入狀態下執行者帳號

            _db.KmuUsersLogs.Add(log_obj);
            _db.SaveChanges();
        }
        //

        private string AccountLoginLockout(string User_ID,int allow_user_login_error_count,int lock_second)//帳戶鎖定機制
        {
            string return_error_message = null;
            DateTime check_login_start_time = DateTime.Now.AddSeconds(-1 * lock_second);//至少「Y」秒內不允許該帳號繼續嘗試登入

            //紀錄本次登入前 log 仍要檢查登入錯誤次數
            int query_error_count_before = _db.KmuUsersLogs.Where(x =>
               x.UserIdno == User_ID
               && x.EventType == "doLogin"
               && x.EventErrorInput != null //針對輸入錯誤的密碼為條件
               && x.EventTime >= check_login_start_time //設定時間內為判斷條件
               ).Count();

            //密碼錯誤上限次數
            if (query_error_count_before >= allow_user_login_error_count)
            {
                //強制 logout
                run_logout();

                //記錄錯誤原因
                return_error_message = "error count[" + query_error_count_before + "]Exceed password retry limit[" + allow_user_login_error_count + "]" + "since[" + check_login_start_time + "](" + lock_second + " seconds ago)";

            }
            return return_error_message;
        }
        //
    }

    public static class HttpContextExtension 
    {
        /// <summary>
        /// 取得使用者連線的來源IP
        /// </summary>
        /// <returns>使用者的IP 如果找不到 則回傳空白</returns>
        public static string GetUserIPAddress(this HttpContext context)
        {//參考資料(http_x_forwarded_for .net core - Google 搜尋)  负载均衡的场景下ASP.NET Core如何获取客户端IP地址 - dudu - 博客园 https://www.cnblogs.com/dudu/p/5972649.html
            try
            {
                string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                //string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string[] addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        return addresses[0];
                    }
                }

                string UserHostAddress = context.Connection.RemoteIpAddress.ToString();//context.Request.UserHostAddress
                //參考資料(userhostaddress .net core - Google 搜尋)UserHostAddress in Asp.net Core - Stack Overflow https://stackoverflow.com/a/44590331

                ipAddress = (UserHostAddress == "::1") ?
                    "127.0.0.1" :
                    UserHostAddress;
                return ipAddress;//.ServerVariables["REMOTE_ADDR"];
            }
            catch (Exception ex)
            {
                return ex.Message;//return String.Empty;
            }

        }
        //引用自OAuthLibrary/Utils/NetworkUtil.cs
    }

    public class ShowUserAuths
    {
    
        public KmuUser user_detail { get; set; }
        
        public IEnumerable<KmuAuth> user_have_auth { get; set; }
    
        public IEnumerable<View_KmuProject> all_project_list { get; set; }
        
    }

}

namespace KMU.HisOrder.MVC.Models
{
    public partial class View_KmuProject
    {//感謝子捷提供方法 解決 View 擴增 虛擬欄位的問題
        public KmuProject db_KmuProject;//private KmuProject _KmuProject;

        public View_KmuProject(KmuProject inKmuProject)
        {
            db_KmuProject = inKmuProject;
        }
        public DateTime? auth_create_time { get; set; }

        public string? auth_create_user { get; set; }
    }
}