using Microsoft.ApplicationInsights;
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
        private readonly TelemetryClient telemetryClient;

        public CleanupService(WorkflowService workflowService, TelemetryClient telemetryClient)
        {
            this.workflowService = workflowService;
            this.telemetryClient = telemetryClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            telemetryClient.TrackPageView("Cleanup-Started");
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await workflowService.CleanupAsync(stoppingToken);
                } catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
    }
}
