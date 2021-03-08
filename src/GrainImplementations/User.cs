using System;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using DAL.Repositories;
using GrainImplementations.States;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace GrainImplementations
{    
    [StorageProvider(ProviderName = Constants.PubSubStore)]
    public class User : Grain<UserState>, IUser
    {
        private readonly IGenericRepository<UserModel> _usersRepository;
        private readonly IGenericRepository<UserChatModel> _userChatRepository;

        public User(IGenericRepository<UserModel> usersRepository
            , IGenericRepository<UserChatModel> userChatRepository)
        {
            _usersRepository = usersRepository;
            _userChatRepository = userChatRepository;
        }
        
        public override async Task OnActivateAsync()
        {
            var nickName = this.GetPrimaryKeyString();
            
            var user = await _usersRepository.GetAll()
                .Include(u => u.Chats)
                .FirstOrDefaultAsync(u => u.Nickname == nickName);
            
            if (user == null)
            {
                user = await _usersRepository.Create(new UserModel
                {
                    Id = Guid.NewGuid(),
                    Nickname = nickName
                });
            }
            
            State.JoinedChats = user.Chats?.Select(c => c.ChatId).ToList();
            State.UserId = user.Id;
            
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            var stream = streamProvider.GetStream<UserChatActionModel>(State.UserId,
                Constants.UsersChatActionsStreamNamespace);
            await stream.SubscribeAsync(UserChatActionHandle);

            await base.OnActivateAsync();
        }

        protected override async Task WriteStateAsync()
        {
            var nickName = this.GetPrimaryKeyString();
            
            var user = await _usersRepository
                .GetAll(u => u.Nickname == nickName)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                user = await _usersRepository.Create(new UserModel
                {
                    Id = Guid.NewGuid(),
                    Nickname = nickName
                });
            }
            else
            {
                user.Nickname = nickName;
                await _usersRepository.Update(user);
            
                await _userChatRepository.DeleteWhere(u => u.UserId == user.Id);

                foreach (var joinedChat in State.JoinedChats)
                {
                    await _userChatRepository.Create(new UserChatModel
                    {
                        ChatId = joinedChat,
                        UserId = user.Id
                    });
                }   
            }

            State.UserId = user.Id;
            
            await base.WriteStateAsync();
        }

        public async Task<IChat> CreateChat(CreateChatModel model)
        {
            var chatId = Guid.NewGuid();

            model.Settings.OwnerNickName = this.GetPrimaryKeyString();

            var chat = GrainFactory.GetGrain<IChat>(chatId);
            await chat.Init(model.Settings);

            await chat.Join(this);

            return chat;
        }

        public Task<Guid> GetUserIdAsync()
        {
            return Task.FromResult(State.UserId);
        }

        private async Task UserChatActionHandle(UserChatActionModel model, StreamSequenceToken token = null)
        {
            switch (model.Type)
            {
                case UserChatActionType.Join:
                    State.JoinedChats.Add(model.ChatId);
                    break;

                case UserChatActionType.Leave:
                    State.JoinedChats.Remove(model.ChatId);
                    break;
                
                case UserChatActionType.Connect:
                    State.ConnectedChatId = model.ChatId;
                    break;

                case UserChatActionType.Disconnect:
                    State.ConnectedChatId = null;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await WriteStateAsync();
        }
    }
}