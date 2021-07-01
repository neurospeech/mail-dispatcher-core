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
        private readonly WorkflowService workflowService;
        private readonly CertificateService certificateService;

        public SmtpReceiver(
            ILogger<SmtpReceiver> logger,
            AppMessageStore messageStore,
            AppMailboxFilter mailboxFilter,
            AppUserAuthenticator userAuthenticator,
            WorkflowService workflowService,
            CertificateService certificateService)
        {
            _logger = logger;
            this.messageStore = messageStore;
            this.mailboxFilter = mailboxFilter;
            this.userAuthenticator = userAuthenticator;
            this.workflowService = workflowService;
            this.certificateService = certificateService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await workflowService.RunAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var options = new SmtpServerOptionsBuilder()
                        .ServerName("localhost")
                        .Port(25)
                        .Port(465, isSecure: true)
                        .Port(587, isSecure: true)
                        .Certificate(CreateX509Certificate2())
                        .MessageStore(messageStore)
                        .MailboxFilter(mailboxFilter)
                        .UserAuthenticator(userAuthenticator)
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

            await workflowService.StopAsync();
        }
        X509Certificate CreateX509Certificate2()
        {
            return certificateService.BuildSelfSignedServerCertificate();
        }

    }
}
