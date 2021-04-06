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

            public string ID { get; internal set; }
            public string AuthKey { get; internal set; }
            public string DomainName { get; internal set; }
            public string Selector { get; internal set; }
            public string PublicKey { get; internal set; }
        }

        [HttpGet()]
        public async Task<IEnumerable<AccountInfo>> Get(
            [FromServices] AccountRepository repository
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
            public string ID { get; set; }

            public  string DomainName { get; set; }

            public string Selector { get; set; }
        }

        [HttpPost]
        public async Task<AccountInfo> Post(
            [FromServices] AccountRepository repository,
            [FromBody] PutBody model)
        {
            var a = await repository.GetAsync(model.ID);
            a.AuthKey = Guid.NewGuid().ToString();
            a = await repository.SaveAsync(a);
            return new AccountInfo(a);
        }
        

        [HttpPut()]
        public async Task<AccountInfo> Put(
            [FromServices] AccountRepository repository,
            [FromBody] PutBody model
        )
        {

            var rsa = System.Security.Cryptography.RSA.Create();

            var r = await repository.SaveAsync(new Account { 
                ID = model.ID,
                Selector = model.Selector,
                DomainName = model.DomainName,
                PublicKey = Convert.ToBase64String( rsa.ExportRSAPublicKey()),
                PrivateKey = Convert.ToBase64String( rsa.ExportPkcs8PrivateKey()),
                AuthKey = Guid.NewGuid().ToHexString()
            });

            return new AccountInfo(r);
        }

    }
}
