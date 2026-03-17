using JobNSharp.Utils.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace JobNSharp.Utils;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var (statusCode, message) = exception switch
        {
            CrawlException crawlEx => (StatusCodes.Status502BadGateway, crawlEx.Message),
            TaskCanceledException => (StatusCodes.Status408RequestTimeout, "Request timed out"),
            HttpRequestException httpEx => (StatusCodes.Status502BadGateway, $"External request failed: {httpEx.Message}"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message }, ct);
        return true;
    }
}
