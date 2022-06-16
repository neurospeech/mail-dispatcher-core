#nullable enable
using MailDispatcher.Core;
using MailDispatcher.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;
using NeuroSpeech.Eternity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{

    public class SendResponse
    {
        public bool Sent { get; set; }
        public string? Error { get; set; }

        public string? Code { get; set; }

        public static implicit operator SendResponse((bool sent, string? code, string? error) r)
        {
            return new SendResponse { 
                Sent = r.sent,
                Code = r.code,
                Error = r.error
            };
        }

        public void Deconstruct(out bool sent, out string? code, out string? error)
        {
            sent = this.Sent;
            code = this.Code;
            error = this.Error;
        }
    }

    public class SendEmailWorkflow : Workflow<SendEmailWorkflow, Job, JobResponse[]>
    {
        public static Regex throttleRegex = new Regex("(throttle|try|again|timed\\s+out|timeout)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);


        private string? mailPath;

        public async override Task<JobResponse[]> RunAsync(Job job)
        {
            this.PreserveTime = TimeSpan.FromMinutes(15);
            this.FailurePreserveTime = TimeSpan.FromDays(1);
            mailPath = job.BlobPath;
            job.RowKey = this.ID;
            var list = job.Recipients
                .GroupBy(x => x.Domain)
                .Select(x => new DomainJob(job, x))
                .ToList();

            var cache = this.Context.ResolveSingleton<StrongCache<ThrottleInfo>>();
            
            var r = new List<JobResponse>();
            foreach(var d in list)
            {
                if (cache.TryGetValue(d.Domain, out var ti))
                {
                    var now = DateTimeOffset.UtcNow;
                    if (ti.Expiration > now)
                    {
                        var diff = ti.Expiration - now;
                        if (diff.TotalMinutes < 5 && diff.TotalMilliseconds > 0)
                        {
                            await this.Delay(diff);
                        }
                    }
                }
                r.Add(await SendEmailAsync(d));
            }

            await DeleteEmailAsync(job.BlobPath);

            return r.ToArray();
        }

        protected override Task RunFinallyAsync()
        {
            return DeleteEmailAsync(mailPath);
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

                after = after.Add(TimeSpan.FromMinutes(30));
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
        public virtual async Task<SendResponse> SendEmailActivityAsync(
            [Schedule]
            TimeSpan ts,
            DomainJob input,
            int i,
            [Inject] StrongCache<ThrottleInfo> cache = null!,
            [Inject] SmtpService smtpService = null!,
            [Inject] TelemetryClient telemetryClient = null!)
        {
            var start = DateTimeOffset.UtcNow;
            var r = await smtpService.SendAsync(input);
            if (r.Sent && r.Error == null)
            {
                var end = DateTimeOffset.UtcNow - start;
                telemetryClient.TrackRequest("MailSent", start, end, "200", true);
                return r;
            }
            if (r.Error != null)
            {
                var key = input.Domain;
                if (throttleRegex.IsMatch(r.Error))
                {
                    var end = DateTimeOffset.UtcNow - start;
                    var t = new RequestTelemetry("Throttle", start, end, "300", true);
                    t.Url = new Uri($"https://{input.Domain}");
                    telemetryClient.TrackRequest(t);
                    if (cache.TryGetValue(key, out var ti))
                    {
                        ti.Increment();
                    }
                    else
                    {
                        ti = new ThrottleInfo(input.Domain);
                    }
                    cache.Set(key, ti, ti.Expiration);
                }
            }
            return r;
        }

        [Activity]
        public virtual async Task<string> DeleteEmailAsync(string? blobPath, [Inject] JobQueueService? jobService = null)
        {
            if (blobPath == null)
                return "ok";
            await jobService!.DeleteAsync(blobPath);
            return "ok";
        }


    }

    public class ThrottleInfo
    {
        public readonly string Domain;
        public DateTimeOffset Expiration { get; private set; }

        public ThrottleInfo(string domain)
        {
            this.Domain = domain;
            this.Expiration = DateTimeOffset.UtcNow.AddSeconds(15);
        }

        internal void Increment()
        {
            this.Expiration = this.Expiration.AddSeconds(5);
        }
    }
}
