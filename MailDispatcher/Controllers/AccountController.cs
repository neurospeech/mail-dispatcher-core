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

        [HttpGet()]
        public async Task<List<Account>> Get(
            [FromServices] AccountRepository repository
            )
        {
            var list = new List<Account>();
            await foreach(var item in repository.QueryAsync(null, this.HttpContext.RequestAborted))
            {
                list.AddRange(item);
            }
            return list;
        }

        public class PutBody
        {
            public string ID { get; set; }

            public  string DomainName { get; set; }
        }

        [HttpPut()]
        public async Task<Account> Put(
            [FromServices] AccountRepository repository,
            [FromBody] PutBody model
        )
        {

            var rsa = System.Security.Cryptography.RSA.Create();

            return await repository.SaveAsync(new Account { 
                ID = model.ID,
                DomainName = model.DomainName,
                PublicKey = Convert.ToBase64String( rsa.ExportRSAPublicKey()),
                PrivateKey = Convert.ToBase64String( rsa.ExportRSAPrivateKey()),
                AuthKey = Guid.NewGuid().ToString()
            });
        }

    }
}
