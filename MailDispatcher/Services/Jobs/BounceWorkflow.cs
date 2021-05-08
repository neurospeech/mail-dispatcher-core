using MailDispatcher.Storage;
using NeuroSpeech.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{
    public class BounceNotification
    {
        public string AccountID { get; set; }
        public string Error { get; set; }
    }

    [Workflow]
    public class BounceWorkflow : Workflow<BounceNotification, Notification[]>
    {
        public override async Task<Notification[]> RunTask(BounceNotification input)
        {
            var accountID = input.AccountID;
            string error = input.Error;

            var urls = await GetUrlsAsync(accountID);

            var tasks = urls.Select(x => NotifyAsync(x, error))
                .ToList();

            return await Task.WhenAll(tasks);
        }

        [Activity]
        public async Task<string[]> GetUrlsAsync(string accountID, [Inject] AccountService accountService = null)
        {
            var acc = await accountService.GetAsync(accountID);
            if (acc.BounceTriggers == null)
                return new string[] { };

            return acc.BounceTriggers
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }

        [Activity]
        public Task<Notification> NotifyAsync(
            string error, 
            string url,
            [Inject] SmtpService smtpService = null)
        {
            return smtpService.NotifyAsync(url, error);
        }
    }
}
