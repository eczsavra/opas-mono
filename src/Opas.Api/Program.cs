var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Tek endpoint
app.MapGet("/", () => new { ok = true, service = "OPAS.Api minimal" });

// Deterministik port
app.Run("http://127.0.0.1:5080");