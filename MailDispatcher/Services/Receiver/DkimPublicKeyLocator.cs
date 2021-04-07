using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Receiver
{
    public class DkimPublicKeyLocator : DkimPublicKeyLocatorBase
    {
        readonly Dictionary<string, AsymmetricKeyParameter> cache;
        private readonly DnsLookupService dnsLookupService;

        public DkimPublicKeyLocator(DnsLookupService dnsLookupService)
        {
            cache = new Dictionary<string, AsymmetricKeyParameter>();
            this.dnsLookupService = dnsLookupService;
        }

        public override AsymmetricKeyParameter LocatePublicKey(string methods, string domain, string selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = selector + "._domainkey." + domain;
            AsymmetricKeyParameter pubkey;

            // checked if we've already fetched this key
            if (cache.TryGetValue(query, out pubkey))
                return pubkey;

            // make a DNS query
            var txt = dnsLookupService.LookupTXT(query);

            // DkimPublicKeyLocatorBase provides us with this helpful method.
            pubkey = GetPublicKey(txt);

            cache.Add(query, pubkey);

            return pubkey;
        }

        public override async Task<AsymmetricKeyParameter> LocatePublicKeyAsync(string methods, string domain, string selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = selector + "._domainkey." + domain;
            AsymmetricKeyParameter pubkey;

            // checked if we've already fetched this key
            if (cache.TryGetValue(query, out pubkey))
                return pubkey;

            // make a DNS query
            var txt = await dnsLookupService.LookupTXTAsync(query, cancellationToken);

            // DkimPublicKeyLocatorBase provides us with this helpful method.
            pubkey = GetPublicKey(txt);

            cache.Add(query, pubkey);

            return pubkey;
        }
    }
}
