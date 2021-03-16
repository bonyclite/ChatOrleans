using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Orleans;

namespace GrainInterfaces
{
    public interface IChat : IGrainWithGuidKey
    {
        Task UpdateSettingsAsync(ChatSettingsModel settings);
        Task CreateAsync(ChatSettingsModel settings);
        Task SendMessage(ChatMessageModel message);
        Task SendMessage(ChatMessageModel message, DateTime when);
        Task JoinAsync(IUser user);
        Task Leave(IUser user);
        Task ConnectAsync(IUser user);
        Task JoinAsync(IUser user, IUser joinedUser);
        Task<IEnumerable<ChatMessageModel>> GetHistory(int messageSize);
        Task Disconnect(IUser user);
        Task<bool> IsPrivate();
        Task<ChatModel> GetInfoAsync();
        Task<string> GetNameAsync();
        Task<int> GetOnlineCountMembersAsync();
    }
}