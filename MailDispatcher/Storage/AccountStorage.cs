using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace MailDispatcher.Storage
{
    public class Account: TableEntity
    {

        [IgnoreProperty]
        public string ID {
            get => this.RowKey;
            set => this.RowKey = value;
        }
        public string AuthKey { get; set; }

        public string DomainName { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }

        public string Password { get; set; }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class AccountRepository: CachedTableRepository<Account> {
        public AccountRepository(AzureStorage storage, AppCache<Account> cache): base(storage, cache)
        {

        }
    }

}
