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
    public class ChatsController : Controller
    {
        private readonly IClusterClient _clusterClient;

        public ChatsController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpPost("{chatId}/message")]
        public async Task<string> SendMessage(Guid chatId, [FromBody] ChatMessageApiModel model)
        {
            var chat = _clusterClient.GetGrain<IChat>(chatId);
            var user = _clusterClient.GetGrain<IUser>(model.UserId);

            await chat.SendMessage(new ChatMessageModel
            {
                Text = model.Message,
                User = await user.GetNickname(),
                UserId = user.GetPrimaryKey()
            });

            await chat.SendMessage(new ChatMessageModel
            {
                Text = $"{model.Message} with timer",
                User = await user.GetNickname(),
                UserId = user.GetPrimaryKey()
            }, DateTime.UtcNow.AddSeconds(15));

            return "Ok";
        }
    }
}