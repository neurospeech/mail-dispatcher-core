using Azure.Storage.Blobs;
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
        DkimPublicKeyLocator publicKeyLocator;
        DkimVerifier verifier;

        public AppMessageStore(
            ILogger<SmtpReceiver> logger, 
            DnsLookupService dnsLookupService,
            BounceService bounceService)
        {
            this.logger = logger;
            this.bounceService = bounceService;
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

                var message = await MimeMessage.LoadAsync(textMessage.Content, cancellationToken);

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

                using (var ms = new MemoryStream())
                {

                    await message.WriteToAsync(ms, cancellationToken);

                    ms.Seek(0, SeekOrigin.Begin);

                    await bounceService.SendAsync(transaction.To.First().User, ms);

                    return SmtpResponse.Ok;
                }

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
