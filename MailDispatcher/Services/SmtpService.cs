using MailDispatcher.Config;
using MailDispatcher.Services.Jobs;
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
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{

    public class SmtpResponse
    {
        public bool Sent { get; set; }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class SmtpService
    {
        private readonly HttpClient httpClient;
        private readonly DnsLookupService lookupService;
        private readonly TelemetryClient telemetryClient;
        private readonly AccountService accountService;
        private readonly string localHost;

        public SmtpService(
            DnsLookupService lookupService, 
            TelemetryClient telemetryClient,
            SmtpConfig smtpConfig,
            AccountService accountService)
        {
            this.httpClient = new HttpClient();
            this.lookupService = lookupService;
            this.telemetryClient = telemetryClient;
            this.accountService = accountService;
            this.localHost = smtpConfig.Host;
        }

        internal async Task<(bool sent, string code, string error)> SendAsync(DomainJob domainJob, CancellationToken token = default)
        {
            var domain = domainJob.Domain;
            var message = domainJob.Job;

            var account = await accountService.GetAsync(message.AccountID);

            var addresses = domainJob.Addresses;
            var (client, error) = await NewClient(domain);
            if (error != null)
                return (false, "ConnectivityError", error);

            using (client)
            {
                var msg = await MimeKit.MimeMessage.LoadAsync(await httpClient.GetStreamAsync(message.MessageBodyUrl), token);
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
                    account.DkimSigner.Sign(msg, new HeaderId[] {
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

        internal async Task<Notification[]> NotifyAsync(string bounceTriggers, string postBody, DomainJob job)
        {
            var postContent = new StringContent(postBody, System.Text.Encoding.UTF8, "application/json");


            return await Task.WhenAll(bounceTriggers.Split('\n')
                .Select(x => x.Trim())
                .Select(x => SendNotification(x, postContent))
                .ToList());


            async Task<Notification> SendNotification(string url, StringContent postBody)
            {
                var body = new HttpRequestMessage(HttpMethod.Post, url);
                body.Content = postBody;
                using (var s = await httpClient.SendAsync(body, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (s.IsSuccessStatusCode)
                        return new Notification { 
                            Url = url, 
                            Sent = DateTime.UtcNow 
                        };
                    var error = await s.Content.ReadAsStringAsync();
                    return new Notification { 
                        Url = url,
                        Sent = DateTime.UtcNow,
                        Error = error.Length > 1024 ? error.Substring(0, 1024) : error
                    };
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
