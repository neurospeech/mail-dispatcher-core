#nullable enable
using System;
using System.Linq;

namespace MailDispatcher.Services.Jobs
{
    public class EmailAddress
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Domain { get; set; }
        public string User { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static explicit operator EmailAddress (string address)
        {
            var tokens = address.Trim().Split('@');
            if (tokens.Length != 2)
                throw new ArgumentException($"Invalid email address {address}");
            return new EmailAddress
            {
                User = tokens[0],
                Domain = tokens.Last()
            };
        }

        public override string ToString()
        {
            return $"{User}@{Domain}";
        }

        private static char[] Separators = new char[] { ',',';' };

        public static EmailAddress[] ParseList(string addresses)
        {
            var tokens = addresses.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new EmailAddress[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = (EmailAddress)tokens[i];
            }
            return result;
        }
    }
}
