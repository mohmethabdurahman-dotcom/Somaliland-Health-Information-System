using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    /// <summary>
    /// Account Basic File(user account )
    /// </summary>
    public partial class KmuUser
    {
        public KmuUser()
        {
            KmuAuths = new HashSet<KmuAuth>();
        }

        public string UserIdno { get; set; }
        public string UserPassword { get; set; }
        public string UserNameMidname { get; set; }
        public DateTime? UserBirthDate { get; set; }
        public string UserSex { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string UserMobilePhone { get; set; }
        public string UserEmail { get; set; }
        public string Creator { get; set; }
        public DateTime CreateTime { get; set; }
        public string UserNameFirstname { get; set; }
        public string UserNameLastname { get; set; }
        /// <summary>
        /// 分類(1:Doctor,2:Nurse,3.Staff
        /// </summary>
        public string UserCategory { get; set; }
        public string AccountStatus { get; set; }

        public virtual ICollection<KmuAuth> KmuAuths { get; set; }
    }
}
