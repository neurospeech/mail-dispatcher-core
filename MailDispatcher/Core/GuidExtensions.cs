using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher
{
    public static class GuidExtensions
    {
        public static string ToHexString(this Guid guid)
        {
            return string.Join("", guid.ToByteArray().Select(x => x.ToString("x2")));
        }

        public static string ToHexString(this byte[] data)
        {
            return string.Join("", data.Select(x => x.ToString("x2")));
        }

    }
}
