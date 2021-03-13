using System;
using System.Net;
using Client.Xamarin.ViewModels;
using Client.Xamarin.Views;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Plugin.Settings;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Client.Xamarin
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        
        public App()
        {
            InitializeComponent();

            _serviceProvider = ConfigureServices();

            if (LocalStore.IsLoggedIn())
            {
                MainPage = new NavigationPage(_serviceProvider.GetRequiredService<ChatListPage>());
            }
            else
            {
                MainPage = new NavigationPage(_serviceProvider.GetRequiredService<LoginPage>());
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            services.AddSingleton(CreateClusterClient);

            services.AddScoped<ChatListPage>();
            services.AddScoped<LoginPage>();
            services.AddScoped<ChatPage>();
            services.AddScoped<CreateChatPage>();
            
            services.AddScoped<ChatListPageViewModel>();
            services.AddScoped<LoginPageViewModel>();
            services.AddScoped<ChatPageViewModel>();
            services.AddScoped<CreateChatPageViewModel>();

            _serviceProvider =  services.BuildServiceProvider();

            return _serviceProvider;
        }
        
        private static IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            var localHost = IPAddress.Parse("192.168.1.74");

            var clusterClient = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.ClusterId;
                    options.ServiceId = Constants.ServiceId;
                })
                .AddSimpleMessageStreamProvider(Constants.StreamProvider)
                .UseStaticClustering(new IPEndPoint(localHost, 228))
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(IUser).Assembly)
                        .WithReferences())
                .Build();

            clusterClient.Connect().Wait();

            return clusterClient;
        }
    }
}