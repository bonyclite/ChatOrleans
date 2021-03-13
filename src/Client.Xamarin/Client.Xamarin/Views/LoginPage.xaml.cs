using System;
using Client.Xamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Client.Xamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private readonly LoginPageViewModel _model;
        private readonly ChatListPage _chatListPage;

        public LoginPage(LoginPageViewModel model
            , ChatListPage chatListPage)
        {
            _model = model;
            _chatListPage = chatListPage;

            BindingContext = _model;

            InitializeComponent();
        }

        private async void Entry_OnCompleted(object sender, EventArgs e)
        {
            await _model.LoginAsync();
            Application.Current.MainPage =  new NavigationPage(_chatListPage);
        }
    }
}