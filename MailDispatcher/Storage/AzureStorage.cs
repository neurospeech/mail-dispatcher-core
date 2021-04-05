using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class AzureStorage
    {
        public readonly CloudTableClient CloudTableClient;
        public readonly BlobServiceClient BlobServiceClient;

        public AzureStorage(IConfiguration configuration)
        {
            var cnstr = configuration.GetSection("ConnectionStrings").GetValue<string>("AzureBlobs");
            var account = CloudStorageAccount.Parse(cnstr);
            this.CloudTableClient = account.CreateCloudTableClient();
            this.BlobServiceClient = new BlobServiceClient(cnstr);
        }
    }
}
