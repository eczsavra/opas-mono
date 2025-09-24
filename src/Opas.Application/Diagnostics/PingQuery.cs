using MediatR;

namespace Opas.Application.Diagnostics;

public sealed record PingQuery(string Name) : IRequest<string>;