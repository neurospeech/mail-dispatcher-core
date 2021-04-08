using MailDispatcher.Config;
using MailDispatcher.Storage;
using MailKit.Net.Smtp;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class SmtpService
    {
        private readonly DnsLookupService lookupService;
        private readonly TelemetryClient telemetryClient;
        private readonly string localHost;

        public SmtpService(
            DnsLookupService lookupService, 
            TelemetryClient telemetryClient,
            SmtpConfig smtpConfig)
        {
            this.lookupService = lookupService;
            this.telemetryClient = telemetryClient;
            this.localHost = smtpConfig.Host;
        }

        internal async Task<(bool sent, string code, string error)> SendAsync(string domain, Job message, List<string> addresses, CancellationToken token)
        {
            var (client, error) = await NewClient(domain);
            if (error != null)
                return (false, "ConnectivityError", error);

            using (client)
            {
                var msg = await MimeKit.MimeMessage.LoadAsync(new MemoryStream(message.Data), token);
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    msg.Date = now;
                    msg.MessageId = $"{message.RowKey}@{localHost}";
                    msg.Headers.Add(HeaderId.ReturnPath, System.Text.Encoding.UTF8, $"{message.RowKey}@{localHost}");
                    if(!msg.ReplyTo.Any())
                    {
                        msg.ReplyTo.Add(msg.From.First());
                    }
                    message.Account.DkimSigner.Sign(msg, new HeaderId[] {
                        HeaderId.From,
                        HeaderId.Subject,
                        HeaderId.Date,
                        HeaderId.MessageId,
                        HeaderId.ReplyTo,
                        HeaderId.MimeVersion,
                        HeaderId.ContentType
                    });
                    await client.SendAsync(msg, MailboxAddress.Parse(message.From), addresses.Select(x => MailboxAddress.Parse(x)), token);
                    return (true, null, null);
                } catch (SmtpCommandException ex) {
                    switch(ex.ErrorCode)
                    {
                        case SmtpErrorCode.MessageNotAccepted:
                        case SmtpErrorCode.SenderNotAccepted:
                        case SmtpErrorCode.RecipientNotAccepted:
                            return (true, ex.ErrorCode.ToString(), ex.ToString());
                    }
                    return (true, ex.StatusCode.ToString(), ex.ToString());
                } catch (Exception ex)
                {
                    return (true, "Unknown", ex.ToString());
                }

            }
        }

        internal async Task<(SmtpClient smtpClient, string error)> NewClient(string domain)
        {
            var client = new SmtpClient();
            client.LocalDomain = localHost;
            client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            var mxes = await lookupService.LookupMXAsync(domain);

            foreach (var mx in mxes)
            {
                try
                {
                    await client.ConnectAsync(mx, 25);
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }

                try
                {
                    await client.ConnectAsync(mx, 587, true);
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }

            }

            return (null, $"Could not connect to any MX host on {domain}");
        }
    }
}
