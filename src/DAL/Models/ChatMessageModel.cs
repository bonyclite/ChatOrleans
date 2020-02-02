using System;

namespace DAL.Models
{
    public class ChatMessageModel : BaseModel
    {
        public virtual UserModel User { get; set; }
        public Guid UserId { get; set; }

        public virtual ChatModel Chat { get; set; }
        public Guid ChatId { get; set; }

        public string Text { get; set; }
    }
}