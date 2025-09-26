using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Services;

namespace Opas.Infrastructure.ScheduledJobs;

/// <summary>
/// Günlük 06:00'da ITS ürün senkronizasyonu yapan scheduled job
/// </summary>
public class ProductSyncScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductSyncScheduler> _logger;
    private readonly TimeSpan _syncTime = new(6, 0, 0); // 06:00

    public ProductSyncScheduler(
        IServiceProvider serviceProvider,
        ILogger<ProductSyncScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product Sync Scheduler started. Next run at 06:00");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = GetNextSyncTime(now);
                var delay = nextRun - now;

                _logger.LogInformation("Next product sync scheduled for: {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunProductSyncAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Product Sync Scheduler cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Product Sync Scheduler");
                
                // Hata durumunda 1 saat bekle
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task RunProductSyncAsync()
    {
        _logger.LogInformation("Starting scheduled product sync at {Time}", DateTime.Now);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ItsTenantSyncService>();

            // Sadece yeni ürünleri ekle (mevcut customization'lara dokunma)
            await syncService.SyncAllTenantsAsync(onlyNew: true);

            _logger.LogInformation("Scheduled product sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled product sync failed");
        }
    }

    private DateTime GetNextSyncTime(DateTime now)
    {
        var today = now.Date;
        var syncTimeToday = today.Add(_syncTime);

        // Eğer bugünün sync saati geçmişse, yarının sync saatini al
        if (now > syncTimeToday)
        {
            return today.AddDays(1).Add(_syncTime);
        }

        return syncTimeToday;
    }
}
