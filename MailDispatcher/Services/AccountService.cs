using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{

    [DIRegister(ServiceLifetime.Singleton)]
    public class AccountService: CachedTableRepository<Account> {
        public AccountService(AzureStorage storage, AppCache<Account> cache): base(storage, cache)
        {

        }

        public override async Task<Account> UpdateAsync(string rowKey, Func<Account, Account> update)
        {

            var a = await base.UpdateAsync(rowKey, update);
            a.DkimSigner = null;
            return a;
        }
    }

}
