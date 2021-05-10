using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    public static class Tasks
    {

        public static Task<TOutput[]> WhenAll<TInput, TOutput>(this IEnumerable<TInput> list, Func<TInput, Task<TOutput>> task)
        {
            return Task.WhenAll(
                list.Select(x => task(x))
                .ToList());
        }

    }
}
