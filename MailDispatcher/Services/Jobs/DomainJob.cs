using MailDispatcher.Storage;
using System.Collections.Generic;
using System.Linq;

namespace MailDispatcher.Services.Jobs
{
    public class DomainJob
    {
        public Job Job { get; set; }
        public string Domain { get; set; }
        public List<string> Addresses { get; set; }

        public DomainJob()
        {

        }

        public DomainJob(Job job, IGrouping<string, (string domain, string address)> addresses)
        {
            this.Job = job;
            this.Domain = addresses.Key ;
            this.Addresses = addresses.Select(x => x.address).ToList();
        }
    }
}
