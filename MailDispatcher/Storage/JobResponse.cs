using Microsoft.Azure.Cosmos.Table;
using System;
using System.Text.Json.Serialization;

namespace MailDispatcher.Storage
{
    public class JobResponse
    {
        public string Domain { get; set; }

        public string Error { get; set; }

        public string Warning { get; set; }

        public DateTime? Sent { get; set; }

        [JsonIgnore]
        public bool Success => Sent != null && string.IsNullOrEmpty(Error);

        public string ErrorCode { get; set; }

        public void AppendError(string error)
        {
            if(Error==null)
            {
                Error = error;
                return;
            }

            Error += "\r\n" + error;
        }
    }
}
