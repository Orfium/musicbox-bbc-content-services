using Microsoft.Extensions.DependencyInjection;
using MusicManager.Application.Services;
using MusicManager.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Infrastructure
{
    public static class AwsCollectionExtension
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services)
        {
            services.AddScoped<IAWSS3Repository, AWSS3Repository>();
            services.AddSingleton<IDeliveryDestinationS3ClientRepository,DeliveryDestinationS3ClientRepository>();
            return services;
        }
    }
}
