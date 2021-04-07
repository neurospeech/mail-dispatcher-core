using Microsoft.Extensions.DependencyInjection;

namespace MailDispatcher.Storage
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class JobRepository: TableRepository<Job>
    {
        public JobRepository(AzureStorage storage): base(storage)
        {

        }
    }
}
