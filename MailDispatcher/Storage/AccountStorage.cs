using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using MimeKit.Cryptography;
using System;
using System.IO;
using System.Threading.Tasks;

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

        public string Selector { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }

        public string Password { get; set; }

        private DkimSigner signer;
        [IgnoreProperty]
        public DkimSigner DkimSigner
        {
            get
            {
                if (signer != null)
                    return signer;
                var ms = new MemoryStream(Convert.FromBase64String(PrivateKey));
                signer = new DkimSigner(ms, DomainName, Selector);
                return signer;
            }
            set
            {
                signer = null;
            }
        }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class AccountRepository: CachedTableRepository<Account> {
        public AccountRepository(AzureStorage storage, AppCache<Account> cache): base(storage, cache)
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
