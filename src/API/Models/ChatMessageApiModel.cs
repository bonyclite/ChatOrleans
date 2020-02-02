using System;

namespace API.Models
{
    public class ChatMessageApiModel
    {
        public Guid UserId { get; set; }
        public string Message { get; set; }
    }
}