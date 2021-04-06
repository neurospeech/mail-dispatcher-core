using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{
    public class CachedTableRepository<T>: TableRepository<T>
        where T: TableEntity, new()
    {
        private readonly AppCache<T> cache;

        public CachedTableRepository(
            AzureStorage storage,
            AppCache<T> cache): base(storage)
        {
            this.cache = cache;
        }

        public override Task DeleteAsync(T entity)
        {
            cache.Remove(entity.RowKey);
            return base.DeleteAsync(entity);
        }

        public override Task<T> GetAsync(string rowKey, bool create = false)
        {
            return cache.GetOrCreateAsync(rowKey, (x) =>
            {
                x.SlidingExpiration = TimeSpan.FromMinutes(5);
                return base.GetAsync(rowKey, create);
            });
        }

        public override async Task<T> SaveAsync(T entity)
        {
            var r = await base.SaveAsync(entity);
            cache.Remove(entity.RowKey);
            return cache.GetOrCreate(entity.RowKey, (x) => {
                x.SlidingExpiration = TimeSpan.FromMinutes(5);
                return r;
            });
        }
    }

    public class TableRepository<T> : IRepository<T>
        where T: TableEntity, new()
    {
        private readonly CloudTable table;
        private readonly string partitionKey;

        public TableRepository(AzureStorage storage)
        {
            this.table = storage.CloudTableClient.GetTableReference(typeof(T).FullName.Replace(".","").ToLower());
            table.CreateIfNotExists();
            this.partitionKey = typeof(T).Name;
        }

        public virtual Task DeleteAsync(T entity)
        {
            return table.ExecuteAsync(TableOperation.Delete(entity));
        }

        public virtual async Task<T> GetAsync(string rowKey, bool create = false)
        {
            var q = table.CreateQuery<T>()
                .Where(x => x.PartitionKey == partitionKey && x.RowKey == rowKey)
                as TableQuery<T>;
            
            var r = await table.ExecuteQuerySegmentedAsync(q, null);
            var first = r.FirstOrDefault();
            if(first == null && create)
            {
                first = Activator.CreateInstance<T>();
                first.PartitionKey = partitionKey;
                first.RowKey = rowKey;
            }
            return first;
        }

        public virtual async IAsyncEnumerable<TableQuerySegment<T>> QueryAsync(
            Func<TableQuery<T>, IQueryable<T>> query = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            var q = table.CreateQuery<T>().Where(x => x.PartitionKey == this.partitionKey);
            if(query != null)
                q = query(q as TableQuery<T>) as TableQuery<T>;
            TableContinuationToken continuationToken = null;
            while (true)
            {
                var r = await table.ExecuteQuerySegmentedAsync(q as TableQuery<T>, continuationToken, token);
                yield return r;
                continuationToken = r.ContinuationToken;
                if (continuationToken == null)
                    break;
            }
        }

        public virtual async Task<T> SaveAsync(T entity)
        {
            entity.PartitionKey = this.partitionKey;
            var x = await table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            return x.Result as T;
        }

        public virtual async Task<T> UpdateAsync(string rowKey, Func<T, T> update)
        {
            var r = await GetAsync(rowKey);
            r = update(r);
            return await SaveAsync(r);
        }
    }

}
