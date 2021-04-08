using Microsoft.Azure.Cosmos.Table;
using System;

namespace MailDispatcher.Storage
{
    public class JobResponse
    {
        public string Domain { get; set; }

        public string Error { get; set; }

        public string Warning { get; set; }

        public DateTime? Sent { get; set; }

        [IgnoreProperty]
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
