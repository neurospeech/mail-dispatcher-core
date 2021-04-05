﻿using MailDispatcher.Storage;
using MailKit.Net.Smtp;
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

        public SmtpService(DnsLookupService lookupService)
        {
            this.lookupService = lookupService;
        }

        internal async Task<(bool sent, string error)> SendAsync(string domain, Job message, List<string> addresses, CancellationToken token)
        {
            var (client, error) = await NewClient(domain);
            if (error != null)
                return (false, error);

            using (client)
            {
                var msg = await MimeKit.MimeMessage.LoadAsync(new MemoryStream(message.Data), token);
                try
                {
                    msg.Date = DateTimeOffset.UtcNow;
                    message.Account.DkimSigner.Sign(msg, new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date });

                    await client.SendAsync(msg, MailboxAddress.Parse(message.From), addresses.Select(x => MailboxAddress.Parse(x)), token);
                    return (true, null);
                } catch (Exception ex)
                {
                    return (true, ex.ToString());
                }

            }
        }

        internal async Task<(SmtpClient smtpClient, string error)> NewClient(string domain)
        {
            var client = new SmtpClient();

            var mxes = await lookupService.LookupAsync(domain);

            foreach (var mx in mxes)
            {
                try
                {
                    await client.ConnectAsync(mx, 587, true);
                    return (client, null);
                }
                catch
                {

                }

                try
                {
                    await client.ConnectAsync(mx, 25);
                    return (client, null);
                }
                catch
                {

                }
            }

            return (null, $"Could not connect to any MX host on {domain}");
        }
    }
}