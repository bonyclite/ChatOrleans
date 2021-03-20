using System;
using System.Linq;
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
    internal class Program
    {
        private const string Host = "3.134.149.250";
        private const int Port = 5432;
        private const string Name = "chatorleansdb";
        private const string Password = "qwe123";
        private const string User = "postgres";
        private static string ConnectionString => $"Host={Host};Port={Port};Username={User};Password={Password};Database={Name};";
        private static string GateWayConnectionString => $"Host={Host};Port={Port};Username={User};Password={Password};Database=orleansDb;";

        private static Task Main(params string[] args)
        {
            var gateWayPort = 0;
            var siloPort = 0;
            var dashBoardPort = 0;

            foreach (var s in args)
            {
                if (s.Contains("gateWayPort"))
                {
                    gateWayPort = Convert.ToInt32(s.Split("gateWayPort=").Last());
                }
                
                if (s.Contains("siloPort"))
                {
                    siloPort = Convert.ToInt32(s.Split("siloPort=").Last());
                }

                if (s.Contains("dashBoardPort"))
                {
                    dashBoardPort = Convert.ToInt32(s.Split("dashBoardPort=").Last());
                }
            }
            
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
                            options.Port = dashBoardPort;
                            options.HostSelf = true;
                            options.CounterUpdateIntervalMs = 1000;
                        })
                        .ConfigureEndpoints(siloPort, gateWayPort, listenOnAnyHostAddress: true)
                        .UseAdoNetClustering(options =>
                        {
                            options.Invariant = Constants.InvariantNamePostgreSql;
                            options.ConnectionString = GateWayConnectionString;
                        })
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
            services.AddDbContext<ChatDbContext>(builder =>
                builder
                    .EnableSensitiveDataLogging()
                    .UseNpgsql(ConnectionString,
                        optionsBuilder =>
                            optionsBuilder.MigrationsAssembly(typeof(ChatDbContext).Assembly.FullName)));
        }
    }
}