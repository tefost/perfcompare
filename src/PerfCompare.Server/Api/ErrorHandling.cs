using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using PerfCompare.Contracts;

namespace PerfCompare.Server.Api;

public static class ErrorHandling
{
    public const string Category = "PerfCompare.Server";

    public static IApplicationBuilder UseJsonErrors(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errors => errors.Run(WriteErrorAsync));
    }

    private static async Task WriteErrorAsync(HttpContext context)
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(Category);
        if (feature is not null)
        {
            logger.LogError(feature.Error, "Помилка під час обробки запиту {Path}", feature.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(
            context.Response.Body, ErrorsResponse.Of("внутрішня помилка сервера"), ContractJson.Options);
    }
}
