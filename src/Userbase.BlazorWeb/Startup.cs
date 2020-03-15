using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Userbase.BlazorWeb.Data;
using Userbase.Client;
using Userbase.Client.Api;
using Userbase.Client.Data;
using Userbase.Client.Ws;

namespace Userbase.BlazorWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();

            var appId = Configuration["appid"];
            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var api = new AuthApi(config);
            services.AddSingleton(config);
            services.AddSingleton(localData);
            services.AddSingleton<ILogger, TestLogger>();
            services.AddSingleton(api);
            services.AddSingleton<WsWrapper>();
            services.AddSingleton<AuthMain>();
            services.AddSingleton<Db>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapHub<ChatHub>("/chatHub");
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }

    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }

    public class FakeLocalData : IStorage
    {
        public string GetItem(string key)
        {
            return string.Empty;
        }
        public void SetItem(string key, string value) {}
    }

    public class TestLogger : ILogger
    {
        private readonly IHubContext<ChatHub> _chatHub;

        public TestLogger(IHubContext<ChatHub> chatHub)
        {
            _chatHub = chatHub;
        }

        public async Task Log(string message)
        {
            await _chatHub.Clients.All.SendAsync("ReceiveMessage", "Server", message);
        }
    }
}
