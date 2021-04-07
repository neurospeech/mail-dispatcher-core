using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class BounceService
    {
        private readonly JobRepository jobs;
        private readonly BlobContainerClient bounced;
        private readonly QueueClient bounces;

        public BounceService(
            JobRepository jobs,
            AzureStorage storge
            )
        {
            this.jobs = jobs;
            this.bounced = storge.BlobServiceClient.GetBlobContainerClient("bounced");
            this.bounces = storge.QueueClientService.GetQueueClient("bounces");
            this.bounces.CreateIfNotExists();
        }

        internal async Task SendAsync(string id, MemoryStream ms)
        {
            var blob = bounced.GetBlobClient(id + ".eml");
            await blob.UploadAsync(ms);
        }
    }
}
