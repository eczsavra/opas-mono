using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Persistence.Seed;
using Opas.Infrastructure.Logging;
using Opas.Infrastructure.ScheduledJobs;
using Opas.Infrastructure.Services;
using Opas.Infrastructure.Search.Services;
using Opas.Shared.Logging;

namespace Opas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration cfg,
        IHostEnvironment env)
    {
        var cs = cfg["Database:Postgres:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(cs))
        {
            services.AddDbContext<PublicDbContext>(o => o.UseNpgsql(cs));
            services.AddDbContext<TenantDbContext>(o => o.UseNpgsql(cs));

            // Dev'de seed'i App start'ında tetiklemek için bir hosted service ekleyelim:
            services.AddHostedService(sp => new SeedHostedService(sp, env));
        }

        // OPAS Logging System
        services.AddScoped<IOpasLogger, OpasLogger>();
        services.AddScoped<TenantLoggingService>();
        services.AddScoped<ManagementLoggingService>();
        services.AddScoped<DatabaseLoggingService>();

        // ITS Integration Services
        services.AddScoped<ItsTokenService>();
        services.AddScoped<ItsProductService>();
        services.AddScoped<CentralProductSyncService>(); // NEW - merkezi DB sync
        services.AddScoped<ItsTenantSyncService>();
        services.AddScoped<TenantProductSyncService>(); // NEW - merkezi DB'den tenant'lara sync
        services.AddScoped<TenantGlnSyncService>(); // NEW - GLN sync servisi
        services.AddScoped<TenantProvisioningService>();
        
        // SuperAdmin Services
        services.AddScoped<SuperAdminAuthService>(); // NEW - SuperAdmin authentication

        // Search Services
        services.AddScoped<ISearchService, SearchService>(); // NEW - Search service

        // Scheduled Jobs
        services.AddHostedService<ProductSyncScheduler>();

        return services;
    }

    private sealed class SeedHostedService : IHostedService
    {
        private readonly IServiceProvider _sp;
        private readonly IHostEnvironment _env;

        public SeedHostedService(IServiceProvider sp, IHostEnvironment env)
        {
            _sp = sp;
            _env = env;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetService<PublicDbContext>();
            if (db is null) return;
            await PublicSeeder.EnsureCreatedAndSeedAsync(db, _env);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
