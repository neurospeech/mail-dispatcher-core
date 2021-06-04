﻿using MailDispatcher.Config;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using MailKit.Net.Smtp;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly MemoryCache cache;
        private readonly string localHost;

        public SmtpService(
            DnsLookupService lookupService, 
            TelemetryClient telemetryClient,
            SmtpConfig smtpConfig,
            AccountService accountService,
            MemoryCache cache)
        {
            this.httpClient = new HttpClient();
            this.lookupService = lookupService;
            this.telemetryClient = telemetryClient;
            this.accountService = accountService;
            this.cache = cache;
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
                    msg.Headers.Add(HeaderId.ReturnPath, System.Text.Encoding.UTF8, $"{account.ID}-{message.RowKey}@{localHost}");
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
                    await client.SendAsync(msg, 
                        MailboxAddress.Parse(message.From), 
                        addresses.Select(x => MailboxAddress.Parse(x.ToString())), token);
                    return (true, null, null);
                } catch (SmtpCommandException ex) {
                    telemetryClient.TrackException(ex);
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
                    telemetryClient.TrackException(ex);
                    return (true, "Unknown", ex.ToString());
                }

            }
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
                await client.ConnectAsync(hp.Host, hp.Port);
                return (client, null);
            }

            foreach (var mx in mxes)
            {
                try
                {
                    await client.ConnectAsync(mx, 25, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 25 }, TimeSpan.FromHours(5));
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }

                try
                {
                    await client.ConnectAsync(mx, 587, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 587 }, TimeSpan.FromHours(5));
                    return (client, null);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }

                try
                {
                    await client.ConnectAsync(mx, 465, true);
                    cache.Set(mxKey, new HostPort { Host = mx, Port = 465 }, TimeSpan.FromHours(5));
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
