﻿using Azure.Storage.Blobs;
using Azure.Storage.Queues;
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

    public class SimpleMail
    {
        public string From { get; set; }
        public string[] To { get; set; }

        public string[] Cc { get; set; }

        public string[] Bcc { get; set; }

        public string Subject { get; set; }

        public string TextBody { get; set; }

        public string HtmlBody { get; set; }
    }

    public class RawMessageRequest
    {
        public string From { get; set; }

        public string[] Recipients { get; set; }

        public string Content { get; set; }
    }

    public class JobResponse
    {
        public string Domain { get; set; }

        public string Error { get; set; }

        public DateTime? Sent { get; set; }

        [IgnoreProperty]
        public bool Success => Sent != null && string.IsNullOrEmpty(Error);
    }
    public class Job: TableEntity
    {
        public string AccountID { get; set; }

        public string From { get; set; }
        public string Recipients { get; set; }

        public string Url { get; set; }

        public DateTime? Locked { get; set; }

        public int Tries { get; set; }

        public string ResponsesJson
        {
            get => Responses == null ? null : JsonSerializer.Serialize(Responses);
            set => Responses = value == null ? null : JsonSerializer.Deserialize<JobResponse[]>(value);
        }

        [IgnoreProperty]
        public JobResponse[] Responses { get; set; }

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

    [DIRegister(ServiceLifetime.Singleton)]
    public class JobStorage
    {
        private readonly CloudTable Identity;
        private readonly AccountRepository accountRepository;
        private readonly JobRepository repository;
        private readonly BlobContainerClient blobs;
        private readonly BlobContainerClient reports;
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
            RawMessageRequest message,
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
                From = message.From,
                Recipients = JsonSerializer.Serialize(message.Recipients),
                RowKey = id.ToString(),
                Url = blob.Uri.ToString()
            };


            var qid = await queue.SendMessageAsync(id, TimeSpan.FromMilliseconds(500));
            body.QueueID = qid.Value.MessageId;
            await repository.SaveAsync(body);
            return id;

        }

        public async Task RemoveAsync(Job job)
        {
            await job.Message.DeleteIfExistsAsync();
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
