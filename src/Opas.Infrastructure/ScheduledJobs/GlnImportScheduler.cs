using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using System.Text.Json;

namespace Opas.Infrastructure.ScheduledJobs;

/// <summary>
/// Günlük GLN import scheduler - her gün 05:00'te çalışır
/// </summary>
public class GlnImportScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlnImportScheduler> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // 24 saat
    private readonly TimeSpan _startTime = new TimeSpan(5, 0, 0); // 05:00

    public GlnImportScheduler(IServiceProvider serviceProvider, ILogger<GlnImportScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GLN Import Scheduler started. Next run at 05:00");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = GetNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogInformation("Next GLN import scheduled for: {NextRun}", nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformGlnImport();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GLN Import Scheduler cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GLN Import Scheduler");
                // Hata durumunda 1 saat bekle ve tekrar dene
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var today = now.Date.Add(_startTime);
        
        if (now < today)
        {
            return today; // Bugün 05:00
        }
        else
        {
            return today.AddDays(1); // Yarın 05:00
        }
    }

    private async Task PerformGlnImport()
    {
        _logger.LogInformation("Starting scheduled GLN import at {Time}", DateTime.Now);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PublicDbContext>();
            var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();

            var token = await tokenProvider.GetTokenAsync("ITS-Access");
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("ITS token alınamadı, import atlandı");
                return;
            }

            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var body = new { stakeholderType = "eczane", getAll = true };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("https://its2.saglik.gov.tr/reference/app/stakeholder/", content);
            resp.EnsureSuccessStatusCode();

            var jsonResp = await resp.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResp);

            JsonElement[] list;
            if (responseObj.TryGetProperty("companyList", out var companyListProp) && companyListProp.ValueKind == JsonValueKind.Array)
            {
                list = companyListProp.EnumerateArray().ToArray();
            }
            else
            {
                _logger.LogError("ITS response format beklenmeyen");
                return;
            }

            if (list == null || list.Length == 0)
            {
                _logger.LogWarning("ITS'den boş liste geldi");
                return;
            }

            var upserted = 0;
            var added = 0;
            var updated = 0;

            foreach (var item in list)
            {
                if (!item.TryGetProperty("gln", out var glnEl) || glnEl.ValueKind != JsonValueKind.String)
                    continue;
                    
                var gln = glnEl.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(gln))
                    continue;

                var rec = await db.GlnRegistry.FirstOrDefaultAsync(x => x.Gln == gln);
                var isNew = rec == null;

                if (rec == null)
                {
                    rec = new GlnRecord { Gln = gln };
                    db.GlnRegistry.Add(rec);
                    added++;
                }
                else
                {
                    updated++;
                }

                rec.CompanyName = item.TryGetProperty("companyName", out var cn) ? cn.GetString() : null;
                rec.Authorized = item.TryGetProperty("authorized", out var auth) ? auth.GetString() : null;
                rec.Email = item.TryGetProperty("email", out var em) ? em.GetString() : null;
                rec.Phone = item.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
                rec.City = item.TryGetProperty("city", out var ci) ? ci.GetString() : null;
                rec.Town = item.TryGetProperty("town", out var to) ? to.GetString() : null;
                rec.Address = item.TryGetProperty("address", out var ad) ? ad.GetString() : null;
                rec.Active = item.TryGetProperty("active", out var ac) ? ac.GetBoolean() : null;
                rec.ImportedAtUtc = DateTime.UtcNow;

                upserted++;
            }

            await db.SaveChangesAsync();

            _logger.LogInformation("Scheduled GLN import completed. Total: {Total}, Added: {Added}, Updated: {Updated}", 
                upserted, added, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled GLN import failed");
        }
    }
}
