using DnsClient;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class DnsLookupService
    {
        private readonly LookupClient client;
        private readonly AppCache<LookupResult> cache;

        public class LookupResult
        {
            public string Domain { get; set; }
            public string[] MX { get; set; }
        }

        public DnsLookupService(AppCache<LookupResult> cache)
        {
            this.client = new LookupClient(
                new NameServer(System.Net.IPAddress.Parse("1.1.1.1")),
                new NameServer(System.Net.IPAddress.Parse("8.8.8.8"))
            );
            this.cache = cache;
        }

        public string LookupTXT(string domain)
        {
            var r = client.Query(domain, QueryType.TXT);
            StringBuilder sb = new StringBuilder();
            foreach (var item in r.Answers.TxtRecords())
            {
                sb.Append(item.Text);
            }
            return sb.ToString();
        }



        public async Task<string> LookupTXTAsync(string domain, CancellationToken cancellationToken)
        {
            var r = await client.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);
            StringBuilder sb = new StringBuilder();
            foreach(var item in r.Answers.TxtRecords())
            {
                sb.Append(item.Text);
            }
            return sb.ToString();
        }

        public async Task<string[]> LookupMXAsync(string domain)
        {
            // var lr = await cache.GetOrCreateAsync(domain, async x => {
            var r = await client.QueryAsync(domain, QueryType.MX);
            return r.Answers.MxRecords()
                .OrderBy(x => x.Preference)
                .Select(x => x.Exchange.Value).ToArray();

                //return new LookupResult
                //{
                //    Domain = domain,
                //    MX = r.Answers.MxRecords()
                //    .OrderBy(x => x.Preference)
                //    .Select(x => x.Exchange.Value).ToArray()
                //};
                // });
                // return lr.MX;
        }

    }
}
