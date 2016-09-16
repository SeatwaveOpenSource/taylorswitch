using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace taylorswitch
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseDefaultFiles().UseStaticFiles();
        }

        public static void Main(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile("wwwroot/setup.json", optional: true)
                .Build();

            new WebHostBuilder()
                .UseUrls(configuration.GetSection("server.urls").Value)
                .UseContentRoot(currentDirectory)
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
