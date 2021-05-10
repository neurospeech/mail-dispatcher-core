using Azure.Storage.Blobs;
using MailDispatcher.Services.Jobs;
using Newtonsoft.Json;
using System;

namespace MailDispatcher.Storage
{
    public class Job
    {
        public string AccountID { get; set; }

        public string From { get; set; }
        public EmailAddress[] Recipients { get; set; }

        public string MessageBodyUrl { get; set; }

        public string RowKey { get; set; }
        public string BlobPath { get; set; }
    }
}
