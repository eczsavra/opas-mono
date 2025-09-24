using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi.Models;
using Microsoft.FeatureManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;
using Npgsql;

using Opas.Application;                    // AssemblyMarker
using Opas.Application.Diagnostics;
using Opas.Application.Common;
using Opas.Domain.ValueObjects;
using Opas.Infrastructure;
using Opas.Infrastructure.Persistence;
using Opas.Shared;
using Opas.Shared.MultiTenancy;
using Opas.Infrastructure.MultiTenancy;
using Opas.Shared.Validation;
using Opas.Api.Endpoints.Control;
using Opas.Api.Endpoints.Infra;
using Opas.Shared.ControlPlane;
using Opas.Infrastructure.ScheduledJobs;
using Opas.Infrastructure.Services;


// ✅ yeni: endpoint modül çağrısı için
using Opas.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Localization (default: tr-TR)
var defaultCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
builder.Services.AddLocalization();

// ✅ HttpContextAccessor (tek kez)
builder.Services.AddHttpContextAccessor();

// ✅ MediatR (tek kez)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
});

// ✅ Validators & Pipeline (tek kez)
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ✅ Feature Flags
builder.Services.AddFeatureManagement();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss ");

// Multi-tenancy helpers
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

// ---- Control Plane DI (koşullu) ----
var cpConn =
    builder.Configuration["Database:ControlPlane:ConnectionString"]
    ?? builder.Configuration["ControlPlane:ConnectionString"];

if (!string.IsNullOrWhiteSpace(cpConn))
{
builder.Services.AddDbContext<ControlPlaneDbContext>(opt =>
    opt.UseNpgsql(cpConn));
}

// register http client + hosted service
builder.Services.AddHttpClient("its", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});

// NVI HTTP Client
builder.Services.AddHttpClient("nvi", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30); // NVI servisi yavaş olabilir
    c.DefaultRequestHeaders.Add("User-Agent", "OPAS-Pharmacy-System/1.0");
});
// builder.Services.AddHostedService<ItsTokenService>(); // ItsTokenService IHostedService değil

// Swagger (dev only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OPAS API", Version = "v1" });
});

// Infrastructure (DbContext vb.)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ITS Token Service
builder.Services.AddScoped<ItsTokenService>();

// NVI Service
builder.Services.AddScoped<NviService>();

// Token Provider
builder.Services.AddScoped<ITokenProvider, TokenProvider>();

// Scheduled Jobs
builder.Services.AddHostedService<GlnImportScheduler>();

var app = builder.Build();


// Correlation Id
app.Use(async (ctx, next) =>
{
    const string Header = "x-request-id";
    if (!ctx.Request.Headers.TryGetValue(Header, out var cid) || string.IsNullOrWhiteSpace(cid))
        cid = Guid.NewGuid().ToString("N");

    ctx.TraceIdentifier = cid!;
    ctx.Response.Headers[Header] = cid!;
    await next();
});

// TenantContext (basit)
app.Use(async (ctx, next) =>
{
    const string TenantHeader = "X-Tenant-Id";
    var tenantId = ctx.Request.Headers.TryGetValue(TenantHeader, out var v) && !string.IsNullOrWhiteSpace(v)
        ? v.ToString().Trim()
        : "default";

    var tenant = new TenantContext(tenantId);
    ctx.Items["TenantContext"] = tenant;

    await next();
});

// Request culture (yalnızca tr-TR)
var supportedCultures = new[] { new CultureInfo("tr-TR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --------------------------
// Mevcut endpoint’ler (şimdilik burada kalsın)



app.MapCoreEndpoints();   
app.MapInfraEndpoints();
app.MapTenantEndpoints();   
app.MapAuthNviEndpoints(); 
app.MapAuthUsernameEndpoints(); // Added Username Check Endpoints
app.MapAuthEmailCheckEndpoints(); // Added Email Check Endpoints
app.MapAuthPharmacistRegistrationEndpoints(); // Added PharmacistAdmin Registration Endpoints
app.MapControlEndpoints();
app.MapControlGlnEndpoints();
app.MapDiagnosticsEndpoints();
app.MapInfraHealthEndpoints();
app.MapControlTenantEndpoints();
app.MapPublicProductsEndpoints();
app.MapAuthRegistrationEndpoints();
app.MapAuthLoginEndpoints(); // Added Login Endpoints
app.MapAuthPasswordResetEndpoints(); // Added Password Reset Endpoints

// --- GLN DRY-RUN PING ---
// GET /control/gln/sync/dry-run
app.MapGet("/control/gln/sync/dry-run", (IConfiguration cfg) =>
{
    // Konfig alanları: sadece var mı yok mu kontrol
    var baseUrl   = cfg["Integrations:State:GlnApi:BaseUrl"];
    var user      = cfg["Integrations:State:GlnApi:User"];
    var pass      = cfg["Integrations:State:GlnApi:Password"];
    var nightlyAt = cfg["Integrations:State:GlnApi:NightlyAt"]; // "05:00" gibi

    var configOk = !string.IsNullOrWhiteSpace(baseUrl)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(pass);

    return Results.Ok(new
    {
        ok = true,
        configOk,
        baseUrlSet = !string.IsNullOrWhiteSpace(baseUrl),
        userSet    = !string.IsNullOrWhiteSpace(user),
        passSet    = !string.IsNullOrWhiteSpace(pass),
        nightlyAt  = nightlyAt ?? "not-set",
        note = "Bu sadece kurgu/doğrulama. Henüz gerçek HTTP çağrısı yok."
    });
});


// Configuration'dan server ayarlarını oku
var host = builder.Configuration["Server:Host"] ?? "127.0.0.1";
var port = builder.Configuration["Server:Port"] ?? "5080";
var url = $"http://{host}:{port}";

app.Run(url);
