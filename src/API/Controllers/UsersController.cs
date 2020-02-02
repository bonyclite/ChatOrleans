using System;
using System.Threading.Tasks;
using API.Models;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using GrainInterfaces.Models.User;
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
            var user = _clusterClient.GetGrain<IUser>(Guid.NewGuid());

            await user.Save(new UserModel
            {
                Nickname = model.UserName
            });
            
            return new UserApiModel
            {
                Id = user.GetPrimaryKey(),
                UserName = await user.GetNickname()
            };
        }
        
        [HttpPost("chat")]
        public async Task<ChatModel> Create(ChatApiModel model)
        {
            var user = _clusterClient.GetGrain<IUser>(model.OwnerId);
            
            var chat = await user.CreateChat(new CreateChatModel
            {
                Settings = new ChatSettingsModel
                {
                    Name = model.Name,
                    IsPrivate = model.IsPrivate,
                    OwnerId = model.OwnerId
                }
            });

            return await chat.GetInfo();
        }
    }
}