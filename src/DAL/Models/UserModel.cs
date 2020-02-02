using System.Collections.Generic;

namespace DAL.Models
{
    public class UserModel : BaseModel
    {
        public string Nickname { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        public virtual ICollection<ChatModel> SelfChats { get; set; }

        public virtual ICollection<UserChatModel> Chats { get; set; }
    }
}