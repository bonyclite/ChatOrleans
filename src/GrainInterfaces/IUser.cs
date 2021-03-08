using System;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Orleans;

namespace GrainInterfaces
{
    public interface IUser : IGrainWithStringKey
    {
        Task<IChat> CreateChat(CreateChatModel model);
        Task<Guid> GetUserIdAsync();
    }
}