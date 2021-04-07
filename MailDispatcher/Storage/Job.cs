using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Text.Json;

namespace MailDispatcher.Storage
{
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
}
