namespace MailDispatcher.Storage
{
    public class RawMessageRequest
    {
        public string From { get; set; }

        public string[] Recipients { get; set; }

        public string Content { get; set; }
    }
}
