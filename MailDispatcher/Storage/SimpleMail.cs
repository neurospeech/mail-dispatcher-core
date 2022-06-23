#nullable enable
using MailDispatcher.Services.Jobs;
using Microsoft.AspNetCore.Http;
using MimeKit;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace MailDispatcher.Storage
{
    public class SimpleMail
    {
        public string? From { get; set; }
        public string[]? To { get; set; }

        public string[]? Cc { get; set; }

        public string[]? Bcc { get; set; }

        public string? ReplyTo { get; set; }

        public string? Subject { get; set; }

        public string? Summary { get; set; }

        public string? TextBody { get; set; }

        public string? HtmlBody { get; set; }

        public string? UnsubscribeLink { get; set; }
        public string? RequestID { get; set; }

        public int Priority { get; set; }

        public (Stream stream, EmailAddress[] recipients) ToMessage(IFormFileCollection? files)
        {
            if (From == null)
                throw new ValidationException("Need from");
            if (Subject == null)
                throw new ValidationException("Need subject");
            if (TextBody == null && HtmlBody == null)
                throw new ValidationException("Need body");


            MimeMessage msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(From));
            List<EmailAddress> recipients
                = new List<EmailAddress>(
                    (To?.Length ?? 0) +
                    (Cc?.Length ?? 0) +
                    (Bcc?.Length ?? 0));
            if (recipients.Capacity == 0)
                throw new ValidationException("Need atleast one recipient");
            AddRecipients(To, msg.To, recipients);
            AddRecipients(Cc, msg.Cc, recipients);
            AddRecipients(Bcc, null, recipients);
            msg.Subject = Subject;

            if(UnsubscribeLink != null)
            {
                msg.Headers.Add(HeaderId.ListUnsubscribe, Encoding.UTF8, $"<{UnsubscribeLink}>");
            }

            if(Summary != null)
            {
                msg.Headers.Add(HeaderId.Summary, Encoding.UTF8, Summary);
            }

            BodyBuilder bb = new BodyBuilder();
            if (TextBody != null)
            {
                bb.TextBody = TextBody;
            }
            if (HtmlBody != null)
            {
                bb.HtmlBody = HtmlBody;
            }

            if(files != null)
            {
                foreach (var file in files) {
                    bb.Attachments.Add(file.FileName, file.OpenReadStream(), ContentType.Parse(file.ContentType));
                }
            }   

            msg.Body = bb.ToMessageBody();
            var ms = new MemoryStream();
            msg.WriteTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return (ms, recipients.ToArray());

            static void AddRecipients(string[]? list, InternetAddressList? addressList, List<EmailAddress> recipients)
            {
                if (list == null)
                    return;
                foreach(var r in list)
                {
                    recipients.Add((EmailAddress)r);
                    addressList?.Add(MailboxAddress.Parse(r));
                }
            }
        }
    }
}
