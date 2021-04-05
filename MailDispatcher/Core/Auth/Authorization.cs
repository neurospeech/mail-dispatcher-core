using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NeuroSpeech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MailDispatcher.Core.Auth
{

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowPublicAttribute : TypeFilterAttribute
    {

        /// <summary>
        /// 
        /// </summary>
        public AllowPublicAttribute() : base(typeof(AllowAnonymousFilter))
        {
            this.Arguments = new object[] { "" };
        }

    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAdminAttribute : AuthorizeRoleAttribute
    {
        public AuthorizeAdminAttribute() : base("Administrator")
        {

        }
    }

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : TypeFilterAttribute
    {

        /// <summary>
        /// 
        /// </summary>
        public AuthorizeRoleAttribute(string role) : base(typeof(AllowAnonymousFilter))
        {
            this.Arguments = new object[] { role };
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class DisallowAnonymousFilter : IAsyncAuthorizationFilter
    {
        /// <summary>
        /// 
        /// </summary>
        public DisallowAnonymousFilter()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var isAuth = user.Identity.IsAuthenticated;

            foreach (var filter in context.Filters.OfType<AllowAnonymousFilter>())
            {
                if (string.IsNullOrWhiteSpace(filter.Role))
                {
                    return Task.CompletedTask;
                }

                if (!isAuth)
                {

                    var role = user.FindFirst(ClaimTypes.Role);
                    if (role != null && role.Value.EqualsIgnoreCase(filter.Role))
                    {
                        return Task.CompletedTask;
                    }
                }

            }
            if (!isAuth)
            {

                context.Result = new UnauthorizedResult();
            }
            return Task.CompletedTask;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class AllowAnonymousFilter : IAsyncAuthorizationFilter
    {

        public AllowAnonymousFilter(string role = null)
        {
            this.Role = role;
        }

        public string Role { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            // do nothing...

            return Task.CompletedTask;
        }
    }
}
