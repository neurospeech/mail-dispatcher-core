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
    public class MailDispatcherEternityStorage : NeuroSpeech.Eternity.EternityAzureStorage
    {
        public MailDispatcherEternityStorage(AzureStorage azureStorage)
            : base(("md", azureStorage.ConnectionString, true))
        {
        }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class WorkflowService: EternityContext
    {
        public readonly MailDispatcherEternityStorage Storage;

        public WorkflowService(
            MailDispatcherEternityStorage storage,
            IServiceProvider services):
            base(storage, services, new EternityClock())
        {
            this.Storage = storage;
        }

    }
}
