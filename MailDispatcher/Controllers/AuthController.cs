using MailDispatcher.Core.Auth;
using MailDispatcher.Services;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

        /// <summary>
        /// Logout
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.SignOutAsync();
            return Ok();
        }

        /// <summary>
        /// Check if you are logged in
        /// </summary>
        /// <returns></returns>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">You are logged in</response>
        [HttpGet]
        public object Get()
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();
            return new { ID = User.Identity.Name };
        }

        public class PasswordModel
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
            public string NewPasswordAgain { get; set; }
        }


        /// <summary>
        /// Change Password
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="hashService"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("password"), AuthorizeAdmin]
        public async Task<IActionResult> Post(
            [FromServices] AccountService repository,
            [FromServices] HashService hashService,
            [FromBody] PasswordModel model)
        {
            var a = await repository.GetAsync(User.Identity.Name);
            if (a.Password != hashService.Hash(a.ID, model.OldPassword))
                return Unauthorized();
            if (model.NewPassword != model.NewPasswordAgain)
                return BadRequest("Both passwords do not match");
            if (model.OldPassword == model.NewPassword)
                return BadRequest("New password cannot be same as old one");
            a.Password = hashService.Hash(a.ID, model.NewPassword);
            await repository.SaveAsync(a);
            return Ok();
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="accountRepository"></param>
        /// <param name="hashService"></param>
        /// <param name="configuration"></param>
        /// <param name="model"></param>
        /// <returns></returns>

        [HttpPut("login")]
        public async Task<object> Login(
            [FromServices] AccountService accountRepository,
            [FromServices] HashService hashService,
            [FromServices] IConfiguration configuration,
            [FromBody] LoginModel model
            )
        {
            var user = await accountRepository.GetAsync(model.Username);
            if (user == null)
            {
                if (model.Username == "admin")
                {
                    string defaultPassword = configuration.GetValue<string>("mail-dispatcher");
                    user = await accountRepository.SaveAsync(new Account
                    {
                        ID = "admin",
                        Active = true,
                        Password = hashService.Hash("admin", defaultPassword)
                    });
                }
            }

            if (user.Password != hashService.Hash(user.ID, model.Password))
                return Unauthorized("Password mismatch");

            if (!user.Active)
                return Unauthorized("Account is not active");

            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ID));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, user.ID));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Administrator"));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { 
                    IsPersistent = true,
                    AllowRefresh = true
                }
                );

            return new { ID = model.Username };
        }

    }
}
