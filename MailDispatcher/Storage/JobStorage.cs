using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{

    public class MessageRequest
    {
        public string From { get; set; }

        public string[] Recipients { get; set; }

        public string Content { get; set; }
    }


    public class Job: TableEntity
    {
        public string AccountID { get; set; }

        public string From { get; set; }
        public string Recipients { get; set; }

        public string Url { get; set; }

        public DateTime? Locked { get; set; }

        public int Tries { get; set; }

        [IgnoreProperty]
        public BlobClient Message { get; set; }

        [IgnoreProperty]
        public Account Account { get; set; }

        [IgnoreProperty]
        public string PopReceipt { get; set; }

        [IgnoreProperty]
        public byte[] Data { get; internal set; }
        public string QueueID { get; set; }
    }
    
    [DIRegister(ServiceLifetime.Singleton)]
    public class JobRepository: TableRepository<Job>
    {
        public JobRepository(AzureStorage storage): base(storage)
        {

        }
    }

    public class JobResponse: TableEntity
    {

        public DateTime? Sent { get; set; }

        [IgnoreProperty]
        public bool Success => string.IsNullOrEmpty(Error) && Sent != null;

        public string Error { get; set; }

        internal void AppendError(string v)
        {
            if (Error == null)
            {
                Error = v;
                return;
            }
            Error += "\r\n" + v;
        }
    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class JobResponseRepository: TableRepository<JobResponse>
    {
        public JobResponseRepository(AzureStorage storage): base(storage)
        {

        }
    }


    [DIRegister(ServiceLifetime.Singleton)]
    public class JobStorage
    {
        private readonly CloudTable Identity;
        private readonly AccountRepository accountRepository;
        private readonly JobRepository repository;
        private readonly BlobContainerClient blobs;
        private readonly QueueClient queue;

        public JobStorage(
            AccountRepository accountRepository,
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
            MessageRequest message,
            IFormFile file)
        {

            var nid = await Identity.GenerateSequenceAsync();
            var id = $"{Guid.NewGuid().ToHexString()}-{nid}.eml";
            var blob = blobs.GetBlobClient(id);
            if (file != null)
            {
                await blob.UploadAsync(file.OpenReadStream());
            } else
            {
                await blob.UploadAsync(new MemoryStream( System.Text.Encoding.UTF8.GetBytes( message.Content) ));
            }

            var body = new Job() {
                AccountID = accountId,
                RowKey = id.ToString(),
                From = message.From,
                Recipients = JsonConvert.SerializeObject(message.Recipients),
                Url = blob.Uri.ToString()
            };


            var qid = await queue.SendMessageAsync(id, TimeSpan.FromMilliseconds(500));
            body.QueueID = qid.Value.MessageId;
            await repository.SaveAsync(body);
            return id;

        }

        public async Task RemoveAsync(Job job)
        {
            await queue.DeleteMessageAsync(job.QueueID, job.PopReceipt);
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
