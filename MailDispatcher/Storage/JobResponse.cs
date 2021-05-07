using MailDispatcher.Services.Jobs;
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

        public string ErrorCode { get; set; }
        public Notification[] Notifications { get; set; }
    }
}
