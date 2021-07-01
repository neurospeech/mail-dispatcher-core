using System;
using MailDispatcher.Storage;
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
        private readonly AccountService accountRepository;

        public AppUserAuthenticator(AccountService accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        public async Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            var a = await accountRepository.GetAsync(user);
            if (a == null)
                return false;
            if (a.AuthKey != password)
                return false;
            return true;
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return this;
        }

    }
}

