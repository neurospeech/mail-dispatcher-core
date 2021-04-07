using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MailDispatcher.Services.Receiver
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class AppLogger : SmtpServer.ILogger
    {
        readonly ILogger<SmtpReceiver> logger;
        public AppLogger(ILogger<SmtpReceiver> logger)
        {
            this.logger = logger;
        }
        public void LogVerbose(string format, object[] args)
        {
            logger.LogInformation(string.Format(format, args));
        }
    }
}

