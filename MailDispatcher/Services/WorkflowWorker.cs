using Microsoft.ApplicationInsights;
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
        private readonly TelemetryClient telemetryClient;

        public WorkflowWorker(WorkflowService workflowService, TelemetryClient telemetryClient)
        {
            this.workflowService = workflowService;
            this.telemetryClient = telemetryClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(true)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    telemetryClient.TrackException(new TaskCanceledException("Background Service Cancelled"));
                    break;
                }
                telemetryClient.TrackPageView("Processing");
                try
                {
                    await workflowService.ProcessChunkedMessagesAsync(cancellationToken: stoppingToken);
                }catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                }
            }
        }
    }
}
