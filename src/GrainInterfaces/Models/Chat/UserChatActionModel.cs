using System;

namespace GrainInterfaces.Models.Chat
{
    public class UserChatActionModel
    {
        public Guid ChatId { get; set; }
        public UserChatActionType Type { get; set; }
    }

    public enum UserChatActionType
    {
        Join = 1,
        Leave,
        Connect,
        Disconnect
    }
}