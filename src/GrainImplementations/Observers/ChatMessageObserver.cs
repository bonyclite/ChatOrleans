using System;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace GrainImplementations.Observers
{
    public class ChatMessageObserver : IAsyncObserver<ChatMessageModel>
    {
        private readonly ILogger<ChatMessageObserver> _logger;

        public ChatMessageObserver(ILogger<ChatMessageObserver> logger)
        {
            _logger = logger;
        }
        
        public Task OnNextAsync(ChatMessageModel item, StreamSequenceToken token = null)
        {
            _logger.LogInformation($" ======================== {item.User} said: '{item.Text}' ========================");
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            _logger.LogInformation("Message stream received stream completed event");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            _logger.LogInformation($"Chat is experiencing message delivery failure, ex :{ex}");
            return Task.CompletedTask;
        }
    }
}