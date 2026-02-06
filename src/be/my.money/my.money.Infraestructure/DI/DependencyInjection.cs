using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using my.money.application.Ports.Persistence;
using my.money.Infraestructure.Persistence;
using my.money.Infraestructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.Infraestructure.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();

            // services.AddScoped<IQuoteRepository, QuoteRepository>(); // solo si Quote es AR

            return services;
        }
    }
}
