#nullable enable
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech.Eternity;
using System.Diagnostics;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class WorkflowLogger : IEternityLogger
    {
        private readonly TelemetryClient telemetryClient;

        public WorkflowLogger(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public void Log(TraceEventType type, string text)
        {
            if (type == TraceEventType.Error)
            {
                var et = new ExceptionTelemetry(new ActivityFailedException(text));
                et.Context.Cloud.RoleName = "Workflows";
                telemetryClient.TrackException(et);
            }
            else if (type == TraceEventType.Information)
            {
                var et = new TraceTelemetry(text, SeverityLevel.Information);
                et.Context.Cloud.RoleName = "Workflows";
                telemetryClient.TrackTrace(et);

            }
        }
    }
}
