using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Repositories;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Orleans.Providers;

namespace GrainImplementations
{
    [StorageProvider(ProviderName = Constants.PubSubStore)]
    public class ChatList : Grain, IChatList
    {
        private readonly IGenericRepository<DAL.Models.ChatModel> _chatsRepository;
        
        public ChatList(IGenericRepository<DAL.Models.ChatModel> chatsRepository)
        {
            _chatsRepository = chatsRepository;
        }
        
        public async Task<IReadOnlyCollection<ChatModel>> GetAllAsync()
        {
            return await _chatsRepository
                .GetAll()
                .Select(c => new ChatModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsPrivate = c.IsPrivate
                })
                .ToListAsync();
        }
    }
}