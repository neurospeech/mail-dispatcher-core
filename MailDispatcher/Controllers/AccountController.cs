using MailDispatcher.Core.Auth;
using MailDispatcher.Storage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Controllers
{
    [Route("api/accounts")]
    public class AccountController: Controller
    {

        public class AccountInfo
        {

            public AccountInfo()
            {

            }

            public AccountInfo(Account account)
            {
                ID = account.ID;
                AuthKey = account.AuthKey;
                DomainName = account.DomainName;
                Selector = account.Selector;
                PublicKey = account.PublicKey;

            }

            /// <summary>
            /// Account ID
            /// </summary>
            public string ID { get; internal set; }

            /// <summary>
            /// AuthKey to use in Dispatch End Point
            /// </summary>
            public string AuthKey { get; internal set; }

            /// <summary>
            /// Domain name
            /// </summary>
            public string DomainName { get; internal set; }

            /// <summary>
            /// Selector for domain key
            /// </summary>
            public string Selector { get; internal set; }

            /// <summary>
            /// Public key for domain key
            /// </summary>
            public string PublicKey { get; internal set; }
        }

        /// <summary>
        /// Display list of accounts
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IEnumerable<AccountInfo>> Get(
            [FromServices] AccountService repository
            )
        {
            var list = new List<Account>();
            await foreach(var item in repository.QueryAsync(null, this.HttpContext.RequestAborted))
            {
                list.AddRange(item);
            }
            return list.Select(y => new AccountInfo(y));
        }

        public class PutBody
        {
            /// <summary>
            /// Unique alpha-numeric id only, no space or any other character
            /// </summary>
            public string ID { get; set; }

            /// <summary>
            /// Domain name used in `From` in email address
            /// </summary>
            public  string DomainName { get; set; }

            /// <summary>
            /// DomainKey selector, try to use same as ID
            /// </summary>
            public string Selector { get; set; }

            /// <summary>
            /// Optional, multiple http rest endpoints separated by new lines, where you will receive bounce notifications
            /// </summary>
            public string BounceTriggers { get; set; }
        }

        /// <summary>
        /// Create new account
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("new")]
        public async Task<AccountInfo> Put(
            [FromServices] AccountService repository,
            [FromBody] PutBody model
        )
        {

            var rsa = System.Security.Cryptography.RSA.Create();

            var r = await repository.SaveAsync(new Account { 
                ID = model.ID,
                Selector = model.Selector,
                DomainName = model.DomainName,
                PublicKey = rsa.ExportPemPublicKey(),
                PrivateKey = rsa.ExportPem(),
                AuthKey = Guid.NewGuid().ToHexString(),
                BounceTriggers = model.BounceTriggers
            });

            return new AccountInfo(r);
        }

    }
}
