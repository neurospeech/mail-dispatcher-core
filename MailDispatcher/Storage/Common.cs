using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Storage
{
    public readonly struct Key
    {
        public readonly string PartitionKey;
        public readonly string RowKey;

        public Key(string pkey, string rkey)
        {
            this.PartitionKey = pkey;
            this.RowKey = rkey;
        }

        public static implicit operator Key(long id)
        {
            if (id == 0)
                return new Key("0", "0");
            var pkey = id & 0xFF;
            var rkey = (id >> 8);
            return new Key(pkey.ToString(), rkey.ToString());
        }
    }

    public static class SequenceGenerator
    {
        public static Task InsertAsync<T>(this CloudTable table, T entity)
            where T : TableEntity
        {
            var insertOpertion = TableOperation.Insert(entity);
            return table.ExecuteAsync(insertOpertion);
        }

        public static Task UpdateAsync<T>(this CloudTable table, T entity, CancellationToken stoppingToken)
            where T : TableEntity
        {
            var insertOpertion = TableOperation.Replace(entity);
            return table.ExecuteAsync(insertOpertion, stoppingToken);
        }

        public async static Task<T> FirstAsync<T>(this CloudTable table, Key key, CancellationToken stoppingToken)
            where T : TableEntity, new()
        {
            string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key.PartitionKey);
            string rowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, key.RowKey);
            string finalFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            var query = new TableQuery<T>().Where(finalFilter);
            var res = await table.ExecuteQuerySegmentedAsync<T>(query, null, stoppingToken).ConfigureAwait(false);
            return res.FirstOrDefault();

        }

        public static async Task<long> GenerateSequenceAsync(this CloudTable table)
        {
            SequenceGeneratorModel sequence = await GetSequenceRecordAsync(table).ConfigureAwait(false);

            if (sequence == null)
            {
                sequence = new SequenceGeneratorModel
                {
                    PartitionKey = "default",
                    RowKey = "rowId",
                    SequenceId = 1
                };
                var insertOperation = TableOperation.Insert(sequence);
                await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
            }
            else
            {
                await RegenerateSequenceAsync(sequence, table).ConfigureAwait(false);
            }

            return sequence.SequenceId;
        }

        private static async Task<SequenceGeneratorModel> GetSequenceRecordAsync(CloudTable table)
        {
            string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "default");
            string rowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rowId");
            string finalFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            TableQuery<SequenceGeneratorModel> query = new TableQuery<SequenceGeneratorModel>().Where(finalFilter);
            var res = await table.ExecuteQuerySegmentedAsync<SequenceGeneratorModel>(query, null);
            return res.FirstOrDefault();
        }

        private static async Task<SequenceGeneratorModel> RegenerateSequenceAsync(SequenceGeneratorModel sequence, CloudTable table)
        {
            try
            {
                sequence.SequenceId++;
                TableResult tblr = await table.ExecuteAsync(
                    TableOperation.InsertOrReplace(sequence),
                    null,
                    new OperationContext { UserHeaders = new Dictionary<string, string> { { "If-Match", sequence.ETag } } });
            }
            catch (StorageException ex)
            {
                ////"Optimistic concurrency violation – entity has changed since it was retrieved."
                if (ex.RequestInformation.HttpStatusCode == 412)
                {
                    await Task.Delay(300);
                    sequence = await GetSequenceRecordAsync(table);
                    await RegenerateSequenceAsync(sequence, table);
                }
                else
                {
                    throw;
                }
            }

            return sequence;
        }

        public class SequenceGeneratorModel : TableEntity
        {
            public long SequenceId { get; set; }

            public byte[] Data { get; set; }
        }
    }
}
