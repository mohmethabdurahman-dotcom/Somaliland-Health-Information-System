using System.Security.Claims;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using Microsoft.AspNetCore.Http;

namespace KMU.HisOrder.MVC
{
    public static class LoginSessionHelper
    {
        public const string SessionKey = "LoginDTO";

        public static LoginDTO GetOrRestore(HttpContext http)
        {
            if (http?.Session == null)
                return null;

            var login = http.Session.GetObject<LoginDTO>(SessionKey);
            if (login != null && !string.IsNullOrWhiteSpace(login.EMPCODE))
                return login;

            var userId = http.Session.GetString("user_idno")
                ?? http.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = http.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            login = new LoginDTO
            {
                EMPCODE = userId,
                EMPNAME = string.IsNullOrWhiteSpace(userName) ? userId : userName
            };
            http.Session.SetObject(SessionKey, login);
            http.Session.SetString("user_idno", userId);
            return login;
        }

        public static bool IsAjax(HttpRequest request)
        {
            if (request == null)
                return false;

            return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
