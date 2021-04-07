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
            MimeMessage msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(model.From));
            List<string> recipients 
                = new List<string>(
                    (model.To?.Length ?? 0) +
                    (model.Cc?.Length ?? 0) +
                    (model.Bcc?.Length ?? 0));
            if (recipients.Capacity == 0)
                return BadRequest("Need at least one recipient");
            foreach(var to in model.To) {
                msg.To.Add(MailboxAddress.Parse(to));
                recipients.Add(to);
            }
            if (model.Cc != null) {
                foreach (var to in model.Cc) {
                    msg.Cc.Add(MailboxAddress.Parse(to));
                    recipients.Add(to);
                }
            }
            if (model.Bcc != null) {
                foreach (var to in model.Bcc) {
                    msg.Bcc.Add(MailboxAddress.Parse(to));
                    recipients.Add(to);
                }
            }
            var ms = new MemoryStream();
            msg.WriteTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var r = await jobs.Queue(id, new RawMessageRequest { 
                From = model.From,
                Recipients = recipients.ToArray()
            }, ms);
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
            [FromBody] RawMessageRequest model
            )
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest();

            var r = await jobs.Queue(id, model, file.OpenReadStream());
            return Ok(new { 
                id = r
            });
        }

    }
}
