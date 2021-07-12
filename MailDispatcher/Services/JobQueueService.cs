using Azure.Storage.Blobs;
using MailDispatcher.Services;
using MailDispatcher.Services.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{

    [DIRegister(ServiceLifetime.Singleton)]
    public class JobQueueService
    {
        private readonly WorkflowService workflowService;
        private readonly BlobContainerClient blobs;

        public JobQueueService(
            WorkflowService workflowService,
            AccountService accountRepository,
            AzureStorage storage)
        {
            this.workflowService = workflowService;
            this.blobs = storage.BlobServiceClient.GetBlobContainerClient("mails");
            this.blobs.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        }

        public Task DeleteAsync(string id)
        {
            return blobs.GetBlobClient(id).DeleteIfExistsAsync();
        }

        public async Task<string> Queue(
            string accountId,
            string from,
            EmailAddress[] recipients,
            Stream file)
        {

            // verify...

            var id = $"{Guid.NewGuid().ToHexString()}-{DateTime.UtcNow.Ticks}";
            string blobPath = id + ".eml";
            var blob = blobs.GetBlobClient(blobPath);

            await blob.UploadAsync(file);

            var job = new Job { 
                AccountID = accountId,
                From = from,
                BlobPath = blobPath,
                Recipients = recipients,
                MessageBodyUrl = blob.Uri.ToString()
            };

            var wid = await SendEmailWorkflow.CreateAsync(workflowService, job);
            workflowService?.Trigger();
            return wid;
        }

    }
}
