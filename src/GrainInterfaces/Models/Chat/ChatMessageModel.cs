using System;

namespace GrainInterfaces.Models.Chat
{
    public class ChatMessageModel
    {
        public Guid UserId { get; set; }
        public string User { get; set; }
        public string Text { get; set; }
        public DateTime CreateDate { get; set; }
    }
}