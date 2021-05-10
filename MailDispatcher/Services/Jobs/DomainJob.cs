using MailDispatcher.Storage;
using System.Collections.Generic;
using System.Linq;

namespace MailDispatcher.Services.Jobs
{
    public class DomainJob
    {
        public Job Job { get; set; }
        public string Domain { get; set; }
        public List<EmailAddress> Addresses { get; set; }

        public DomainJob()
        {

        }

        public DomainJob(Job job, IGrouping<string, EmailAddress> addresses)
        {
            this.Job = job;
            this.Domain = addresses.Key ;
            this.Addresses = addresses.ToList();
        }
    }
}
