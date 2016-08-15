using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                    var json = await Get<object>("http://localhost:5000", "/features");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{json}");
                });
            });

            app.Map("/updateFeature", _app =>
            {
                _app.Run(async context =>
                {
                    var json = await Get<object>("http://localhost:5000", "/features");

                    if (json == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    var subscribers = await Get<List<string>>("http://localhost:5000", "/subscribers");

                    IEnumerable<Task<bool>> updateSubscribers = subscribers.Select(subscriber => Send(subscriber, "/testUpdate"));

                    await Task.WhenAll(updateSubscribers);

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

        private static async Task<T> Get<T>(string url, string query) where T : class, new()
        {
            T subscribers = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await client.GetAsync(query);
                if (response.IsSuccessStatusCode)
                {
                    subscribers = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
                }
            }

            return subscribers;
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
