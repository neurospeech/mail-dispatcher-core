﻿using MailDispatcher.Services;
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
    public class MailboxController : Controller
    {

        [HttpPut("{?id}")]
        public async Task<IActionResult> Create(
            [FromRoute] string id,
            [FromServices] MailboxService mailboxService,
            [FromServices] AccountService accountRepository,
            [FromServices] WorkflowService workflowService,
            [FromHeader(Name = "x-id")] string accountID,
            [FromHeader(Name = "x-auth")] string authKey
            )
        {

            var a = await accountRepository.GetAsync(accountID);
            if (a.AuthKey != authKey)
                return Unauthorized();

            await TemporaryMailboxWorkflow.CreateAsync(workflowService, id);

            var r = await mailboxService.GetAsync(id, true);

            return Ok(new { 
                username = r.Name,
                address = $"{r.Name}@{a.DomainName}"
            });
        }

        [HttpGet("{id}")]
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

            if (mailId != null)
            {
                var s = await r.ReadAsync(mailId);
                this.Response.RegisterForDispose(s);
                return File(s, "application/octat-stream");
            }

            return Ok(await r.ListAsync(next, cancellationToken));
        }

    }
}