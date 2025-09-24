using System.Globalization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Opas.Application;
using Opas.Application.Common;
using Opas.Application.Diagnostics;
using Opas.Domain.ValueObjects;
using Opas.Shared;
using Opas.Shared.Validation;

namespace Opas.Api.Endpoints;

public static class CoreEndpoints
{
    public static IEndpointRouteBuilder MapCoreEndpoints(this IEndpointRouteBuilder app)
    {
        // Root & basic health
        app.MapGet("/", () => new { ok = true, service = "OPAS.Api minimal", culture = CultureInfo.CurrentCulture.Name });
        app.MapGet("/health", () => Results.Ok("healthy"));

        // Shared ping (Guard + Result)
        app.MapGet("/v1/ping", (string? name) =>
        {
            try
            {
                Guard.NotNullOrWhiteSpace(name, nameof(name));
                var result = Result<string>.Success($"Merhaba {name}!");
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                var result = Result<string>.Failure(ex.Message);
                return Results.BadRequest(result);
            }
        });

        // Feature flags
        app.MapGet("/v1/flags", (IConfiguration cfg) =>
        {
            var fm = cfg.GetSection("FeatureManagement").GetChildren()
                        .ToDictionary(c => c.Key, c => c.Value ?? "false");
            return Results.Ok(fm);
        });

        // CQRS ping (MediatR + FluentValidation)
        app.MapGet("/v1/app/ping", async (IMediator mediator, string? name) =>
        {
            try
            {
                var response = await mediator.Send(new PingQuery(name ?? ""));
                return Results.Ok(new { ok = true, message = response });
            }
            catch (FluentValidation.ValidationException ex)
            {
                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return Results.ValidationProblem(errors, statusCode: 400);
            }
        });

        // EMAIL validate
        app.MapGet("/v1/validate/email", ([FromQuery] string? value) =>
        {
            if (Email.TryCreate(value, out var email, out var err))
                return Results.Ok(new { ok = true, email = email!.Value });

            return Results.BadRequest(new { ok = false, error = err });
        });

        // MONEY validate (Invariant + CurrentCulture)
        app.MapGet("/v1/validate/money", ([FromQuery] string? amount, [FromQuery] string? currency) =>
        {
            if (string.IsNullOrWhiteSpace(amount))
                return Results.BadRequest(new { ok = false, error = "amount is required." });

            if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) &&
                !decimal.TryParse(amount, NumberStyles.Number, CultureInfo.CurrentCulture, out amt))
            {
                return Results.BadRequest(new { ok = false, error = "amount parse failed." });
            }

            if (Money.TryCreate(amt, currency, out var money, out var err))
                return Results.Ok(new { ok = true, money = money!.ToString(), amount = money.Amount, currency = money.Currency });

            return Results.BadRequest(new { ok = false, error = err });
        });

        return app;
    }
}
