using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using API.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.Azure;
// using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var client = new SecretClient(new Uri("https://examplekeyvaultcjk.vault.azure.net/"), new DefaultAzureCredential(), options);

            KeyVaultSecret connString = client.GetSecret("BlogStorageConnectionString");
            KeyVaultSecret containerName = client.GetSecret("FileContrainerName");

            // Great! I am able to grab the values from the key vault.
            // The new problem is how do I use those values in the config....

            AzureStorageConfig azureStorageConfig = new AzureStorageConfig
            {
                ConnectionString = connString.Value,
                FileContainerName = containerName.Value
            };  

            // Set up IOptions and populate AzureStorageConfig from configuration
            services.AddOptions();
            //services.Configure<AzureStorageConfig>(Configuration.GetSection("AzureStorageConfig"));

            // Wire up a single instance of BlobStorage, calling Initialize() when we first use it.
            //services.AddSingleton<IStorage>(serviceProvider => {
            //    var blobStorage = new BlobStorage(serviceProvider.GetService<IOptions<AzureStorageConfig>>());
            //    blobStorage.Initialize().GetAwaiter().GetResult();
            //    return blobStorage;
            //});

            services.AddSingleton<IStorage>(serviceProvider => {
                var blobStorage = new BlobStorage(azureStorageConfig);
                blobStorage.Initialize().GetAwaiter().GetResult();
                return blobStorage;
            });

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(routes =>
            {
                routes.MapDefaultControllerRoute();
                routes.MapControllers();
            });
        }
    }
}
