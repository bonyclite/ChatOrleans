using System;

namespace GrainInterfaces.Models.Chat
{
    public class ChatModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
    }
}