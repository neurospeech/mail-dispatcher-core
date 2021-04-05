using Heijden.DNS;
using MailDispatcher.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    public class DispatchService: BackgroundService
    {
        private readonly JobStorage jobs;
        private readonly TelemetryClient telemetry;
        private readonly JobResponseRepository responseRepository;

        public DispatchService(JobStorage jobs,TelemetryClient telemetry, JobResponseRepository responseRepository)
        {
            this.jobs = jobs;
            this.telemetry = telemetry;
            this.responseRepository = responseRepository;
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
            await Task.WhenAll(jobs.Select(x => SendEmailAsync(x, stoppingToken)));
        }

        Resolver dns = new Resolver();

        private async Task SendEmailAsync(Job message, CancellationToken token)
        {
            byte[] data = null;
            using (var ms = new MemoryStream())
            {
                await message.Message.DownloadToAsync(ms, token);
                data = ms.ToArray();
            }

            var rlist = JsonConvert.DeserializeObject<string[]>(message.Recipients)
                .Select(x => (tokens: x.ToLower().Split('@'),address: x))
                .Where(x => x.tokens.Length > 1)
                .Select(x => ( account: x.tokens.First(), domain: x.tokens.Last(), address: x.address ))
                .GroupBy(x => x.domain)
                .ToList();


            var r = await Task.WhenAll(rlist.Select(x => SendEmailAsync(message, x.Key, x.ToList(), token) ));

            if (r.Any(x => !x))
                return;

            await jobs.RemoveAsync(message);
            
        }

        private async Task<bool> SendEmailAsync(
            Job message, 
            string domain, 
            List<(string account, string domain, string address)> list, 
            CancellationToken token)
        {
            var response = await responseRepository.GetAsync(message.RowKey + "/" + domain);
            if (response.Sent != null)
                return true;

            return false;
        }
    }
}
