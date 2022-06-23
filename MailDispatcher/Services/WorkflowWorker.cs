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
//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            Task.Run(async () => {
//                try {
//                    // await workflowService.Storage.DeleteOrphanActivities();
//                    await workflowService.Storage.DeleteOldWorkflows(30);
//                }
//                catch (Exception ex)
//                {
//                    telemetryClient.TrackException(ex);
//                }
//            });
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            while (true)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    telemetryClient.TrackException(new TaskCanceledException("Background Service Cancelled"));
                    break;
                }
                telemetryClient.TrackPageView("Processing");
                try
                {
                    await workflowService.ProcessMessagesAsync(cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}
