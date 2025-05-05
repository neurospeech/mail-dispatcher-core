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

        public readonly string WorkflowTable;

        public readonly string Storage;

        public SmtpConfig(IConfiguration configuration)
        {

            var config = configuration.GetSection("Smtp");

            this.Host = config.GetValue<string>("Domain");
            this.WorkflowTable = config.GetValue<string>("WorkflowTable");
            this.Storage = config.GetValue<string>("Storage");
        }

    }
}
