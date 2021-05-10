#nullable enable
using System;

namespace MailDispatcher.Services.Jobs
{
    public class Notification
    {
        public string? Url { get; set; }
        public string? Error { get; set; }

        public DateTime Sent { get; set; }
    }
}
