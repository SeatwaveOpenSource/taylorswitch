using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace taylorswitch
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            app.UseDefaultFiles().UseStaticFiles();

            app.MapWhen(context => context.Request.Path == "/getFeatures", _app =>
            {
                _app.Run(async context =>
                {
                    //var json = ShakeItOff("http://localhost:5000", "/features").Result;
                    await context.Response.WriteAsync("shake it off!");
                });
            });
        }

        private async Task<object> ShakeItOff(string baseUrl, string path)
        {
            object json = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsAsync<object>();
                }
            }

            return json;
        }

        public static void Main(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            new WebHostBuilder().UseConfiguration
                (
                    new ConfigurationBuilder()
                        .SetBasePath(currentDirectory)
                        .AddJsonFile("hosting.json", optional: true)
                        .Build()
                )
                .UseContentRoot(currentDirectory)
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
