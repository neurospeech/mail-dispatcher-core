using MailDispatcher.Core.Auth;
using MailDispatcher.Services;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NeuroSpeech.Eternity;
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
        /// <summary>
        /// Send email as Json or Form encoded body along with Attachments, Attachments can only work with form encoded mime type
        /// </summary>
        /// <param name="accountRepository"></param>
        /// <param name="jobs"></param>
        /// <param name="id"></param>
        /// <param name="authKey"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("simple")]
        public async Task<IActionResult> Simple(
            [FromServices] AccountService accountRepository,
            [FromServices] JobQueueService jobs,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string authKey,
            [FromBody] SimpleMail model)
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != authKey)
                return Unauthorized();
            var (ms, recipients) = model.ToMessage( Request.HasFormContentType ? Request.Form.Files : null);
            var r = await jobs.Queue(id, model.From, recipients, ms);
            return Ok(new
            {
                id = r
            });

        }

        /// <summary>
        /// Send single raw email in Mime format
        /// </summary>
        /// <param name="accountRepository"></param>
        /// <param name="jobs"></param>
        /// <param name="id"></param>
        /// <param name="authKey"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("raw")]
        public async Task<IActionResult> Put(
            [FromServices] AccountService accountRepository,
            [FromServices] JobQueueService jobs,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string authKey,
            [FromForm] RawMessageRequest model
            )
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != authKey)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest();
            if (model.From == null)
                return BadRequest("From is missing");
            if (model.Recipients == null)
                return BadRequest("Recipient is missing");
            var r = await jobs.Queue(id, model.From, EmailAddress.ParseList(model.Recipients), file.OpenReadStream());
            return Ok(new { 
                id = r
            });
        }

        /// <summary>
        /// Request Status of the job
        /// </summary>
        /// <param name="accountRepository"></param>
        /// <param name="workflowService"></param>
        /// <param name="id"></param>
        /// <param name="auth"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet("status/{jobId}")]
        public async Task<WorkflowStatus<JobResponse[]?>> Status(
            [FromServices] AccountService accountRepository,
            [FromServices] WorkflowService workflowService,
            [FromHeader(Name = "x-id")] string id,
            [FromHeader(Name = "x-auth")] string auth,
            [FromRoute] string jobId)
        {
            var a = await accountRepository.GetAsync(id);
            if (a.AuthKey != auth)
                throw new UnauthorizedAccessException();

            return await SendEmailWorkflow.GetStatusAsync(workflowService, jobId);

        }

    }
}
