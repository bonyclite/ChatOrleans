using System;

namespace GrainInterfaces
{
    public static class Constants
    {
        public const string StreamProvider = "Chat";
        public const string ClusterId = "chat-deployment";
        public const string ServiceId = "ChatApp";
        public const string PubSubStore = "PubSubStore";

        public const string UsersChatActionsStreamNamespace = nameof(UsersChatActionsStreamNamespace);
        public const string ChatMessagesNamespace = nameof(ChatMessagesNamespace);
        public const string ChatOnlineMembersNamespace = nameof(ChatOnlineMembersNamespace);

        public const string SystemUser = "System";
        public static readonly Guid SystemUserId = new Guid("DCB331D8-3D70-4E4A-AF75-71108F706044");

        public const string ChatListId = nameof(ChatListId);
    }
}