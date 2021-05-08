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
        public string Url { get; set; }
        public string Error { get; set; }

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
                    error = sb.ToString() + "\r\n" + error;
                    if (error == null)
                    {
                        return new JobResponse { 
                            Sent = context.CurrentUtcDateTime,
                            Domain = input.Domain
                        };
                    }

                    var postBody = JsonSerializer.Serialize(new
                    {
                        domain = input.Domain,
                        addresses = input.Addresses,
                        id = input.Job.RowKey,
                        code,
                        error
                    });

                    // send bounce notice...
                    var n = await SendBounceNoticeAsync(input, postBody);

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
        public async virtual Task<Notification[]> SendBounceNoticeAsync(
            DomainJob input, 
            string error,
            [Inject] AccountService accountService = null,
            [Inject] SmtpService smtpService = null)
        {
            var acc = await accountService.GetAsync(input.Job.AccountID);

            if (string.IsNullOrWhiteSpace(acc.BounceTriggers))
                return null;

            return await smtpService.NotifyAsync(acc.BounceTriggers, error, input);
        }

        [Activity]
        public virtual Task<(bool sent, string code, string error)> SendEmailAsync(
            DomainJob input,
            int i,
            [Inject] SmtpService smtpService = null)
        {
            return smtpService.SendAsync(input);
        }
    }
}
