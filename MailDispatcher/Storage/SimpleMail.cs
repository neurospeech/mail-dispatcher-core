﻿namespace MailDispatcher.Storage
{
    public class SimpleMail
    {
        public string From { get; set; }
        public string[] To { get; set; }

        public string[] Cc { get; set; }

        public string[] Bcc { get; set; }

        public string Subject { get; set; }

        public string TextBody { get; set; }

        public string HtmlBody { get; set; }
    }
}
