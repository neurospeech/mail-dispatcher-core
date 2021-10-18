using Azure.Storage.Blobs;
using MailDispatcher.Services.Jobs;
using MailDispatcher.Storage;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class BounceService
    {
        private readonly BlobContainerClient bounced;
        private readonly WorkflowService workflowService;

        public BounceService(
            AzureStorage storge,
            WorkflowService workflowService
            )
        {
            this.bounced = storge.BlobServiceClient.GetBlobContainerClient("bounced");
            this.bounced.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            this.workflowService = workflowService;
        }

        internal async Task SendAsync(
            IMailbox address,
            Stream ms)
        {
            string user = address.User;
            string domain = address.Host;

            var tokens = user.Split('-');
            string accountId = tokens[0];
            string id = tokens[1];
            
            var blob = bounced.GetBlobClient(id + ".eml");
            await blob.UploadAsync(ms);

            var postBody = JsonSerializer.Serialize(new
            {
                domain = domain,
                addresses = new string[] { address.ToString() },
                id = id,
                code = "Unknown",
                error = blob.Uri.ToString()
            });

            await BounceWorkflow.CreateAsync(workflowService, new BounceNotification
            {
                AccountID = accountId,
                Error = postBody
            });

        }
    }
}
