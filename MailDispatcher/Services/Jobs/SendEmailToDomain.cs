#nullable enable
using MailDispatcher.Storage;
using NeuroSpeech.Workflows;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{
    public class Notification
    {
        public string? Url { get; set; }
        public string? Error { get; set; }

        public DateTime Sent { get; set; }
    }

    [Workflow]
    public class SendEmailToDomain: Workflow<DomainJob, JobResponse>
    {
        public async override Task<JobResponse> RunTask(DomainJob input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                var (sent, code, error) =  await SendEmailAsync(input, i);
                if (sent)
                {
                    if (error == null)
                    {
                        return new JobResponse { 
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

                    // send bounce notice...
                    var n = await context!.CreateSubOrchestrationInstance<Notification[]>(
                        typeof(BounceWorkflow), new BounceNotification { 
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
            }
            return new JobResponse
            {
                Domain = input.Domain,
                Error = sb.ToString()
            };
        }

        [Activity]
        public virtual Task<(bool sent, string code, string error)> SendEmailAsync(
            DomainJob input,
            int i,
            [Inject] SmtpService? smtpService = null)
        {
            return smtpService!.SendAsync(input);
        }
    }
}
