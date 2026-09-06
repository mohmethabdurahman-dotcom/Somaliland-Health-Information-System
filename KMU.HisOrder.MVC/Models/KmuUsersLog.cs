using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    /// <summary>
    /// Account change log(帳號基本檔修改紀錄表2023.03.03)
    /// </summary>
    public partial class KmuUsersLog
    {
        public string UserIdno { get; set; }
        public string EventType { get; set; }
        public bool IsSuccess { get; set; }
        public string EventErrorInput { get; set; }
        public string Message { get; set; }
        public DateTime EventTime { get; set; }
        public string Ip { get; set; }
        public string EventLoggingUser { get; set; }
    }
}
