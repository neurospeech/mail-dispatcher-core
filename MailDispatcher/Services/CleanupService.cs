using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    public class CleanupService : BackgroundService
    {
        private readonly WorkflowService workflowService;

        public CleanupService(WorkflowService workflowService)
        {
            this.workflowService = workflowService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await workflowService.CleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
    }
}
