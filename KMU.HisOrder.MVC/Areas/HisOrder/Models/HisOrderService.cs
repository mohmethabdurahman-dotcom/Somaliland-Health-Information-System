using KMU.HisOrder.MVC.Areas.HisOrder.Controllers;
using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Models
{
    public class HisOrderService : IDisposable
    {
        private readonly KMUContext _context;

        public void Dispose() { }

        public HisOrderService(KMUContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 2023.06.20 add by 1100079
        /// 中文：透過 InhospId 取得診別資訊
        /// English：Get ClinicDto by InhospId
        /// </summary>
        /// <param name="inHospID"></param>
        /// <returns>ClinicDTO</returns>
        public ClinicDTO getClinicDto(string inHospID)
        {
            ClinicDTO dto = new ClinicDTO();

            try
            {
                if (inHospID.StartsWith("IN"))
                {
                    InpatientReservation result = _context.InpatientReservations.Where(c => c.inhospId == inHospID).FirstOrDefault();
                    KmuDepartment result2 = _context.KmuDepartments.Where(c => c.DptCode.Trim() == result.department).FirstOrDefault();

                    if (result != null)
                    {
                        dto.RegDate = result.reserveDate;
                        dto.DeptCode = result.department;
                        dto.DeptName = result2.DptName;
                        dto.NoonNO = result.reserveDate.ToString("tt"); 
                        dto.DoctorCode = string.IsNullOrWhiteSpace(result.department) ? "" : result.department.Trim();
                        dto.DoctorName = string.IsNullOrWhiteSpace(dto.DoctorCode) ? "" : _context.KmuUsers.Where(c => c.UserIdno.Trim() == dto.DoctorCode.Trim()).Select(c => c.UserNameFirstname + " " + c.UserNameMidname + " " + c.UserNameLastname).FirstOrDefault();
                        dto.RoomNumber = result.bedId;
                        dto.InhospType = "WARD";
                    }
                }
                else
                {
                    Registration result = _context.Registrations.Where(c => c.Inhospid == inHospID).FirstOrDefault();
                    KmuDepartment result2 = _context.KmuDepartments.Where(c => c.DptCode.Trim() == result.RegDepartment).FirstOrDefault();

                    if (result != null)
                    {
                        dto.RegDate = result.RegDate.ToDateTime(TimeOnly.Parse("00:00AM"));
                        dto.DeptCode = result.RegDepartment;
                        dto.DeptName = result2.DptName;
                        dto.NoonNO = result.RegNoon;
                        dto.DoctorCode = string.IsNullOrWhiteSpace(result.RegDoctorId) ? "" : result.RegDoctorId.Trim();
                        dto.DoctorName = string.IsNullOrWhiteSpace(dto.DoctorCode) ? "" : _context.KmuUsers.Where(c => c.UserIdno.Trim() == dto.DoctorCode.Trim()).Select(c => c.UserNameFirstname + " " + c.UserNameMidname + " " + c.UserNameLastname).FirstOrDefault();
                        dto.RoomNumber = result.RegRoomNo;
                        dto.InhospType = result2.DptCategory;
                    }
                }

              
            }
            catch (Exception ex)
            {
                dto = null;
            }

            return dto;
        }


        /// <summary>
        /// 2023.06.20 add by 1100079
        /// 中文：透過 InhospId 取得全域變數 GlobalVariableDTO
        /// English：Get GlobalVariableDTO by InhospId    
        /// </summary>
        /// <param name="inHospID"></param>
        /// <returns>GlobalVariableDTO</returns>
        public GlobalVariableDTO getGlobalVariableDTO(string inHospID, HttpContext inHttpContext)
        {
            GlobalVariableDTO grv = new GlobalVariableDTO();

            // 這一塊再拆出來 直接呼叫 function
            using (HisOrderController col = new HisOrderController(_context))
            {
                string PatientId;
                if (inHospID.StartsWith("IN"))
                {
                    PatientId = _context.InpatientReservations.Where(c => c.inhospId == inHospID).Select(c => c.healthId).FirstOrDefault();
                }
                else
                {
                    PatientId = _context.Registrations.Where(c => c.Inhospid == inHospID).Select(c => c.RegHealthId).FirstOrDefault();
                }

                if (inHttpContext.Session.GetObject<LoginDTO>("LoginDTO") != null)
                {
                    grv.Login = inHttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                }

                grv.Clinic = getClinicDto(inHospID);
                grv.Patient = col.GetPatientInfo(inHospID, PatientId);
            }

            return grv;
        }
    }
}
