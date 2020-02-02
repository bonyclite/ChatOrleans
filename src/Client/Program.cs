using System;
using System.Net;
using System.Threading.Tasks;
using DAL;
using GrainInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            
            services.AddSingleton(CreateClusterClient);
            services.AddSingleton<ConsoleSession>();

            SetupContext(services);
            DependencyInjectionModule.Load(services);
            
            var serviceProvider = services.BuildServiceProvider();

            var consoleSession = serviceProvider.GetRequiredService<ConsoleSession>();
            await consoleSession.Start();
        }
        
        private static IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            var localHost = IPAddress.Parse("127.0.0.1");

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
                .ConfigureLogging(_ => _.AddConsole())
                .Build();

            clusterClient.Connect().Wait();

            return clusterClient;
        }

        private static void SetupContext(IServiceCollection services)
        {
            const string host = "localhost";
            const int port = 5432;
            const string name = "chatorleansdb";
            const string password = "qwe123";
            const string user = "postgres";
            
            var connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={name};";;
            
            services.AddDbContext<ChatDbContext>(builder =>
                builder
                    .EnableSensitiveDataLogging()
                    .UseNpgsql(connectionString,
                        optionsBuilder =>
                            optionsBuilder.MigrationsAssembly(typeof(ChatDbContext).Assembly.FullName)));
        }
    }
}