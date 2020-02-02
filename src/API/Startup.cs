using System;
using System.Net;
using DAL;
using GrainInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo {Title = "Chat Orleans API", Version = "v1"});
            });

            services.AddSingleton(CreateClusterClient);
            
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            
            app.EnsureMigrationOfContext<ChatDbContext>();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Orleans"); });
        }

        private IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
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
    }
}