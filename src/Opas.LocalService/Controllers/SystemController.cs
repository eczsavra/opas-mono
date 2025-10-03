using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.ServiceProcess;

namespace Opas.LocalService.Controllers;

[ApiController]
[Route("system")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Protocol handler endpoint - opas://start-service çağrısı buraya gelir
    /// </summary>
    [HttpGet("protocol-handler")]
    public IActionResult HandleProtocolCall([FromQuery] string? action = null, [FromQuery] string? param = null)
    {
        _logger.LogInformation($"Protocol handler called: action={action}, param={param}");

        try
        {
            switch (action?.ToLowerInvariant())
            {
                case "start":
                case "start-service":
                    return StartService();
                
                case "restart":
                case "restart-service":
                    return RestartService();
                
                case "status":
                    return GetServiceStatus();
                
                case "stop":
                case "stop-service":
                    return StopService();
                
                default:
                    return Ok(new { 
                        success = true, 
                        message = "OPAS Local Service is running",
                        availableActions = new[] { "start", "restart", "status", "stop" }
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling protocol call");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Servisi başlat (eğer çalışmıyorsa)
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartService()
    {
        try
        {
            // Servis zaten çalışıyor demektir (bu endpoint'e ulaşabiliyorsak)
            _logger.LogInformation("Service start requested - service is already running");
            
            return Ok(new { 
                success = true, 
                message = "Service is already running",
                status = "running"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Servisi yeniden başlat
    /// </summary>
    [HttpPost("restart")]
    public IActionResult RestartService()
    {
        try
        {
            _logger.LogInformation("Service restart requested");
            
            // Graceful shutdown başlat (background'da)
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // 2 saniye bekle (response dönmesi için)
                Environment.Exit(0); // Kendini kapat (Windows Service Manager tekrar başlatacak)
            });

            return Ok(new { 
                success = true, 
                message = "Service restart initiated",
                status = "restarting"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting service");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Servisi durdur
    /// </summary>
    [HttpPost("stop")]
    public IActionResult StopService()
    {
        try
        {
            _logger.LogInformation("Service stop requested");
            
            // Graceful shutdown başlat (background'da)
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // 1 saniye bekle (response dönmesi için)
                Environment.Exit(0); // Kendini kapat
            });

            return Ok(new { 
                success = true, 
                message = "Service stop initiated",
                status = "stopping"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping service");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Servis durumunu getir
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetServiceStatus()
    {
        try
        {
            var status = new
            {
                isRunning = true,
                startTime = Process.GetCurrentProcess().StartTime,
                uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                processId = Process.GetCurrentProcess().Id,
                version = "1.0.0",
                services = new
                {
                    pos = true, // TODO: Gerçek POS durumu
                    printer = true, // TODO: Gerçek yazıcı durumu  
                    medula = true // TODO: Gerçek MEDULA durumu
                }
            };

            return Ok(new { 
                success = true, 
                status = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service status");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Windows Service olarak kurulu mu kontrol et
    /// </summary>
    [HttpGet("installation-status")]
    public IActionResult GetInstallationStatus()
    {
        try
        {
            bool isInstalled = IsServiceInstalled("OPAS Local Service");
            bool isAutoStart = false;

            if (isInstalled)
            {
                // Auto-start durumunu kontrol et
                isAutoStart = CheckAutoStartStatus();
            }

            return Ok(new { 
                success = true,
                isInstalled = isInstalled,
                isAutoStart = isAutoStart,
                serviceName = "OPAS Local Service"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking installation status");
            return StatusCode(500, new { 
                success = false, 
                error = ex.Message 
            });
        }
    }

    private bool IsServiceInstalled(string serviceName)
    {
        try
        {
            // Windows platform kontrolü
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var service = new ServiceController(serviceName);
            var status = service.Status; // Bu satır exception fırlatır eğer servis yoksa
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckAutoStartStatus()
    {
        try
        {
            // Registry'den startup type kontrol et
            // TODO: Implement registry check
            return true; // Şimdilik true dön
        }
        catch
        {
            return false;
        }
    }
}
