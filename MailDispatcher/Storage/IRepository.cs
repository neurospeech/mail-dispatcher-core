using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{
    public interface IRepository<T>
        where T: TableEntity, new()
    {
        public IAsyncEnumerable<TableQuerySegment<T>> QueryAsync(Func<TableQuery<T>, IQueryable<T>> query, CancellationToken token);

        public Task<T> SaveAsync(T entry);

        public Task<T> UpdateAsync(string rowKey, Func<T, T> update);

        public Task DeleteAsync(T entity);

        public Task<T> GetAsync(string rowKey);

    }

}
