using System;
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

        public async Task LoadHistoryAsync(Guid chatId)
        {
            await _chatPageViewModel.InitAsync(chatId);
            _chatPageViewModel.RefreshCommand.Execute(null);
        }

        private async void Entry_OnCompleted(object sender, EventArgs e)
        {
            await _chatPageViewModel.SendMyMessageAsync();
        }

        protected override async void OnDisappearing()
        {
            await _chatPageViewModel.ClearSessionAsync();
        }
    }
}