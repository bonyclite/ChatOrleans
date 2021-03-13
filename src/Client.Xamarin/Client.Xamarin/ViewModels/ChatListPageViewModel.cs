using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Client.Xamarin.Models;
using GrainInterfaces;
using Orleans;
using Xamarin.Forms;

namespace Client.Xamarin.ViewModels
{
    public class ChatListPageViewModel : RefreshViewModel
    {
        private readonly IClusterClient _client;
        
        public sealed override ICommand RefreshCommand => new Command(async () => await RefreshItemsAsync());
        
        public ObservableCollection<ChatModel> Items { get; set; }
        
        public ChatListPageViewModel(IClusterClient client)
        {
            _client = client;
            Items = new ObservableCollection<ChatModel>();

            RefreshCommand.Execute(null);
        }

        private async Task RefreshItemsAsync()
        {
            IsRefreshing = true;
            
            await LoadChatsAsync();
            
            IsRefreshing = false;
        }

        private async Task LoadChatsAsync()
        {
            Items.Clear();

            var chatList = _client.GetGrain<IChatList>(Constants.ChatListId);

            var chats = await chatList.GetAllAsync();

            foreach (var chat in chats)
            {
                Items.Add(new ChatModel
                {
                    Id = chat.Id,
                    Name = chat.Name
                });
            }
        }
    }
}