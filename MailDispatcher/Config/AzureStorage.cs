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
    public class MailApp
    {
        public readonly string DefaultConnection;

        public MailApp(IConfiguration configuration)
        {
            this.DefaultConnection = configuration.GetConnectionString("DefaultConnection");
        }

    }

    [DIRegister(ServiceLifetime.Singleton)]
    public class AzureStorage
    {
        public readonly CloudTableClient CloudTableClient;
        public readonly BlobServiceClient BlobServiceClient;
        public readonly string ConnectionString;
        public readonly BlobContainerClient MailBlobs;

        public AzureStorage(IConfiguration configuration)
        {
            var cnstr = configuration.GetSection("ConnectionStrings").GetValue<string>("AzureBlobs");
            var account = CloudStorageAccount.Parse(cnstr);
            this.CloudTableClient = account.CreateCloudTableClient();
            this.BlobServiceClient = new BlobServiceClient(cnstr);
            this.ConnectionString = cnstr;

            this.MailBlobs = BlobServiceClient.GetBlobContainerClient("mails3");
            MailBlobs.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        }

    }
}
