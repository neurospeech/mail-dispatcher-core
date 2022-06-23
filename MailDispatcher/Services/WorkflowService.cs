#nullable enable
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
        public MailDispatcherEternityStorage(MailApp app, WorkflowClock clock)
            : base(app.DefaultConnection, clock, "MailEternityEntities", "Workflows")
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
            IServiceProvider services):
            base(services, clock, storage)
        {
            this.Storage = storage;
        }

    }
}
