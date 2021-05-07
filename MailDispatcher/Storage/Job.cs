using Azure.Storage.Blobs;
using Newtonsoft.Json;
using System;

namespace MailDispatcher.Storage
{
    public class Job
    {
        public string AccountID { get; set; }

        public string From { get; set; }
        public string Recipients { get; set; }

        public string MessageBodyUrl { get; set; }

        public DateTime? Locked { get; set; }

        public int Tries { get; set; }

        public string Status { get; set; }

        public string ResponsesJson
        {
            get => Responses == null ? null : JsonConvert.SerializeObject(Responses);
            set => Responses = value == null ? null : JsonConvert.DeserializeObject<JobResponse[]>(value);
        }

        [JsonIgnore]
        public JobResponse[] Responses { get; set; }
        public string RowKey { get; set; }
    }
}
