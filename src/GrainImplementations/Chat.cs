using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Repositories;
using GrainImplementations.Observers;
using GrainImplementations.States;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace GrainImplementations
{
    public class Chat : Grain<ChatState>, IChat
    {
        private readonly IGenericRepository<DAL.Models.ChatModel> _chatRepository;
        private readonly IGenericRepository<DAL.Models.ChatMessageModel> _messagesRepository;

        private IAsyncStream<ChatMessageModel> _chatMessageStream;

        public Chat(IGenericRepository<DAL.Models.ChatModel> chatRepository
            , IGenericRepository<DAL.Models.ChatMessageModel> messagesRepository)
        {
            _chatRepository = chatRepository;
            _messagesRepository = messagesRepository;
        }

        protected override Task ReadStateAsync()
        {
            var d = 2;
            return base.ReadStateAsync();
        }

        public override async Task OnActivateAsync()
        {
            await FillState();
            
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            _chatMessageStream = streamProvider.GetStream<ChatMessageModel>(this.GetPrimaryKey(),
                Constants.ChatMessagesNamespace);

            await _chatMessageStream.SubscribeAsync(
                new ChatMessageObserver(ServiceProvider.GetRequiredService<ILogger<ChatMessageObserver>>()));

            await base.OnActivateAsync();
        }

        protected override async Task WriteStateAsync()
        {
            var chat = await _chatRepository.GetById(this.GetPrimaryKey());

            if (chat == null)
            {
                await _chatRepository.Create(new DAL.Models.ChatModel
                {
                    Id = this.GetPrimaryKey(),
                    Name = State.Settings.Name,
                    OwnerId = State.Settings.OwnerId,
                    IsPrivate = State.Settings.IsPrivate
                });
            }
            else
            {
                chat.Name = State.Settings.Name;
                chat.OwnerId = State.Settings.OwnerId;
                chat.IsPrivate = State.Settings.IsPrivate;

                await _chatRepository.Update(chat);
            }

            foreach (var newMessage in State.NewMessages)
            {
                await _messagesRepository.Create(new DAL.Models.ChatMessageModel
                {
                    ChatId = this.GetPrimaryKey(),
                    UserId = newMessage.UserId,
                    Text = newMessage.Text
                });
            }
            
            await base.WriteStateAsync();
        }

        public async Task UpdateSettings(ChatSettingsModel settings)
        {
            if (State.Settings == null)
            {
            }

            if (State.Settings != null && State.Settings.Name != settings.Name)
            {
                var user = GrainFactory.GetGrain<IUser>(settings.OwnerId);

                await SendMessage(new ChatMessageModel
                {
                    User = Constants.SystemUser,
                    Text = $"{await user.GetNickname()} updated chat name to <{settings.Name}>",
                    UserId = Constants.SystemUserId
                });
            }
            
            State.Settings = settings;

            await WriteStateAsync();
        }

        public async Task Init(ChatSettingsModel settings)
        {
            if (State.IsInitializedFirstly)
            {
                var owner = GrainFactory.GetGrain<IUser>(settings.OwnerId);
                var nickname = await owner.GetNickname();

                await SendMessage(new ChatMessageModel
                {
                    User = Constants.SystemUser,
                    UserId = Constants.SystemUserId,
                    Text = $"{nickname} created the chat «{settings.Name}»"
                });

                State.IsInitializedFirstly = false;
                await UpdateSettings(settings);
            }
        }

        public async Task SendMessage(ChatMessageModel message)
        {
            await _chatMessageStream.OnNextAsync(message);
            
            State.NewMessages.Add(message);
        }

        public Task SendMessage(ChatMessageModel message, DateTime when)
        {
            var sdf = TimeSpan.FromTicks(when.Ticks);
            
            var asd = TimeSpan.FromSeconds(15).Ticks;
            var qwe = sdf.Ticks;
            
            RegisterTimer( o => SendMessage(o as ChatMessageModel), message, TimeSpan.FromTicks(when.Ticks), 
                TimeSpan.FromMilliseconds(-1));
            
            return Task.CompletedTask;
        }

        public async Task Join(IUser user)
        {
            await SendMessage(new ChatMessageModel
            {
                User = Constants.SystemUser,
                UserId = Constants.SystemUserId,
                Text = $"'{await user.GetNickname()}' joined the chat"
            });
            
            await SendUserChatActionModel(user.GetPrimaryKey(), new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Join
            });
        }

        public async Task Leave(IUser user)
        {
            await Disconnect(user);
            
            await SendMessage(new ChatMessageModel
            {
                User = Constants.SystemUser,
                UserId = Constants.SystemUserId,
                Text = $"'{user.GetPrimaryKeyString()}' leaved from chat"
            });

            await SendUserChatActionModel(user.GetPrimaryKey(), new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Leave
            });
        }

        public async Task Connect(IUser user)
        {
            State.OnlineMembers.Add(user.GetPrimaryKey());
            
            await SendUserChatActionModel(user.GetPrimaryKey(), new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Connect
            });
        }

        public Task<IEnumerable<ChatMessageModel>> GetHistory(int messageSize)
        {
            return Task.FromResult(State.Messages.TakeLast(messageSize));
        }

        public async Task Disconnect(IUser user)
        {
            State.OnlineMembers.Remove(user.GetPrimaryKey());
        }

        public Task<bool> IsPrivate()
        {
            return Task.FromResult(State.Settings.IsPrivate);
        }

        public Task<ChatModel> GetInfo()
        {
            return Task.FromResult(new ChatModel
            {
                Id = this.GetPrimaryKey(),
                Name = State.Settings.Name,
                IsPrivate = State.Settings.IsPrivate
            });
        }

        private async Task FillState()
        {
            var chat = await _chatRepository.GetById(this.GetPrimaryKey());

            if (chat != null)
            {
                State.Settings = new ChatSettingsModel
                {
                    Name = chat.Name,
                    IsPrivate = chat.IsPrivate,
                    OwnerId = chat.OwnerId
                };

                var messages = await _messagesRepository
                    .GetAll()
                    .Include(m => m.User)
                    .ToArrayAsync();

                State.Messages = messages.Select(m => new ChatMessageModel
                {
                    Text = m.Text,
                    User = m.User.Nickname,
                    UserId = m.UserId
                }).ToList();
            
                State.IsInitializedFirstly = false;
            }
            
            State.IsInitializedFirstly = true;
        }

        private IAsyncStream<T> GetUserChatActionStream<T>(Guid userId) where T : UserChatActionModel
        {
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            return streamProvider.GetStream<T>(userId, Constants.UsersChatActionsStreamNamespace);
        }

        private async Task SendUserChatActionModel<T>(Guid userId, T model) where T : UserChatActionModel
        {
            var stream = GetUserChatActionStream<T>(userId);

            await stream.OnNextAsync(model);
        }
    }
}