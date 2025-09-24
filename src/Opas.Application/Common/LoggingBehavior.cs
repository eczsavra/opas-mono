using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Opas.Application.Common;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var reqName = typeof(TRequest).Name;

        _logger.LogInformation("CQRS START {Request}", reqName);

        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("CQRS END   {Request} durMs={Dur} ok=true", reqName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "CQRS ERR   {Request} durMs={Dur} msg={Msg}", reqName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
