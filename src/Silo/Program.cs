using System.Net;
using System.Threading.Tasks;
using DAL;
using GrainImplementations;
using GrainInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Silo
{
    class Program
    {
        static Task Main(string[] args)
        {
            var localHost = IPAddress.Parse("192.168.1.74");
            
            return new HostBuilder()
                .UseOrleans(builder =>
                {
                    builder
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = Constants.ClusterId;
                            options.ServiceId = Constants.ServiceId;
                        })
                        .AddSimpleMessageStreamProvider(Constants.StreamProvider)
                        .AddMemoryGrainStorage(Constants.PubSubStore)
                        .UseDashboard(options =>
                        {
                            options.Username = "username";
                            options.Password = "password";
                            options.Host = "*";
                            options.Port = 8080;
                            options.HostSelf = true;
                            options.CounterUpdateIntervalMs = 1000;
                        })
                        .Configure<EndpointOptions>(options =>
                        {
                            // Port to use for Silo-to-Silo
                            options.SiloPort = 11111;
                            // Port to use for the gateway
                            options.GatewayPort = 228;
                            // IP Address to advertise in the cluster
                            options.AdvertisedIPAddress = localHost;
                        })
                        .UseDevelopmentClustering(options =>
                            options.PrimarySiloEndpoint = new IPEndPoint(localHost, 11111))
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(User).Assembly).WithReferences());
                })
                .ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>
                    {
                        options.SuppressStatusMessages = true;
                    });
                    
                    SetupContext(services);
                    
                    DependencyInjectionModule.Load(services);
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                .RunConsoleAsync();
        }

        private static void SetupContext(IServiceCollection services)
        {
            const string host = "localhost";
            const int port = 5432;
            const string name = "chatorleansdb";
            const string password = "qwe123";
            const string user = "postgres";
            
            var connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={name};";
            
            services.AddDbContext<ChatDbContext>(builder =>
                builder
                    .EnableSensitiveDataLogging()
                    .UseNpgsql(connectionString,
                        optionsBuilder =>
                            optionsBuilder.MigrationsAssembly(typeof(ChatDbContext).Assembly.FullName)));
        }
    }
}