using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{env.EnvironmentName}.setup.json", optional: true)
                .Build();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var featuresBaseUrl = Configuration.GetSection("featuresBaseUrl").Value;
            var featuresRequestUri = Configuration.GetSection("featuresRequestUri").Value;

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
                    var json = await Get<object>(featuresBaseUrl, featuresRequestUri);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{json}");
                });
            });

            app.Map("/updateFeature", _app =>
            {
                _app.Run(async context =>
                {
                    if (!(await Send(featuresBaseUrl, featuresRequestUri, context.Request.Body)))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    var subscribersBaseUrl = Configuration.GetSection("subscribersBaseUrl").Value;
                    var subscribersRequestUri = Configuration.GetSection("subscribersRequestUri").Value;

                    var subscribers = await Get<List<string>>(subscribersBaseUrl, subscribersRequestUri);

                    IEnumerable<Task<bool>> updateSubscribers = subscribers.Select(subscriber => Send(subscriber, "/killCache"));

                    await Task.WhenAll(updateSubscribers);

                    context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                });
            });
        }

        private static async Task<bool> Send(string url, string requestUri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri));

                return response.IsSuccessStatusCode;
            }
        }

        private static async Task<bool> Send(string url, string requestUri, Stream data)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);

                string json;
                using (var sr = new StreamReader(data))
                    json = sr.ReadToEnd();

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUri, httpContent);

                return response.IsSuccessStatusCode;
            }
        }

        private static async Task<T> Get<T>(string url, string requestUri) where T : class, new()
        {
            T subscribers = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await client.GetAsync(requestUri);
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
