using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Orleans;
using Orleans.Streams;
using Plugin.Settings;
using Xamarin.Forms;
using ChatMessageModel = Client.Xamarin.Models.ChatMessageModel;

namespace Client.Xamarin.ViewModels
{
    public class ChatPageViewModel : RefreshViewModel
    {
        private readonly IClusterClient _clusterClient;
        
        private Guid _chatId;
        private string _chatName;
        private StreamSubscriptionHandle<GrainInterfaces.Models.Chat.ChatMessageModel> _chatMessageSubscriptionHandle;
        private StreamSubscriptionHandle<OnlineCountMembersModel> _onlineCountMembersSubscriptionHandle;
        private string _currentMessage;
        private IChat _chat;
        private int _onlineCountMember;

        public sealed override ICommand RefreshCommand => new Command(async () => await RefreshItemsAsync());

        public ObservableCollection<ChatMessageModel> Messages { get; set; }

        public string ChatName
        {
            get => _chatName;
            set
            {
                _chatName = value;
                OnPropertyChanged();
            }
        }

        public string CurrentMessage
        {
            get => _currentMessage;
            set
            {
                _currentMessage = value;
                OnPropertyChanged();
            }
        }

        public int OnlineCountMember
        {
            get => _onlineCountMember;
            set
            {
                _onlineCountMember = value;
                OnPropertyChanged();
            }
        }

        public ChatPageViewModel(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;

            Messages = new ObservableCollection<ChatMessageModel>();
        }

        public async Task ClearSessionAsync()
        {
            await _chat.Disconnect(LocalStore.GetUserGrain());
            _chat = null;
            Messages.Clear();
            
            if (_chatMessageSubscriptionHandle is not null)
            {
                await _chatMessageSubscriptionHandle.UnsubscribeAsync();
                _chatMessageSubscriptionHandle = null;
            }

            if (_onlineCountMembersSubscriptionHandle is not null)
            {
                await _onlineCountMembersSubscriptionHandle.UnsubscribeAsync();
                _onlineCountMembersSubscriptionHandle = null;
            }
        }
        
        public async Task ConnectAsync(Guid chatId)
        {
            _chatId = chatId;
            
            _chat = _clusterClient.GetGrain<IChat>(_chatId);
            ChatName = await _chat.GetNameAsync();
            OnlineCountMember = await _chat.GetOnlineCountMembersAsync();

            var streamProvider = _clusterClient.GetStreamProvider(Constants.StreamProvider);
            
            var chatMessageStream = streamProvider.GetStream<GrainInterfaces.Models.Chat.ChatMessageModel>(chatId, Constants.ChatMessagesNamespace);
            _chatMessageSubscriptionHandle = await chatMessageStream.SubscribeAsync(ChatMessageHandle);
            
            var onlineCountMembersActionStream = streamProvider.GetStream<OnlineCountMembersModel>(chatId, Constants.ChatOnlineMembersNamespace);
            _onlineCountMembersSubscriptionHandle = await onlineCountMembersActionStream.SubscribeAsync(OnlineMemberChangedActionHandle);
            
            await _chat.ConnectAsync(LocalStore.GetUserGrain());
        }

        private async Task RefreshItemsAsync()
        {
            IsRefreshing = true;

            await LoadHistoryAsync();
            OnlineCountMember = await _chat.GetOnlineCountMembersAsync();

            IsRefreshing = false;
        }

        private async Task LoadHistoryAsync()
        {
            var history = await _chat.GetHistory(1_000);
            
            Messages.Clear();

            foreach (var messageModel in history)
            {
                ShowMessage(messageModel);
            }
        }

        private Task ChatMessageHandle(GrainInterfaces.Models.Chat.ChatMessageModel model, StreamSequenceToken token)
        {
            ShowMessage(model);
            
            return Task.CompletedTask;
        }

        private Task OnlineMemberChangedActionHandle(OnlineCountMembersModel model, StreamSequenceToken token = null)
        {
            OnlineCountMember = model.CountOnline;
            
            return Task.CompletedTask;
        }

        private void ShowMessage(GrainInterfaces.Models.Chat.ChatMessageModel messageModel)
        {
            var currentUserNickName = LocalStore.GetUserNickName();

            var color = messageModel.User switch
            {
                _ when messageModel.User == currentUserNickName => Color.CornflowerBlue,
                _ when messageModel.User == Constants.SystemUser => Color.WhiteSmoke,
                _ => Color.DarkGray
            };

            Messages.Add(new ChatMessageModel
            {
                BackgroundColor = color,
                UserNickName = messageModel.User,
                MessageText = messageModel.Text
            });
        }

        public async Task SendMyMessageAsync()
        {
            await _chat.SendMessage(new GrainInterfaces.Models.Chat.ChatMessageModel
            {
                User = LocalStore.GetUserNickName(),
                Text = CurrentMessage,
                UserId = LocalStore.GetUserId()
            });
        }
    }
}