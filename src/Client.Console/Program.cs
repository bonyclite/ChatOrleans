using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Client.Console
{
    partial class Program
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
            var clusterClient = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.ClusterId;
                    options.ServiceId = Constants.ServiceId;
                })
                .AddSimpleMessageStreamProvider(Constants.StreamProvider)
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = Constants.InvariantNamePostgreSql;
                    options.ConnectionString = GateWayConnectionString;
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(IUser).Assembly)
                        .WithReferences())
                .Build();

            clusterClient.Connect().Wait();

            return clusterClient;
        }
    }
}