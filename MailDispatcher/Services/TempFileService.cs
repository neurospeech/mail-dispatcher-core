using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class TempFileService
    {
        private readonly string TempFolder;

        private static long id = 0;

        public TempFileService(IConfiguration config)
        {
            this.TempFolder = config.GetValue<string>("TempFolder");
        }

        public Stream Create(string name = null)
        {
            name ??= Interlocked.Increment(ref id).ToString();
            return File.Create($"{TempFolder}/{name}.dat", 4096, FileOptions.DeleteOnClose);
        }

        public async Task<Stream> Create(Stream stream, string name = null)
        {
            name ??= Interlocked.Increment(ref id).ToString();
            var fs = File.Create($"{TempFolder}/{name}.dat", 4096, FileOptions.DeleteOnClose);
            await stream.CopyToAsync(fs);
            return fs;
        }
    }
}
