using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Models;
using System.Globalization;
using System.IO.Pipelines;
using System.Resources;

namespace KMU.HisOrder.MVC.Extesion
{
    public class MessageFunction
    {
        /// <summary>
        /// 傳回多語系的reply code string
        /// </summary>
        /// <param name="code">原始的reply code</param>
        /// <param name="lang">要顯示的語言</param>
        /// <param name="args">需要額外帶參數的訊息的參數內容 數量不固定</param>
        /// <returns>根據reply code與要顯示的語言 所查到的回應字串</returns>
        public static string GetReplyNoMsg(EnumClass.ReplyNoCode code, EnumClass.DisplayLanguage lang, params string[] args)
        {
            //ResourceManager manager = new ResourceManager("KMU.Registration.Resources.Res", typeof(MessageFunction).Assembly);

            //// Updated by lcm
            //CultureInfo cul = CultureInfo.CreateSpecificCulture(lang.EnumToCode());
            string replyMsg = String.Empty;   // 回傳的結果

            //manager.IgnoreCase = true;

            //replyMsg = manager.GetString(code.ToString(), cul);

            /////如果有帶參數 就format string
            //if (null != args && 0 < args.Count())
            //{
            //    replyMsg = String.Format(replyMsg, args);
            //}

            return replyMsg;
        }

        /// <summary>
        /// 傳回在ReplyNo前方補0的字串
        /// 因為ReplyNo為Enum 所以要先轉為int
        /// </summary>
        /// <param name="code">要轉換的ReplyNo Code</param>
        /// <returns>轉換後的字串</returns>
        public static string GetFormattedReplyNo(EnumClass.ReplyNoCode code)
        {
            if (EnumClass.ReplyNoCode.ReplyOK == code)
            {
                return String.Empty;    // 因為原本健璋的service會判斷空字串為reply ok, 所以要多加上這個case判斷
            }

            return String.Format("{0:00}", (int)code);
        }


        public static ReturnMsg GetFullReplyNoMsg(EnumClass.ReplyNoCode code, EnumClass.DisplayLanguage lang, params string[] args)
        {
            ReturnMsg result = new ReturnMsg();

            if (false == Enum.IsDefined(typeof(EnumClass.ReplyNoCode), code) ||
                false == Enum.IsDefined(typeof(EnumClass.DisplayLanguage), lang))
            {
                return result;
            }

            if (EnumClass.ReplyNoCode.ReplyOK == code)
            {
                result.isSuccess = true;
            }
            else
            {
                result.isSuccess = false;
            }

            result.ReplyNo = GetFormattedReplyNo(code);
            //result.StatusMessage = GetReplyNoMsg(code, lang, args);
            result.StatusMessage = code.EnumToCode();

            return result;
        }
    }
}
