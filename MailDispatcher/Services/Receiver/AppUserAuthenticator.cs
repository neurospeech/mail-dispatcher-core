using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Receiver
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class AppUserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        private static Task<bool> False = Task.FromResult(false);

        public AppUserAuthenticator()
        {
        }

        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            return False;
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return this;
        }

    }
}

