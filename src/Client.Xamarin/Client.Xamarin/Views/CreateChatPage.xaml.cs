using System;
using System.Linq;
using Client.Xamarin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Client.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateChatPage : ContentPage
    {
        private readonly CreateChatPageViewModel _createChatPageViewModel;
        private readonly IServiceProvider _serviceProvider;

        public CreateChatPage(CreateChatPageViewModel createChatPageViewModel
            , IServiceProvider serviceProvider)
        {
            _createChatPageViewModel = createChatPageViewModel;
            _serviceProvider = serviceProvider;
            BindingContext = _createChatPageViewModel;
            
            InitializeComponent();
        }
        
        private async void Button_OnClicked(object sender, EventArgs e)
        {
            var chatId = await _createChatPageViewModel.CreateAsync();
            Navigation.RemovePage(this);

            var chatPage = _serviceProvider.GetRequiredService<ChatPage>();
            await Navigation.PushAsync(chatPage); 
            await chatPage.LoadHistoryAsync(chatId);
        }
    }
}