using DurableTask.AzureStorage;
using DurableTask.Core;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class WorkflowService: BaseWorkflowService
    {
        private readonly AzureStorageOrchestrationService service;

        private readonly TaskHubWorker worker;
        private readonly Task initAsync;

        public WorkflowService(AzureStorage storage, IServiceProvider services)
        {

            DurableTask.AzureStorage.AzureStorageOrchestrationServiceSettings settings = new DurableTask.AzureStorage.AzureStorageOrchestrationServiceSettings()
            {
                StorageConnectionString = storage.ConnectionString,
                TaskHubName = "mailq1"
            };

            this.service = new DurableTask.AzureStorage.AzureStorageOrchestrationService(settings);

            this.client = new TaskHubClient(service);
            this.worker = new TaskHubWorker(service);
            worker.Register(services, typeof(WorkflowService).Assembly);

            this.initAsync = service.CreateIfNotExistsAsync();
        }



        public async Task<string> QueueTask<T>(object input = null)
        {
            await initAsync;
            var instance = await client.CreateOrchestrationInstanceAsync(typeof(T), input);
            return instance.InstanceId;
        }

        public async Task RaiseEvent<T>(string id, string name, object data)
        {
            var state = await client.GetOrchestrationStateAsync(id);
            await client.RaiseEventAsync(state.OrchestrationInstance, name, data);
        }

        internal async Task<string> GetAsync(string f)
        {
            try
            {
                return (await client.GetOrchestrationStateAsync(f)).OrchestrationInstance.InstanceId;
            } catch
            {
                return null;
            }
        }

        public async Task RunAsync()
        {
            await initAsync;
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            await client.PurgeOrchestrationInstanceHistoryAsync(lastWeek, OrchestrationStateTimeRangeFilterType.OrchestrationCompletedTimeFilter);

            await worker.StartAsync();
        }

        public async Task StopAsync()
        {
            await worker.StopAsync(true);
        }

    }
}
