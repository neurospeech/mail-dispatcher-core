using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Config
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class SmtpConfig
    {
        public readonly string Host;

        public SmtpConfig(IConfiguration configuration)
        {
            this.Host = configuration.GetSection("Smtp").GetValue<string>("Domain");
        }

    }
}
