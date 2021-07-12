#nullable enable
using MailDispatcher.Storage;
using Microsoft.ApplicationInsights;
using NeuroSpeech.Eternity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{


    public class SendEmailWorkflow : Workflow<SendEmailWorkflow, Job, JobResponse[]>
    {
        public async override Task<JobResponse[]> RunAsync(Job job)
        {
            job.RowKey = this.ID;
            var list = job.Recipients
                .GroupBy(x => x.Domain)
                .Select(x => new DomainJob(job, x))
                .ToList();

            var r = new List<JobResponse>();
            foreach(var d in list)
            {
                r.Add(await SendEmailAsync(d));
            }

            await DeleteEmailAsync(job.BlobPath);

            return r.ToArray();
        }


        public async Task<JobResponse> SendEmailAsync(DomainJob input)
        {
            StringBuilder sb = new StringBuilder();
            var after = TimeSpan.FromMilliseconds(1);
            for (int i = 0; i < 3; i++)
            {
                var (sent, code, error) = await SendEmailActivityAsync(after, input, i);
                if (sent)
                {
                    if (error == null)
                    {
                        return new JobResponse
                        {
                            Sent = this.CurrentUtc.UtcDateTime,
                            Domain = input.Domain
                        };
                    }

                    error = sb.ToString() + "\r\n" + error;

                    var postBody = JsonSerializer.Serialize(new
                    {
                        domain = input.Domain,
                        addresses = input.Addresses,
                        id = input.Job.RowKey,
                        code,
                        error
                    });

                    var n = await ReportBouncesAsync(new BounceNotification { 
                        AccountID = input.Job.AccountID,
                        Error = postBody
                    });

                    return new JobResponse
                    {
                        Domain = input.Domain,
                        Error = error,
                        ErrorCode = code,
                        Notifications = n
                    };
                }
                sb.AppendLine(code);
                sb.AppendLine(error);

                after = TimeSpan.FromMinutes(15);
            }
            return new JobResponse
            {
                Domain = input.Domain,
                Error = sb.ToString()
            };
        }

        private async Task<Notification[]?> ReportBouncesAsync(BounceNotification input)
        {

            var accountID = input.AccountID;
            string? error = input.Error;

            if (accountID == null || error == null)
                return null;

            var urls = await GetUrlsAsync(accountID);

            var tasks = urls.Select(x => NotifyAsync(x, error))
                .ToList();

            return await Task.WhenAll(tasks);
        }

        [Activity]
        public virtual async Task<string[]> GetUrlsAsync(string accountID,
            [Inject] AccountService? accountService = null)
        {
            var acc = await accountService!.GetAsync(accountID);
            if (acc.BounceTriggers == null)
                return new string[] { };

            return acc.BounceTriggers
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }

        [Activity]
        public virtual Task<Notification> NotifyAsync(
            string error,
            string url,
            [Inject] SmtpService? smtpService = null)
        {
            return smtpService!.NotifyAsync(url, error);
        }


        [Activity]
        public virtual async Task<(bool sent, string code, string error)> SendEmailActivityAsync(
            [Schedule]
            TimeSpan ts,
            DomainJob input,
            int i,
            [Inject] SmtpService smtpService = null!,
            [Inject] TelemetryClient telemetryClient = null!)
        {
            var start = DateTimeOffset.UtcNow;
            var r = await smtpService.SendAsync(input);
            if(r.sent && r.error == null)
            {
                var end = DateTimeOffset.UtcNow - start;
                telemetryClient.TrackRequest("MailSent", start, end, "200", true);
            }
            return r;
        }

        [Activity]
        public virtual async Task<string> DeleteEmailAsync(string blobPath, [Inject] JobQueueService? jobService = null)
        {
            await jobService!.DeleteAsync(blobPath);
            return "ok";
        }


    }
}
