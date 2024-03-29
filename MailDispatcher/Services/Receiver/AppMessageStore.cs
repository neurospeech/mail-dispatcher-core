﻿using Azure.Storage.Blobs;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Cryptography;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Receiver
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class AppMessageStore : IMessageStore, IMessageStoreFactory
    {
        private ILogger<SmtpReceiver> logger;
        private readonly BounceService bounceService;
        private readonly JobQueueService jobs;
        private readonly TempFileService tempFileService;
        private readonly MailboxService mailboxService;
        DkimPublicKeyLocator publicKeyLocator;
        DkimVerifier verifier;

        public AppMessageStore(
            ILogger<SmtpReceiver> logger, 
            DnsLookupService dnsLookupService,
            BounceService bounceService,
            JobQueueService jobs,
            TempFileService tempFileService,
            MailboxService mailboxService)
        {
            this.logger = logger;
            this.bounceService = bounceService;
            this.jobs = jobs;
            this.tempFileService = tempFileService;
            this.mailboxService = mailboxService;
            publicKeyLocator = new DkimPublicKeyLocator(dnsLookupService);
            verifier = new DkimVerifier(publicKeyLocator);
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return this;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                var textMessage = (ITextMessage)transaction.Message;

                if (context.Authentication.IsAuthenticated)
                {
                    var user = context.Authentication.User;
                    await jobs.Queue(user,
                        null,
                        transaction.From.ToEmailAddress(),
                        transaction.To.Select(x => (EmailAddress)x.ToEmailAddress()).ToArray(), textMessage.Content);
                    return SmtpResponse.Ok;
                }

                // either it has to have an associated workflow
                // or it needs to have mailboxes enabled

                // check if we have any mailbox
                bool saved = false;
                var stream = await tempFileService.Create(textMessage.Content);
                foreach (var to in transaction.To)
                {
                    var mailbox = await mailboxService.GetAsync(to.ToEmailAddress());
                    if (mailbox.Exists)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        await mailbox.Save(stream, cancellationToken);
                        saved = true;
                    }
                }
                if (saved)
                {
                    return SmtpResponse.Ok;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var message = await MimeMessage.LoadAsync(stream, cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);
                var recipients = transaction.To.Select(x => x.ToEmailAddress()).ToList();


                var all = string.Join(",", recipients);

                try
                {
                    await VerifyAsync(message, transaction.From.ToEmailAddress(), all);
                }
                catch (Exception ve)
                {
                    logger.LogWarning(ve, "DKIM Fail");
                    message.Headers.Add("X-Spam-Error", ve.Message);
                }

                logger.LogTrace($"Storing at {all}");


                await bounceService.SendAsync(transaction.To.First(), stream);
                return SmtpResponse.Ok;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                return new SmtpResponse(SmtpReplyCode.Error, ex.Message);
            }
        }
        async Task VerifyAsync(MimeMessage message, string fromEmail, string recipients)
        {
            int index = message.Headers.IndexOf(HeaderId.DkimSignature);
            if (index == -1)
            {
                return;
            }
            Exception verificationError = null;
            string details = "";
            try
            {
                var v = await verifier.VerifyAsync(message, message.Headers[index]);
                if (v)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                verificationError = ex;
                details = $" Failure: {ex.Message}";
            }
            throw new Exception($"DKIM Verification Failed Message From {fromEmail} to {recipients} with Subject {message.Subject}{details}", verificationError);

        }
    }
}
