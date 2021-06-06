﻿#nullable enable
using DurableTask.Core;
using MailDispatcher.Storage;
using Microsoft.ApplicationInsights;
using NeuroSpeech.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{

    public class EmailAddress
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Domain { get; set; }
        public string User { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static explicit operator EmailAddress (string address)
        {
            var tokens = address.Trim().Split('@');
            if (tokens.Length != 2)
                throw new ArgumentException($"Invalid email address {address}");
            return new EmailAddress
            {
                User = tokens[0],
                Domain = tokens.Last()
            };
        }

        public override string ToString()
        {
            return $"{User}@{Domain}";
        }

        private static char[] Separators = new char[] { ',',';' };

        public static EmailAddress[] ParseList(string addresses)
        {
            var tokens = addresses.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new EmailAddress[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = (EmailAddress)tokens[i];
            }
            return result;
        }
    }


    [Workflow]
    public class SendEmailWorkflow : Workflow<SendEmailWorkflow, Job, JobResponse[]>
    {
        public async override Task<JobResponse[]> RunTask(Job job)
        {
            job.RowKey = context!.OrchestrationInstance.InstanceId;
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
            for (int i = 0; i < 3; i++)
            {
                var (sent, code, error) = await SendEmailAsync(input, i);
                if (sent)
                {
                    if (error == null)
                    {
                        return new JobResponse
                        {
                            Sent = context!.CurrentUtcDateTime,
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

                    var n = await BounceWorkflow.RunInAsync(this, new BounceNotification { 
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

                await Delay(TimeSpan.FromMinutes(15));
            }
            return new JobResponse
            {
                Domain = input.Domain,
                Error = sb.ToString()
            };
        }


        [Activity]
        public virtual async Task<(bool sent, string code, string error)> SendEmailAsync(
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
