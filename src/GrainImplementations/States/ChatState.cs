using System;
using System.Collections.Generic;
using GrainInterfaces.Models.Chat;

namespace GrainImplementations.States
{
    public class ChatState
    {
        public ChatSettingsModel Settings { get; set; }
        public bool IsInitializedFirstly { get; set; } = true;
        public List<Guid> OnlineMembers { get; set; }
        public List<ChatMessageModel> Messages { get; set; }
        public List<ChatMessageModel> NewMessages { get; set; }
        
        public ChatState()
        {
            OnlineMembers = new List<Guid>();
            
            Messages = new List<ChatMessageModel>();
            NewMessages = new List<ChatMessageModel>();
        }
    }
}