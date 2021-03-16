using System;
using System.Threading.Tasks;
using API.Models;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly IClusterClient _clusterClient;

        public UsersController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpPost]
        public async Task<UserApiModel> Create(UserApiModel model)
        {
            var user = _clusterClient.GetGrain<IUser>(model.UserName);

            return new UserApiModel
            {
                Id = await user.GetUserIdAsync(),
                UserName = user.GetPrimaryKeyString()
            };
        }
        
        [HttpPost("chat")]
        public async Task<ChatModel> Create(ChatApiModel model)
        {
            var user = _clusterClient.GetGrain<IUser>(model.OwnerNickName);

            var chatId = Guid.NewGuid();
            var chat = _clusterClient.GetGrain<IChat>(chatId);
            
            await chat.CreateAsync(new ChatSettingsModel
            {
                Name = model.Name,
                IsPrivate = model.IsPrivate,
                OwnerNickName = user.GetPrimaryKeyString()
            });

            await chat.JoinAsync(user);
            
            return await chat.GetInfoAsync();
        }
    }
}