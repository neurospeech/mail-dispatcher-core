using MailDispatcher.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    public class DispatchService: BackgroundService
    {
        private static CancellationTokenSource WaitTokenSource = null;
        public static void Signal()
        {
            WaitTokenSource?.Cancel();
        }

        private readonly JobQueueService jobs;
        private readonly TelemetryClient telemetry;
        private readonly SmtpService smtpService;

        public DispatchService(
            JobQueueService jobs,
            TelemetryClient telemetry, 
            SmtpService smtpService)
        {
            this.jobs = jobs;
            this.telemetry = telemetry;
            this.smtpService = smtpService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendEmailsAsync(stoppingToken);
                } catch (Exception ex)
                {
                    telemetry.TrackException(ex);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        private async Task SendEmailsAsync(CancellationToken stoppingToken)
        {
            var jobs = await this.jobs.DequeueAsync(stoppingToken);
            if(jobs.Length == 0)
            {
                var c = new CancellationTokenSource();
                var old = WaitTokenSource;
                old?.Dispose();
                WaitTokenSource = c;
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), c.Token);
                } catch (TaskCanceledException) {

                }
                return;
            }
            await Task.WhenAll(jobs.Select(x => SendEmailAsync(x, stoppingToken)));
        }

        private async Task SendEmailAsync(Job message, CancellationToken token)
        {
            byte[] data = null;
            using (var ms = new MemoryStream())
            {
                await message.Message.DownloadToAsync(ms, token);
                ms.Position = 0;
                data = ms.ToArray();
                message.Data = data;
            }

            var rlist = JsonSerializer.Deserialize<string[]>(message.Recipients)
                .Select(x => (tokens: x.ToLower().Split('@'),address: x))
                .Where(x => x.tokens.Length > 1)
                .Select(x => ( domain: x.tokens.Last(), x.address ))
                .GroupBy(x => x.domain)
                .ToList();

            var domains = rlist.Count;
            if(message.Responses == null)
            {
                message.Responses = new JobResponse[domains];
                int i = 0;
                foreach(var domain in rlist)
                {
                    message.Responses[i++] = new JobResponse { Domain = domain.Key };
                }
            }

            var r = await Task.WhenAll(rlist.Select(x => SendEmailAsync(message, x.Key, x.Select(x => x.address).ToList(), token) ));

            message.Responses = r;

            await jobs.UpdateAsync(message);
            
        }



        private async Task<JobResponse> SendEmailAsync(
            Job message, 
            string domain, 
            List<string> addresses, 
            CancellationToken token)
        {
            var r = message.Responses.FirstOrDefault(x => x.Domain == domain);
            if(message.Locked != null)
            {
                if (message.Locked > DateTime.UtcNow)
                {
                    return r;
                }
            }
            if (message.Tries> 3)
            {
                r.Sent = DateTime.UtcNow;
                r.AppendError("Failed after 3 retries");
                return r;
            }
            var (sent, code, error) = await smtpService.SendAsync(domain, message, addresses, token);
            var now = DateTime.UtcNow;
            if(code != null)
            {
                r.ErrorCode = code;
            }
            if(error != null)
            {
                message.Locked = now.AddMinutes( (message.Tries + 1) * 15);
                r.AppendError(error);
            }
            if(!sent)
            {
                message.Tries++;
            } else
            {
                r.Sent = DateTime.UtcNow;
                r.Warning = error;
                r.Error = null;
            }
            return r;
        }
    }
}
