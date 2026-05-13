using Microsoft.Extensions.DependencyInjection;
using WorldBank_CRUD.API.Services;

namespace WorldBank_CRUD.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<TokenService>();
            services.AddScoped<RabbitMqService>();

            return services;
        }
    }
}