using MailDispatcher.Config;
using MailDispatcher.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Receiver
{

    public class SmtpReceiver : BackgroundService
    {
        private readonly ILogger<SmtpReceiver> _logger;
        private readonly AppMessageStore messageStore;
        private readonly AppMailboxFilter mailboxFilter;
        private readonly AppUserAuthenticator userAuthenticator;

        public SmtpReceiver(
            ILogger<SmtpReceiver> logger,
            AppMessageStore messageStore,
            AppMailboxFilter mailboxFilter,
            AppUserAuthenticator userAuthenticator)
        {
            _logger = logger;
            this.messageStore = messageStore;
            this.mailboxFilter = mailboxFilter;
            this.userAuthenticator = userAuthenticator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var options = new SmtpServerOptionsBuilder()
                        .ServerName("localhost")
                        .Port(25)
                        // .Port(465, isSecure: true)
                        // .Certificate(CreateX509Certificate2())
                        .UserAuthenticator(userAuthenticator)
                        .MessageStore(messageStore)
                        .MailboxFilter(mailboxFilter)
                        .Logger(new AppLogger(_logger))
                        .Build();

                    var smtpServer = new SmtpServer.SmtpServer(options);
                    await smtpServer.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error");
                    break;
                }
            }
        }
        X509Certificate CreateX509Certificate2()
        {
            _logger.LogInformation($"Opening Certificates");
            var now = DateTime.UtcNow;
            using (X509Store store = new X509Store("WebHosting", StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var existing in store.Certificates)
                {
                    _logger.LogInformation($"{existing}");
                    if (existing.NotAfter <= now) continue;
                    if (existing.PrivateKey == null) continue;
                    var names = GetNames(existing);
                    _logger.LogTrace("Cert Names: " + string.Join(", ", names));
                    if (names.Any(x => x.Equals("*.neurospeech.com")))
                    {
                        return existing;
                    }
                }
            }

            throw new InvalidOperationException("No certificate found...");
        }
        IEnumerable<string> GetNames(X509Certificate2 certificate)
        {
            System.Security.Cryptography.X509Certificates.X509Extension uccSan = certificate.Extensions["2.5.29.17"];
            if (uccSan != null)
            {
                foreach (string nvp in uccSan.Format(true).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = nvp.Split('=');
                    string name = parts[0];
                    string value = (parts.Length > 0) ? parts[1] : null;
                    yield return value;
                }
            }
        }
    }
}
