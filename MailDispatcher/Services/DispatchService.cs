using MailDispatcher.Storage;
using MailKit.Net.Smtp;
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
        private readonly SmtpService smtpService;

        public DispatchService(
            JobStorage jobs,
            TelemetryClient telemetry, 
            JobResponseRepository responseRepository,
            SmtpService smtpService)
        {
            this.jobs = jobs;
            this.telemetry = telemetry;
            this.responseRepository = responseRepository;
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

            var rlist = JsonConvert.DeserializeObject<string[]>(message.Recipients)
                .Select(x => (tokens: x.ToLower().Split('@'),address: x))
                .Where(x => x.tokens.Length > 1)
                .Select(x => ( domain: x.tokens.Last(), address: x.address ))
                .GroupBy(x => x.domain)
                .ToList();


            var r = await Task.WhenAll(rlist.Select(x => SendEmailAsync(message, x.Key, x.Select(x => x.address).ToList(), token) ));

            if (r.Any(x => !x))
                return;

            await jobs.RemoveAsync(message);
            
        }



        private async Task<bool> SendEmailAsync(
            Job message, 
            string domain, 
            List<string> addresses, 
            CancellationToken token)
        {
            if(message.Locked != null)
            {
                if (message.Locked > DateTime.UtcNow)
                    return false;
            }
            var response = await responseRepository.GetAsync(message.RowKey + "/" + domain, true);
            if (message.Tries> 3)
            {
                response.AppendError("Failed after 3 retries");
                response.Sent = DateTime.Now;
                return true;
            }
            if (response.Sent != null)
                return true;
            var (sent, error) = await smtpService.SendAsync(domain, message, addresses, token);
            var now = DateTime.UtcNow;
            if(error == null)
            {
                response.Sent = now;
            } else
            {
                message.Locked = now.AddMinutes( (message.Tries + 1) * 15);
                response.AppendError(error);
            }
            if(!sent)
            {
                message.Tries++;
            }
            await responseRepository.SaveAsync(response);
            return sent;
        }
    }
}
