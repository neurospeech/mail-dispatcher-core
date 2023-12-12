using MailDispatcher.Config;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using MimeKit;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{

    [DIRegister(ServiceLifetime.Singleton)]
    public class SmtpService
    {
        private readonly HttpClient httpClient;
        private readonly DnsLookupService lookupService;
        private readonly TelemetryClient telemetryClient;
        private readonly AccountService accountService;
        private readonly AzureStorage storage;
        private readonly IMemoryCache cache;
        private readonly string localHost;

        public SmtpService(
            DnsLookupService lookupService, 
            TelemetryClient telemetryClient,
            SmtpConfig smtpConfig,
            AccountService accountService,
            AzureStorage storage,
            IMemoryCache cache)
        {
            this.httpClient = new HttpClient();
            this.lookupService = lookupService;
            this.telemetryClient = telemetryClient;
            this.accountService = accountService;
            this.storage = storage;
            this.cache = cache;
            this.localHost = smtpConfig.Host;
        }

        internal async Task<SendResponse> SendAsync(DomainJob domainJob, CancellationToken token = default)
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
                var msg = await DownloadMesssageAsync(message, token);
                var sender = MailboxAddress.Parse(message.From);
                var replyTo = msg.ReplyTo?.OfType<MailboxAddress>()?.FirstOrDefault()?.Address;
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    msg.Date = now;
                    if (string.IsNullOrWhiteSpace(msg.MessageId)) {
                        msg.MessageId = $"{message.RowKey}@{localHost}";
                    }
                    msg.Headers.Add(HeaderId.ReturnPath, System.Text.Encoding.UTF8, $"{account.ID}-{message.RowKey}@{localHost}");
                    if (!msg.ReplyTo.Any())
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
                    await client.SendAsync(msg,
                        sender,
                        addresses.Select(x => MailboxAddress.Parse(x.ToString())), token);
                    return (true, null, null);
                }
                catch (SmtpCommandException ex)
                {
                    telemetryClient.TrackException(ex);

                    switch (ex.StatusCode)
                    {
                        case SmtpStatusCode.ServiceNotAvailable:
                        case SmtpStatusCode.MailboxBusy:
                        case SmtpStatusCode.TransactionFailed:
                        case SmtpStatusCode.ErrorInProcessing:
                        case SmtpStatusCode.ExceededStorageAllocation:
                        case SmtpStatusCode.InsufficientStorage:
                        case SmtpStatusCode.TemporaryAuthenticationFailure:
                            return (false, ex.ErrorCode.ToString(), ex.ToString());
                    }

                    switch (ex.ErrorCode)
                    {
                        case SmtpErrorCode.MessageNotAccepted:
                        case SmtpErrorCode.SenderNotAccepted:
                        case SmtpErrorCode.RecipientNotAccepted:
                            // send email..
                            if (replyTo != null)
                            {
                                try
                                {
                                    // send SMTP Error back to sender...

                                    var replyAddress = MailboxAddress.Parse(replyTo);

                                    var failed = new MimeMessage();
                                    failed.From.Add(sender);
                                    failed.Headers.Add(HeaderId.OriginalFrom, ex.Mailbox.Address);
                                    failed.To.Add(replyAddress);
                                    failed.Subject = "Mail Delivery Failed to " + ex.Mailbox.Address;
                                    failed.Date = DateTime.UtcNow;
                                    failed.InReplyTo = msg.MessageId;
                                    var bodyBuilder = new BodyBuilder();
                                    bodyBuilder.TextBody = "Mail Delivery Failed to " + ex.Mailbox.Address + "\r\n"
                                        + ex.ToString();

                                    failed.Body = bodyBuilder.ToMessageBody();

                                    var sc = await NewClient(replyAddress.Address.Split('@').Last());
                                    await sc.smtpClient.SendAsync(failed, sender, new MailboxAddress[] { replyAddress });

                                }
                                catch (Exception ex2)
                                {
                                    telemetryClient.TrackException(ex2);
                                }

                            }


                            return (true, ex.ErrorCode.ToString(), ex.ToString());
                    }

                    return (true, ex.StatusCode.ToString(), ex.ToString());
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    return (true, "Unknown", ex.ToString());
                }

            }
        }

        private async Task<MimeMessage> DownloadMesssageAsync(Job message, CancellationToken token)
        {
            Exception last = null;
            var blobClient = storage.MailBlobs.GetBlobClient(message.BlobPath);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var streaming = await blobClient.DownloadStreamingAsync();
                    using var value = streaming.Value;
                    using var stream = value.Content;
                    return await MimeKit.MimeMessage.LoadAsync(stream, token);
                }
                catch (Exception ex)
                {
                    last = ex;
                }
                await Task.Delay(1000);
            }
            throw (last ?? new InvalidOperationException());
        }

        internal async Task<Notification> NotifyAsync(string url, string postBody)
        {
            var postContent = new StringContent(postBody, System.Text.Encoding.UTF8, "application/json");
            var body = new HttpRequestMessage(HttpMethod.Post, url);
            body.Content = postContent;
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

        public class HostPort
        {
            public string Host { get; set; }
            public int Port { get; set; }

            public SecureSocketOptions SecureSocketOptions { get; set; }
        }

        internal async Task<(SmtpClient smtpClient, string error)> NewClient(string domain)
        {
            var client = new SmtpClient();
            client.LocalDomain = localHost;
            client.Timeout = 15000;
            client.CheckCertificateRevocation = false;
            client.SslProtocols = SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
            client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            var mxes = await lookupService.LookupMXAsync(domain);

            var mxKey = $"Smtp-MX:{domain}";

            if(cache.TryGetValue<HostPort>(mxKey, out var hp))
            {
                if (hp.Host != null)
                {
                    await client.ConnectAsync(hp.Host, hp.Port, hp.SecureSocketOptions);
                    return (client, null);
                }
                return (null, $"Could not connect to any MX host on {domain}");
            }

            foreach (var mx in mxes)
            {
                if (string.IsNullOrWhiteSpace(mx))
                    continue;
                try
                {
                    await client.ConnectAsync(mx, 25, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 25, SecureSocketOptions = SecureSocketOptions.StartTlsWhenAvailable }, TimeSpan.FromMinutes(15));
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(new Exception($"Unable to connect {domain} at {mx}:25\r\n{ex.Message}", ex));
                }
            }

            foreach (var mx in mxes)
            {
                if (string.IsNullOrWhiteSpace(mx))
                    continue;
                try
                {
                    await client.ConnectAsync(mx, 465, SecureSocketOptions.SslOnConnect);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 465, SecureSocketOptions = SecureSocketOptions.SslOnConnect }, TimeSpan.FromMinutes(15));
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(new Exception($"Unable to connect {domain} at {mx}:465\r\n{ex.Message}", ex));
                }
            }

            foreach (var mx in mxes)
            {
                if (string.IsNullOrWhiteSpace(mx))
                    continue;
                try
                {
                    await client.ConnectAsync(mx, 587, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 587, SecureSocketOptions = SecureSocketOptions.SslOnConnect}, TimeSpan.FromMinutes(15));
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(new Exception($"Unable to connect {domain} at {mx}:587\r\n{ex.Message}", ex));
                }

            }

            cache.Set(mxKey, new { }, TimeSpan.FromMinutes(15));

            return (null, $"Could not connect to any MX host on {domain}");
        }
    }
}
