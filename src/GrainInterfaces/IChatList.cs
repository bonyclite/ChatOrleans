using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Orleans;

namespace GrainInterfaces
{
    public interface IChatList : IGrainWithStringKey
    {
        Task<IReadOnlyCollection<ChatModel>> GetAllAsync();
        Task<IReadOnlyCollection<ChatModel>> GetAllAsync(Guid userId);
    }
}