using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using MailDispatcher.Services;
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
        private readonly CloudTable Identity;
        private readonly AccountService accountRepository;
        private readonly JobRepository repository;
        private readonly BlobContainerClient blobs;
        private readonly QueueClient queue;

        public JobQueueService(
            AccountService accountRepository,
            AzureStorage storage,
            JobRepository repository)
        {
            this.Identity = storage.CloudTableClient.GetTableReference("Identity");
            this.Identity.CreateIfNotExists();
            this.accountRepository = accountRepository;
            this.repository = repository;
            this.blobs = storage.BlobServiceClient.GetBlobContainerClient("mails");
            this.blobs.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

            this.queue = storage.QueueClientService.GetQueueClient("pending");
            this.queue.CreateIfNotExists();
        }

        public async Task<string> Queue(
            string accountId,
            string from,
            IEnumerable<string> recipients,
            Stream file)
        {

            var nid = await Identity.GenerateSequenceAsync();
            var id = $"{Guid.NewGuid().ToHexString()}-{nid}";
            var blob = blobs.GetBlobClient(id);
            if (file != null)
            {
                await blob.UploadAsync(file);
            }

            var body = new Job() {
                AccountID = accountId,
                From = from,
                Recipients = JsonSerializer.Serialize(recipients),
                RowKey = id.ToString(),
                Url = blob.Uri.ToString(),
                Status = "Queued"
            };


            var qid = await queue.SendMessageAsync(id, TimeSpan.FromMilliseconds(500));
            body.QueueID = qid.Value.MessageId;
            await repository.SaveAsync(body);
            DispatchService.Signal();

            return id;

        }

        //public async Task RemoveAsync(Job job)
        //{
        //    await job.Message.DeleteIfExistsAsync();
        //    await queue.DeleteMessageAsync(job.QueueID, job.PopReceipt);
        //}

        public async Task UpdateAsync(Job job)
        {
            bool completed = false;
            if(!job.Responses.Any(y => y.Sent == null))
            {
                job.Status = "Completed";
                completed = true;
            }
            await repository.SaveAsync(job);
            
            if (completed)
            {
                await job.Message.DeleteIfExistsAsync();
                await queue.DeleteMessageAsync(job.QueueID, job.PopReceipt);
            }
        }


        public async Task<Job[]> DequeueAsync(CancellationToken stoppingToken)
        {
            var items = await queue.ReceiveMessagesAsync(16, TimeSpan.FromMinutes(5), stoppingToken);
            var tasks = items.Value.Select(async x => {
                var jobID = x.Body.ToString();
                var msg = await repository.GetAsync(jobID);
                msg.Message = blobs.GetBlobClient(jobID);
                msg.Account = await accountRepository.GetAsync(msg.AccountID);
                msg.PopReceipt = x.PopReceipt;
                return msg;
            });
            return await Task.WhenAll(tasks);
        }

    }
}
