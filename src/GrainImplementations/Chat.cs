using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Extensions;
using DAL.Models;
using DAL.Repositories;
using GrainImplementations.States;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using ChatMessageModel = GrainInterfaces.Models.Chat.ChatMessageModel;
using ChatModel = GrainInterfaces.Models.Chat.ChatModel;

namespace GrainImplementations
{
    [StorageProvider(ProviderName = Constants.PubSubStore)]
    public class Chat : Grain<ChatState>, IChat
    {
        private readonly IGenericRepository<DAL.Models.ChatModel> _chatRepository;
        private readonly IGenericRepository<DAL.Models.ChatMessageModel> _messagesRepository;

        private IAsyncStream<ChatMessageModel> _chatMessageStream;
        private IAsyncStream<OnlineCountMembersModel> _onlineCountMembersStream;

        public Chat(IGenericRepository<DAL.Models.ChatModel> chatRepository
            , IGenericRepository<DAL.Models.ChatMessageModel> messagesRepository)
        {
            _chatRepository = chatRepository;
            _messagesRepository = messagesRepository;
        }

        public override async Task OnActivateAsync()
        {
            await FillStateAsync();

            RegisterTimer(o => SaveNewMessages(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await SaveNewMessages();
            await base.OnDeactivateAsync();
        }

        protected override async Task WriteStateAsync()
        {
            var chat = await _chatRepository.GetById(this.GetPrimaryKey());

            var user = GrainFactory.GetGrain<IUser>(State.Settings.OwnerNickName);

            if (chat == null)
            {
                await _chatRepository.Create(new DAL.Models.ChatModel
                {
                    Id = this.GetPrimaryKey(),
                    Name = State.Settings.Name,
                    OwnerId = await user.GetUserIdAsync(),
                    IsPrivate = State.Settings.IsPrivate
                });
            }
            else
            {
                chat.Name = State.Settings.Name;
                chat.OwnerId = await user.GetUserIdAsync();
                chat.IsPrivate = State.Settings.IsPrivate;

                await _chatRepository.Update(chat);
            }

            await base.WriteStateAsync();
        }

        public async Task UpdateSettingsAsync(ChatSettingsModel settings)
        {
            if (State.Settings != null && State.Settings.Name != settings.Name)
            {
                await SendMessage(new ChatMessageModel
                {
                    User = Constants.SystemUser,
                    Text = $"{settings.OwnerNickName} updated chat name to <{settings.Name}>",
                    UserId = Constants.SystemUserId
                });
            }

            State.Settings = settings;

            await WriteStateAsync();
        }

        public async Task CreateAsync(ChatSettingsModel settings)
        {
            await UpdateSettingsAsync(settings);
            await FillStateAsync();
            await InitStreamsAsync();

            await SendMessage(new ChatMessageModel
            {
                User = Constants.SystemUser,
                UserId = Constants.SystemUserId,
                Text = $"{settings.OwnerNickName} created the chat «{settings.Name}»"
            });
        }

        public async Task SendMessage(ChatMessageModel message)
        {
            message.CreateDate = DateTime.UtcNow;

            await _chatMessageStream.OnNextAsync(message);

            State.NewMessages.Add(message);
            State.Messages.Add(message);
        }

        public Task SendMessage(ChatMessageModel message, DateTime when)
        {
            RegisterTimer(o => SendMessage(o as ChatMessageModel), message, TimeSpan.FromTicks(when.Ticks),
                TimeSpan.FromMilliseconds(-1));

            return Task.CompletedTask;
        }

        public async Task JoinAsync(IUser user)
        {
            await SendMessage(new ChatMessageModel
            {
                User = Constants.SystemUser,
                UserId = Constants.SystemUserId,
                Text = $"'{user.GetPrimaryKeyString()}' joined the chat"
            });

            await SendUserChatActionModel(await user.GetUserIdAsync(), new UserChatActionModel
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
                Text = $"'{user.GetPrimaryKeyString()}' left from chat"
            });

            await SendUserChatActionModel(await user.GetUserIdAsync(), new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Leave
            });
        }

        public async Task ConnectAsync(IUser user)
        {
            var userId = await user.GetUserIdAsync();
            State.OnlineMembersCount++;

            await SendUserChatActionModel(userId, new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Connect
            });

            await SendOnlineCountMembers();
        }

        public async Task JoinAsync(IUser user, IUser joinedUser)
        {
            await SendMessage(new ChatMessageModel
            {
                User = Constants.SystemUser,
                UserId = Constants.SystemUserId,
                Text = $"'{user.GetPrimaryKeyString()}' added '{joinedUser.GetPrimaryKeyString()}'"
            });

            await SendUserChatActionModel(await joinedUser.GetUserIdAsync(), new UserChatActionModel
            {
                ChatId = this.GetPrimaryKey(),
                Type = UserChatActionType.Join
            });
        }

        public async Task<IEnumerable<ChatMessageModel>> GetHistory(int messageSize)
        {
            return State.Messages
                .OrderBy(m => m.CreateDate)
                .TakeLast(messageSize)
                .ToArray();
        }

        public async Task Disconnect(IUser user)
        {
            if (State.OnlineMembersCount > 0)
            {
                State.OnlineMembersCount--;
                await SendOnlineCountMembers();
            }
        }

        private async Task SendOnlineCountMembers()
        {
            await _onlineCountMembersStream.OnNextAsync(new OnlineCountMembersModel
            {
                CountOnline = State.OnlineMembersCount
            });
        }

        public Task<bool> IsPrivate()
        {
            return Task.FromResult(State.Settings.IsPrivate);
        }

        public Task<ChatModel> GetInfoAsync()
        {
            return Task.FromResult(new ChatModel
            {
                Id = this.GetPrimaryKey(),
                Name = State.Settings.Name,
                IsPrivate = State.Settings.IsPrivate
            });
        }

        public Task<string> GetNameAsync()
        {
            return Task.FromResult(State.Settings.Name);
        }

        public Task<int> GetOnlineCountMembersAsync()
        {
            return Task.FromResult(State.OnlineMembersCount);
        }

        private async Task InitStreamsAsync()
        {
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            _chatMessageStream = streamProvider.GetStream<ChatMessageModel>(State.Id, Constants.ChatMessagesNamespace);
            _onlineCountMembersStream = streamProvider.GetStream<OnlineCountMembersModel>(State.Id, Constants.ChatOnlineMembersNamespace);

            var subscriptionHandles = await _chatMessageStream.GetAllSubscriptionHandles();
            State.OnlineMembersCount = subscriptionHandles.Count;
        }

        private async Task FillStateAsync()
        {
            var chatId = this.GetPrimaryKey();

            if (_chatRepository.GetAll().Any(c => c.Id == chatId))
            {
                var chat = await _chatRepository
                    .GetAll()
                    .Include(c => c.Owner)
                    .FirstOrDefaultAsync(model => model.Id == chatId);

                State.Settings = new ChatSettingsModel
                {
                    Name = chat.Name,
                    IsPrivate = chat.IsPrivate,
                    OwnerNickName = chat.Owner.Nickname
                };

                State.Id = chatId;

                var messages = await _messagesRepository
                    .GetAll()
                    .Where(c => c.ChatId == chatId)
                    .Include(m => m.User)
                    .ToArrayAsync();

                State.Messages = messages.Select(m => new ChatMessageModel
                {
                    Text = m.Text,
                    User = m.User.Nickname,
                    UserId = m.UserId,
                    CreateDate = m.CreateDate
                }).ToList();

                await InitStreamsAsync();
            }
        }

        private async Task SendUserChatActionModel<T>(Guid userId, T model) where T : UserChatActionModel
        {
            var stream = GetUserChatActionStream<T>(userId);

            await stream.OnNextAsync(model);
        }

        private IAsyncStream<T> GetUserChatActionStream<T>(Guid userId) where T : UserChatActionModel
        {
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            return streamProvider.GetStream<T>(userId, Constants.UsersChatActionsStreamNamespace);
        }

        private async Task SaveNewMessages()
        {
            foreach (var newMessage in State.NewMessages)
            {
                await _messagesRepository.Create(new DAL.Models.ChatMessageModel
                {
                    ChatId = this.GetPrimaryKey(),
                    UserId = newMessage.UserId,
                    Text = newMessage.Text,
                    CreateDate = newMessage.CreateDate
                });
            }

            State.NewMessages.RemoveAll();
        }
    }
}