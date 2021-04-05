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
    }


    public class MessageBody: TableEntity
    {
        public string From { get; set; }
        public string Recipients { get; set; }

        public string Url { get; set; }

        [IgnoreProperty]
        public BlobClient Message { get; set; }

        [IgnoreProperty]
        public Account Account { get; set; }

    }
    
    [DIRegister(ServiceLifetime.Singleton)]
    public class JobRepository: TableRepository<MessageBody>
    {
        public JobRepository(AzureStorage storage): base(storage)
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
            var id = $"{Guid.NewGuid()}/{nid}.eml";
            var blob = blobs.GetBlobClient(id);
            await blob.UploadAsync(file.OpenReadStream());

            var body = new MessageBody() {
                PartitionKey = accountId,
                RowKey = id.ToString(),
                From = message.From,
                Recipients = JsonConvert.SerializeObject(message.Recipients),
                Url = blob.Uri.ToString()
            };

            await repository.SaveAsync(body);



            return id;

        }


        public async Task<MessageBody[]> DequeueAsync(CancellationToken stoppingToken)
        {
            var items = await queue.ReceiveMessagesAsync(16, TimeSpan.FromSeconds(30), stoppingToken);
            var tasks = items.Value.Select(async x => {
                var msg = await repository.GetAsync(x.MessageId);
                msg.Message = blobs.GetBlobClient(x.MessageId);
                msg.Account = await accountRepository.GetAsync(msg.PartitionKey);
                return msg;
            });
            return await Task.WhenAll(tasks);
        }

    }
}
