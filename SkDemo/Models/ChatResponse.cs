public class ChatResponse
    {
        public string Reply { get; set; }
        public string ThreadId { get; set; }

        public ChatResponse(string reply)
        {
            Reply = reply;
            ThreadId = string.Empty; // Default value
        }
    }