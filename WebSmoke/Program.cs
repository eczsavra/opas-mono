var b = WebApplication.CreateBuilder(args);
var a = b.Build();
a.MapGet("/", () => "ok websmoke");
a.Run("http://127.0.0.1:5099");