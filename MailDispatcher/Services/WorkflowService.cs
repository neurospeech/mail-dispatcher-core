#nullable enable
using MailDispatcher.Config;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech.Eternity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{

    [DIRegister(ServiceLifetime.Singleton)]
    public class WorkflowClock: EternityClock
    {

    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class MailDispatcherEternityStorage : NeuroSpeech.Eternity.SqlStorage.EternitySqlStorage
    {
        public MailDispatcherEternityStorage(
            MailApp app,
            WorkflowClock clock,
            SmtpConfig config
            )
            : base(app.DefaultConnection, clock, config.WorkflowTable, "Workflows")
        {
        }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class WorkflowService: EternityContext
    {
        public readonly MailDispatcherEternityStorage Storage;

        public WorkflowService(
            MailDispatcherEternityStorage storage,
            WorkflowClock clock,
            IServiceProvider services,
            WorkflowLogger logger):
            base(services, clock, storage, logger)
        {
            this.Storage = storage;
        }

    }
}
