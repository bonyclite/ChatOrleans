using System;
using System.Collections.Generic;

namespace DAL.Models
{
    public class ChatModel : BaseModel
    {
        public string Name { get; set; }
        public bool IsPrivate { get; set; }

        public Guid OwnerId { get; set; }
        public virtual UserModel Owner { get; set; }

        public virtual ICollection<UserChatModel> Users { get; set; }
    }
}