using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class HashService
    {
        private readonly SHA256 hash;

        public HashService()
        {
            this.hash = SHA256.Create();
        }

        public string Hash(string userName, string password)
        {
            var key = $"{userName}-{password}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(key);
            var h = hash.ComputeHash(bytes);
            return h.ToHexString();
        }

    }
}
