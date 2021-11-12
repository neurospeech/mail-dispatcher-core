using MailDispatcher.Storage;
using NeuroSpeech.Eternity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{
    public class CreateMailboxRequest
    {
        public string Address;

        public int MaxDays;
    }

    public class TemporaryMailboxWorkflow : Workflow<TemporaryMailboxWorkflow, CreateMailboxRequest, string>
    {

        public const string Delete = nameof(Delete);
        private string Address;

        public override async Task<string> RunAsync(CreateMailboxRequest input)
        {
            this.Address = input.Address;

            await CreateMailboxAsync();

            await WaitForExternalEventsAsync(TimeSpan.FromDays(input.MaxDays), Delete);

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
