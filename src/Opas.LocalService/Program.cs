// WinExe olarak çalıştığı için console penceresi yok

// Protocol handler argument processing
if (args.Length > 0)
{
    var protocolArg = args[0];
    
    if (protocolArg.StartsWith("opas://"))
    {
        // opas://start-service gibi çağrıları parse et
        var uri = new Uri(protocolArg);
        var action = uri.Host; // "start-service"
        
        // Eğer servis zaten çalışıyorsa, ona HTTP çağrısı yap
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2); // Kısa timeout
            var response = await client.GetAsync($"http://localhost:8080/system/protocol-handler?action={action}");
            
            if (response.IsSuccessStatusCode)
            {
                return; // Exit - servis zaten çalışıyor
            }
        }
        catch
        {
            // Servis çalışmıyor, devam et ve başlat (sessizce)
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Logging ayarları (Debug: Console, Release: File)
#if DEBUG
    builder.Logging.AddConsole(); // Debug'da console'a log
#else
    builder.Logging.ClearProviders();
    builder.Logging.AddFile("logs/opas-local-{Date}.log"); // Release'de dosyaya log
#endif

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for web app communication
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => "OPAS Local Service is healthy");

#if DEBUG
Console.WriteLine("🚀 OPAS Local Service başlatıldı!");
Console.WriteLine("📍 URL: http://localhost:8080");
Console.WriteLine("🔗 Health: http://localhost:8080/health");
Console.WriteLine("⏹️  Durdurmak için Ctrl+C");
Console.WriteLine();
#endif

app.Run("http://localhost:8080");
