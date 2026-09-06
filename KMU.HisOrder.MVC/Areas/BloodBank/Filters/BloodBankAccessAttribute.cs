using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Filters
{
    /// <summary>
    /// Blood Bank access requires User Auth Setting roles:
    /// Blood Bank Admin (full) or Blood Bank Staff (all except Staff Audit).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BloodBankAccessAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
                return;

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            if (HasBloodBankAccess(user))
                return;

            context.Result = new RedirectToActionResult("NotAuth", "Login", new { area = "" });
        }

        internal static bool HasBloodBankAccess(ClaimsPrincipal user)
        {
            if (user == null || !user.Identity!.IsAuthenticated)
                return false;

            return HasBloodBankMenuAccess(user.Claims);
        }

        /// <summary>
        /// True when the user has Blood Bank Admin or Blood Bank Staff.
        /// </summary>
        internal static bool HasBloodBankMenuAccess(IEnumerable<Claim> claims)
        {
            if (claims == null)
                return false;

            var list = claims as IList<Claim> ?? claims.ToList();
            if (list.Count == 0)
                return false;

            return HasRole(list, BloodBankRoles.Admin) || HasRole(list, BloodBankRoles.Staff);
        }

        private static bool HasRole(IList<Claim> claims, string projectId)
        {
            var pageClaimType = "User_auth_page_Name「" + projectId + "」";
            return claims.Any(c =>
                (c.Type == ClaimTypes.Role && c.Value == projectId)
                || c.Type == pageClaimType);
        }
    }
}
