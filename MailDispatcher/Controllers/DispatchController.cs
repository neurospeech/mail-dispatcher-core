using MailDispatcher.Core.Auth;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Controllers
{


    [AllowPublic]
    [Route("api/queue")]
    public class DispatchController : Controller
    {
        [HttpPut]
        public async Task<IActionResult> Put(
            [FromServices] AccountRepository accountRepository,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string auth,
            [FromBody] MessageBody model
            )
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                return Unauthorized();

            Request.Form.Files.FirstOrDefault();

        }

    }
}
