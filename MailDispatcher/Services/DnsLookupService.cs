using DnsClient;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
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
            this.client = new LookupClient();
            this.cache = cache;
        }

        public async Task<string[]> LookupAsync(string domain)
        {
            var lr = await cache.GetOrCreateAsync(domain, async x => {
                var r = await client.QueryAsync(domain, QueryType.MX);
                return new LookupResult
                {
                    Domain = domain,
                    MX = r.Answers.MxRecords()
                    .OrderBy(x => x.Preference)
                    .Select(x => x.Exchange.Value).ToArray()
                };
            });
            return lr.MX;
        }

    }
}
