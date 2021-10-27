using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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


    [DIRegister(ServiceLifetime.Singleton)]
    public class MailboxService
    {
        private readonly BlobContainerClient mailboxes;
        private readonly AppCache<Mailbox> cache;

        public MailboxService(AzureStorage storage, AppCache<Mailbox> cache)
        {
            this.mailboxes = storage.BlobServiceClient.GetBlobContainerClient("mailboxes");
            this.mailboxes.CreateIfNotExists();
            this.cache = cache;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            return (await GetAsync(name)).Exists;
        }

        public Task<Mailbox> GetAsync(string name, bool create = false)
        {
            if (name.StartsWith('@'))
            {
                name = Guid.NewGuid().ToString("N") + name;
            }
            return cache.GetOrCreateAsync(name, async (c) =>
            {
                var client = mailboxes.GetBlobClient($"{name}/config.json");
                var exists = await client.ExistsAsync();
                if (exists)
                {
                    c.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    return new Mailbox(name, mailboxes, true);
                }
                if (create)
                {
                    await client.UploadAsync(new BinaryData("{}"));
                    c.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    return new Mailbox(name, mailboxes, true);
                }
                c.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return new Mailbox(name, mailboxes, false);
            });
        }

        public async Task DeleteAsync(string name)
        {
            cache.Remove(name);
            await foreach(var item in mailboxes.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.All, Azure.Storage.Blobs.Models.BlobStates.All, name + "/")) {
                await mailboxes.DeleteBlobIfExistsAsync(item.Name);
            }
            await foreach (var item in mailboxes.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.All, Azure.Storage.Blobs.Models.BlobStates.All, name))
            {
                await mailboxes.DeleteBlobIfExistsAsync(item.Name);
            }
        }

    }

    public class Mailbox
    {
        public readonly string Name;
        public BlobContainerClient Container;
        public readonly bool Exists;

        public Mailbox(string name, BlobContainerClient container, bool exists)
        {
            this.Name = name;
            this.Container = container;
            this.Exists = exists;
        }

        public async Task<string> Save(Stream data, CancellationToken cancellationToken = default) {
            // get next available id...
            var id = $"{Name}/mails/{long.MaxValue - DateTime.UtcNow.Ticks:d20}-{Guid.NewGuid():N}.eml";
            var blob = Container.GetBlobClient(id);
            await blob.UploadAsync(data, cancellationToken);
            return id;
        }

        public Task<Stream> ReadAsync(string id, CancellationToken cancellationToken = default)
        {
            var blob = Container.GetBlobClient(Name + "/mails/" + id);
            return blob.OpenReadAsync(new Azure.Storage.Blobs.Models.BlobOpenReadOptions(false), cancellationToken);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var blob = Container.GetBlobClient(Name + "/mails/" + id);
            return blob.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }

        public async Task<List<Mail>> ListAsync(string next = null, int max = 100, CancellationToken cancellationToken = default)
        {
            var prefix = Name + "/mails/";
            var pages = Container.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.All, Azure.Storage.Blobs.Models.BlobStates.All, prefix, cancellationToken)
                .AsPages(next, max);
            var list = new List<Mail>();
            await foreach (var page in pages)
            {
                foreach (var blob in page.Values)
                {
                    var mail = Container.GetBlobClient(blob.Name);
                    using var stream = await mail.OpenReadAsync(new Azure.Storage.Blobs.Models.BlobOpenReadOptions(false), cancellationToken);
                    var msg = await MimeMessage.LoadAsync(stream, cancellationToken);
                    list.Add(new Mail
                    {
                        Subject = msg.Subject,
                        HtmlBody = msg.HtmlBody,
                        TextBody = msg.TextBody,
                        Date = msg.Date,
                        From = msg.From.OfType<MailboxAddress>().Select(x => new { x.Name, x.Address }).FirstOrDefault(),
                        To = msg.To.OfType<MailboxAddress>().Select(x => new { x.Name, x.Address }),
                        Cc = msg.Cc.OfType<MailboxAddress>().Select(x => new { x.Name, x.Address })
                    });
                }
                return list;
            }
            return list;
        }
    }

    public class Mail
    {
        public string Subject { get; internal set; }
        public string HtmlBody { get; internal set; }
        public string TextBody { get; internal set; }
        public DateTimeOffset Date { get; internal set; }
        public object From { get; internal set; }
        public object To { get; internal set; }
        public object Cc { get; internal set; }
    }

}
