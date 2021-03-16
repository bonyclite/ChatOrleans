using System;
using Client.Xamarin.Models;
using Client.Xamarin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;

namespace Client.Xamarin.Views
{
    public partial class ChatListPage : ContentPage
    {
        private readonly ChatListPageViewModel _pageViewModel;
        private readonly IServiceProvider _serviceProvider;

        public ChatListPage(ChatListPageViewModel chatListPageViewModel
            , IServiceProvider serviceProvider)
        {
            _pageViewModel = chatListPageViewModel;
            _serviceProvider = serviceProvider;
            BindingContext = _pageViewModel;

            InitializeComponent();
        }

        private async void ListView_OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is ChatModel chatModel)
            {
                var chatPage = _serviceProvider.GetRequiredService<ChatPage>();
                await Navigation.PushAsync(chatPage);
                await chatPage.ConnectAsync(chatModel.Id);
            }
            
            ((ListView)sender).SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            _pageViewModel?.RefreshCommand.Execute(null);
        }

        private async void ImageButton_OnClicked(object sender, EventArgs e)
        {
            var chatPage = _serviceProvider.GetRequiredService<CreateChatPage>();
            await Navigation.PushAsync(chatPage); 
        }
    }
}