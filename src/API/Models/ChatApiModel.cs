using System;

namespace API.Models
{
    public class ChatApiModel
    {
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public Guid OwnerId { get; set; }
    }
}