using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elasticsearch
{
    public static class ElasticSearchExtension
    {
        public static void AddElasticsearch(
            this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration.GetValue<string>("AppSettings:elasticsearch:url");

            //var settings = new ConnectionSettings(new Uri(url))              
            //   .MaximumRetries(8)
            //   .SniffOnStartup()
            //   .RequestTimeout(TimeSpan.FromMilliseconds(6000))
            //   .PingTimeout(TimeSpan.FromMilliseconds(6000))               
            //   .ThrowExceptions();

            var settings = new ConnectionSettings(new Uri(url))
             .MaximumRetries(2)
             .SniffOnStartup()
             .PingTimeout(TimeSpan.FromMilliseconds(2000))
             .ThrowExceptions();


            //var settings = new ConnectionSettings(new Uri(url));

            var client = new ElasticClient(settings);
            
            // maybe check response to be safe...
            services.AddSingleton<IElasticClient>(client);
        }
    }
}
