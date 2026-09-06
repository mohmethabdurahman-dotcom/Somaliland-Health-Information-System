using KMU.HisOrder.MVC.Extesion;
using System.Runtime.Serialization;

namespace KMU.HisOrder.MVC.Models
{
    public class BaseContract
    {
    }

    public partial class ReturnMsg
    {
        private bool _isSuccess;
        /// <summary>
        /// 是否成功
        /// </summary>
        [DataMember]
        public bool isSuccess
        {
            get { return _isSuccess; }
            set { _isSuccess = value; }
        }

        private string? _ReplyNo;
        /// <summary>
        /// 回傳代碼	CHAR(2) || "00" 或 空白=>表示作業成功
        /// </summary>
        [DataMember]
        public string? ReplyNo
        {
            get { return _ReplyNo.NullToString(); }
            set { _ReplyNo = value; }
        }

        private string? _StatusMessage;
        /// <summary>
        /// 錯誤訊息或提示訊息	CHAR(120)
        /// </summary>
        [DataMember]
        public string? StatusMessage
        {
            get { return _StatusMessage.NullToString(); }
            set { _StatusMessage = value; }
        }
    }


    public partial class EmployeeList
    {
        private string? _UserID;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? UserID
        {
            get { return _UserID.NullToString(); }
            set { _UserID = value; }
        }

        private string? _UserName;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? UserName
        {
            get { return _UserName.NullToString(); }
            set { _UserName = value; }
        }

        private string? _Category;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? Category
        {
            get { return _Category.NullToString(); }
            set { _Category = value; }
        }
    }

    public partial class DepartmentList
    {
        private string? _DptCode;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? DptCode
        {
            get { return _DptCode.NullToString(); }
            set { _DptCode = value; }
        }

        private string? _DptName;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? DptName
        {
            get { return _DptName.NullToString(); }
            set { _DptName = value; }
        }

        private string? _DptCategory;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string? DptCategory
        {
            get { return _DptCategory.NullToString(); }
            set { _DptCategory = value; }
        }
    }
}
