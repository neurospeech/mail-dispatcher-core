using MailDispatcher.Core.Auth;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MailDispatcher.Controllers
{
    [AllowPublic]
    [Route("api/auth")]
    public class AuthController : Controller
    {

        public class LoginModel
        {
            public string Username { get; set; }

            public string Password { get; set; }


        }

        [HttpPut("login")]
        public async Task<object> Login(
            [FromServices] AccountRepository accountRepository,
            [FromBody] LoginModel model
            )
        {
            var user = await accountRepository.GetAsync(model.Username);
            if (user == null && model.Username == "admin")
            {
                user = await accountRepository.SaveAsync(new Account { 
                    ID = "admin",
                    Password = "mail-dispatcher"
                });
            }

            if (user.Password != model.Password)
                return Unauthorized();

            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            claimsIdentity.AddClaim(new Claim(claimsIdentity.NameClaimType, "admin"));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { 
                    IsPersistent = true,
                    AllowRefresh = true
                }
                );

            return "ok";
        }

    }
}
