#nullable enable
using DurableTask.Core;
using MailDispatcher.Storage;
using NeuroSpeech.Workflows;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MailDispatcher.Services.Jobs
{

    [Workflow]
    public class SendEmailWorkflow : Workflow<Job, JobResponse[]>
    {
        public async override Task<JobResponse[]> RunTask(Job job)
        {
            job.RowKey = context!.OrchestrationInstance.InstanceId;
            var rlist = job.Recipients.Split(',', ';')
                .Select(x => x.Trim())
                .Select(x => (tokens: x.ToLower().Split('@'), address: x))
                .Where(x => x.tokens.Length > 1)
                .Select(x => (domain: x.tokens.Last(), x.address))
                .GroupBy(x => x.domain)
                .Select(x => new DomainJob(job, x))
                .Select(SendEmailAsync)
                .ToList();

            var r = await Task.WhenAll(rlist);

            return r;

        }

        public Task<JobResponse> SendEmailAsync(DomainJob job)
        {
            return context!.CreateSubOrchestrationInstance<JobResponse>(typeof(SendEmailToDomain), job);
        }
    }
}
