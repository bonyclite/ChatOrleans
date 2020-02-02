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
using UserModel = GrainInterfaces.Models.User.UserModel;

namespace GrainImplementations
{    
    [StorageProvider(ProviderName = "PubSubStore")]
    public class User : Grain<UserState>, IUser
    {
        private readonly IGenericRepository<DAL.Models.UserModel> _usersRepository;
        private readonly IGenericRepository<UserChatModel> _userChatRepository;

        public User(IGenericRepository<DAL.Models.UserModel> usersRepository
            , IGenericRepository<UserChatModel> userChatRepository)
        {
            _usersRepository = usersRepository;
            _userChatRepository = userChatRepository;
        }
        
        public override async Task OnActivateAsync()
        {
            var user = await _usersRepository.GetAll()
                .Include(u => u.Chats)
                .FirstOrDefaultAsync(u => u.Id == this.GetPrimaryKey());
            
            State.JoinedChats = user?.Chats.Select(c => c.ChatId).ToList();
            State.Nickname = user?.Nickname;
            
            var streamProvider = GetStreamProvider(Constants.StreamProvider);

            var stream = streamProvider.GetStream<UserChatActionModel>(this.GetPrimaryKey(),
                Constants.UsersChatActionsStreamNamespace);
            await stream.SubscribeAsync(UserChatActionHandle);

            await base.OnActivateAsync();
        }

        protected override async Task WriteStateAsync()
        {
            var user = await _usersRepository.GetById(this.GetPrimaryKey());

            if (user == null)
            {
                await _usersRepository.Create(new DAL.Models.UserModel
                {
                    Id = this.GetPrimaryKey(),
                    Nickname = State.Nickname
                });
            }
            else
            {
                user.Nickname = State.Nickname;
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
            
            await base.WriteStateAsync();
        }

        public async Task<IChat> CreateChat(CreateChatModel model)
        {
            var chatId = Guid.NewGuid();

            model.Settings.OwnerId = this.GetPrimaryKey();

            var chat = GrainFactory.GetGrain<IChat>(chatId);
            await chat.Init(model.Settings);

            await chat.Join(this);

            return chat;
        }

        public async Task Save(UserModel model)
        {
            State.Nickname = model.Nickname;
            
            await WriteStateAsync();
        }

        public Task<string> GetNickname()
        {
            return Task.FromResult(State.Nickname);
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