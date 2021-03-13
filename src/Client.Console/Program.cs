using System;
using System.Net;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
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

            var serviceProvider = services.BuildServiceProvider();

            var consoleSession = serviceProvider.GetRequiredService<ConsoleSession>();
            await consoleSession.Start();
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