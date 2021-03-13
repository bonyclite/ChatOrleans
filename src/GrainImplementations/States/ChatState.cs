using System;
using System.Collections.Generic;
using GrainInterfaces.Models.Chat;

namespace GrainImplementations.States
{
    public class ChatState
    {
        public ChatSettingsModel Settings { get; set; }
        public Guid Id { get; set; }
        public int OnlineMembersCount { get; set; }
        public List<ChatMessageModel> Messages { get; set; }
        public List<ChatMessageModel> NewMessages { get; set; }
        
        public ChatState()
        {
            Messages = new List<ChatMessageModel>();
            NewMessages = new List<ChatMessageModel>();
        }
    }
}