using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Orleans;

namespace GrainInterfaces
{
    public interface IChat : IGrainWithGuidKey
    {
        Task UpdateSettings(ChatSettingsModel settings);
        Task Init(ChatSettingsModel settings);
        Task SendMessage(ChatMessageModel message);
        Task SendMessage(ChatMessageModel message, DateTime when);
        Task Join(IUser user);
        Task Leave(IUser user);
        Task Connect(IUser user);
        Task<IEnumerable<ChatMessageModel>> GetHistory(int messageSize);
        Task Disconnect(IUser user);
        Task<bool> IsPrivate();
        Task<ChatModel> GetInfo();
    }
}