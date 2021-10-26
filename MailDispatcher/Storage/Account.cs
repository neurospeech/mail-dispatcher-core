using Microsoft.Azure.Cosmos.Table;
using MimeKit.Cryptography;
using System.IO;
using System.Text.Json.Serialization;

namespace MailDispatcher.Storage
{
    public class Account: TableEntity
    {

        [IgnoreProperty]
        public string ID {
            get => this.RowKey;
            set => this.RowKey = value;
        }
        public string AuthKey { get; set; }

        public string DomainName { get; set; }

        public string Selector { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }

        public bool Active { get; set; }

        public bool EnableMailboxes { get; set; }

        public string Password { get; set; }

        public string Notifications { get; set; }

        public string BounceNoticeEmails { get; set; }

        public string BounceTriggers { get; set; }


        private DkimSigner signer;
        [IgnoreProperty]
        [JsonIgnore]
        public DkimSigner DkimSigner
        {
            get
            {
                if (signer != null)
                    return signer;
                var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(PrivateKey));
                signer = new DkimSigner(ms, DomainName, Selector);
                return signer;
            }
            set
            {
                signer = null;
            }
        }
    }

}
