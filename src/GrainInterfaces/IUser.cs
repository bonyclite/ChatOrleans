using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using GrainInterfaces.Models.User;
using Orleans;

namespace GrainInterfaces
{
    public interface IUser : IGrainWithGuidKey
    {
        Task<IChat> CreateChat(CreateChatModel model);
        Task Save(UserModel model);
        Task<string> GetNickname();
    }
}