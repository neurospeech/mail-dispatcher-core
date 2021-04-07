using MailDispatcher.Core.Auth;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Controllers
{


    [AllowPublic]
    [Route("api/queue")]
    public class DispatchController : Controller
    {
        [HttpPut("simple")]
        public async Task<IActionResult> Simple(
            [FromServices] AccountService accountRepository,
            [FromServices] JobQueueService jobs,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string auth,
            [FromBody] SimpleMail model)
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                return Unauthorized();
            var (ms, recipients) = model.ToMessage(Request.Form.Files);
            var r = await jobs.Queue(id, model.From, recipients, ms);
            return Ok(new
            {
                id = r
            });

        }

        [HttpPut("raw")]
        public async Task<IActionResult> Put(
            [FromServices] AccountService accountRepository,
            [FromServices] JobQueueService jobs,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string auth,
            [FromForm] RawMessageRequest model
            )
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest();
            if (model.From == null)
                return BadRequest("From is missing");
            if (model.Recipients == null)
                return BadRequest("Recipient is missing");
            var recipients =
                    model.Recipients
                        .Split(',')
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0);
            var r = await jobs.Queue(id, model.From, recipients, file.OpenReadStream());
            return Ok(new { 
                id = r
            });
        }

    }
}
