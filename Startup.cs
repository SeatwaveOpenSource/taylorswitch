using System;
using System.IO;
using System.Net;
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

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles().UseStaticFiles();

            app.Map("/getFeatures", _app =>
            {
                _app.Run(async context =>
                {
                    var json = await Get("http://localhost:5000", "/features");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{json}");
                });
            });

            app.Map("/updateFeature", _app =>
            {
                _app.Run(async context =>
                {
                    var json = await Get("http://localhost:5000", "/features");
                  
                    if (json == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    //foreach or something subscribers ditch the cache
                    var subscribers = await Get("http://localhost:5000", "/subscribers");
                    await Send("http://localhost:5001", "/testUpdate");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{json}");
                });
            });

            app.Map("/testUpdate", _app => 
            {
                _app.Run(async context =>
                {
                    await context.Response.WriteAsync("it worked");
                });
            });
        }

        private static async Task<bool> Send(string url, string query)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, query));

                return response.IsSuccessStatusCode;
            }
        } 

        private static async Task<object> Get(string url, string query)
        {
            object json = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await client.GetAsync(query);
                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsStringAsync();
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
