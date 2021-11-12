using MailDispatcher.Core.Auth;
using MailDispatcher.Services;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Controllers
{
    [Route("api/temp-mailboxes")]
    [AllowPublic]
    public class MailboxController : Controller
    {

        /// <summary>
        /// Create email address with given id, set it to only `@domainname` to generate username automatically.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mailboxService"></param>
        /// <param name="accountRepository"></param>
        /// <param name="workflowService"></param>
        /// <param name="accountID"></param>
        /// <param name="authKey"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create(
            [FromRoute] string id,
            [FromServices] MailboxService mailboxService,
            [FromServices] AccountService accountRepository,
            [FromServices] WorkflowService workflowService,
            [FromHeader(Name = "x-id")] string accountID,
            [FromHeader(Name = "x-auth")] string authKey,
            [FromQuery] int days = 1 
            )
        {

            var a = await accountRepository.GetAsync(accountID);
            if (a.AuthKey != authKey)
                return Unauthorized();
            if (!a.MailboxesEnabled)
            {
                return this.Conflict();
            }
            if (!id.EndsWith("@" + a.DomainName))
            {
                return BadRequest();
            }

            var r = await mailboxService.GetAsync(id, true);
            await TemporaryMailboxWorkflow.CreateAsync(workflowService, new NeuroSpeech.Eternity.WorkflowOptions<CreateMailboxRequest> {
                Input = new CreateMailboxRequest { 
                    Address = id,
                    MaxDays = days
                }
            });

            return Ok(new { 
                address = $"{r.Name}"
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> List(
            [FromRoute] string id,
            [FromQuery] string next,
            [FromServices] MailboxService mailboxService,
            [FromServices] AccountService accountRepository,
            [FromHeader(Name = "x-id")] string accountID,
            [FromHeader(Name = "x-auth")] string authKey,
            CancellationToken cancellationToken, 
            [FromQuery] int max = 100)
        {
            var a = await accountRepository.GetAsync(accountID);
            if (a.AuthKey != authKey)
                return Unauthorized();

            var r = await mailboxService.GetAsync(id);

            return Ok(await r.ListAsync(next, max, cancellationToken));
        }

        [HttpGet("{id}/{mailId}")]
        public async Task<IActionResult> Get(
            [FromRoute] string id,
            [FromRoute] string mailId,
            [FromQuery] string next,
            [FromServices] MailboxService mailboxService,
            [FromServices] AccountService accountRepository,
            [FromHeader(Name = "x-id")] string accountID,
            [FromHeader(Name = "x-auth")] string authKey,
            CancellationToken cancellationToken
            )
        {

            var a = await accountRepository.GetAsync(accountID);
            if (a.AuthKey != authKey)
                return Unauthorized();

            var r = await mailboxService.GetAsync(id);

            var s = await r.ReadAsync(mailId);
            this.Response.RegisterForDispose(s);
            return File(s, "application/octat-stream");
        }



        [HttpDelete("{id}/{mailId}")]
        public async Task<IActionResult> Delete(
            [FromRoute] string id,
            [FromRoute] string mailId,
            [FromServices] MailboxService mailboxService,
            [FromServices] AccountService accountRepository,
            [FromHeader(Name = "x-id")] string accountID,
            [FromHeader(Name = "x-auth")] string authKey,
            CancellationToken cancellationToken
            )
        {

            var a = await accountRepository.GetAsync(accountID);
            if (a.AuthKey != authKey)
                return Unauthorized();

            var r = await mailboxService.GetAsync(id);

            await r.DeleteAsync(mailId, cancellationToken);
            return Ok(new { });
        }



    }
}
