using System;
using System.Threading.Tasks;
using GrainInterfaces.Models.Chat;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Utils;

namespace Client.Observers
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
            PrettyConsole.WriteLine($" ======================== {item.User} said: '{item.Text}' ========================", ConsoleColor.Green);
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