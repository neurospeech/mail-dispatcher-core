using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    public class WorkflowWorker : BackgroundService
    {
        private readonly WorkflowService workflowService;

        public WorkflowWorker(WorkflowService workflowService)
        {
            this.workflowService = workflowService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try {
                    await workflowService.ProcessMessagesOnceAsync();
                } catch
                {}
                try
                {
                    await Task.Delay(15000, workflowService.WaitToken);
                }catch (TaskCanceledException)
                {

                }
            }
        }
    }
}
