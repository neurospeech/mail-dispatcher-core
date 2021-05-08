using MailDispatcher.Config;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Receiver
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class AppMailboxFilter : IMailboxFilter, IMailboxFilterFactory
    {

        private static readonly Task<MailboxFilterResult> Yes = Task.FromResult(MailboxFilterResult.Yes);

        private static readonly Task<MailboxFilterResult> SizeLimitExceeded = Task.FromResult(MailboxFilterResult.SizeLimitExceeded);
        private ILogger<SmtpReceiver> logger;
        private readonly WorkflowService workflowService;
        private readonly string host;

        public AppMailboxFilter(
            ILogger<SmtpReceiver> logger, 
            SmtpConfig config,
            WorkflowService workflowService)
        {
            this.logger = logger;
            this.workflowService = workflowService;
            this.host = config.Host;
        }

        public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            // abuse still pending..

            if (size > 1 * 1024 * 1024)
            {
                return SizeLimitExceeded;
            }
            return Yes;
        }

        public async Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            if (to.Host != host)
            {
                return MailboxFilterResult.NoTemporarily;
            }

            string f = to.User.Split('-').Last();
            var item = await workflowService.GetAsync(f);
            if (item == null)
                return MailboxFilterResult.NoTemporarily;
            return MailboxFilterResult.Yes;
        }

        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return this;
        }
    }
}
