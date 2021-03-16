using System;
using System.Linq;
using System.Threading.Tasks;
using Client.Xamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Client.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatPage : ContentPage
    {
        private readonly ChatPageViewModel _chatPageViewModel;

        public ChatPage(ChatPageViewModel chatPageViewModel)
        {
            _chatPageViewModel = chatPageViewModel;
            BindingContext = _chatPageViewModel;
            
            InitializeComponent();
        }

        public async Task ConnectAsync(Guid chatId)
        {
            await _chatPageViewModel.ConnectAsync(chatId);
            _chatPageViewModel.RefreshCommand.Execute(null);
        }

        protected override async void OnDisappearing()
        {
            await _chatPageViewModel.ClearSessionAsync();
        }
        
        private async void Entry_OnCompleted(object sender, EventArgs e)
        {
            await _chatPageViewModel.SendMyMessageAsync();
            MessagesListView.ScrollTo(_chatPageViewModel.Messages.Last(), ScrollToPosition.End, true);
        }
    }
}