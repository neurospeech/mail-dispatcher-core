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
            [FromServices] JobStorage jobs,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string auth,
            [FromBody] MessageRequest model
            )
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest();

            var r = await jobs.Queue(id, model, file);
            return Ok(new { 
                id = r
            });
        }

    }
}
