using MailDispatcher.Storage;
using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class SmtpService
    {
        internal Task<(bool sent, string error)> SendAsync(Job message, List<string> addresses)
        {
            
            throw new NotImplementedException();
        }

        internal async Task<SmtpClient> NewClient(string domain)
        {

        }
    }
}
