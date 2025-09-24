using MediatR;

namespace Opas.Application.Diagnostics;

public sealed class PingHandler : IRequestHandler<PingQuery, string>
{
    public Task<string> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(request.Name) ? "OPAS" : request.Name.Trim();
        return Task.FromResult($"pong from Application, hello {name}");
    }
}
