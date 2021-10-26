using MailDispatcher.Storage;
using NeuroSpeech.Eternity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{
    public class TemporaryMailboxWorkflow : Workflow<TemporaryMailboxWorkflow, string, string>
    {

        public const string Delete = nameof(Delete);
        private string Address;

        public override async Task<string> RunAsync(string input)
        {
            this.Address = input;

            await CreateMailboxAsync();

            await WaitForExternalEventsAsync(TimeSpan.FromDays(1), Delete);

            await DeleteAll();

            return Address;
        }

        [Activity]
        public virtual async Task CreateMailboxAsync([Inject] MailboxService mailboxService = null)
        {
            await mailboxService.GetAsync(Address, true);
        }

        [Activity]
        public virtual async Task DeleteAll(
            [Inject] MailboxService mailboxService = null
            )
        {
            await mailboxService.DeleteAsync(Address);
        }
    }
}
