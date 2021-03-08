using System;

namespace GrainInterfaces.Models.Chat
{
    public class ChatSettingsModel
    {
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public string OwnerNickName { get; set; }
    }
}