using System;

namespace GrainInterfaces.Models.Chat
{
    public class ChatSettingsModel
    {
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public Guid OwnerId { get; set; }
    }
}