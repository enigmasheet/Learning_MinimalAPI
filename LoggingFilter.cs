using System.Diagnostics;

public class LoggingFilter(ILogger<LoggingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint()?.DisplayName;
        logger.LogInformation("Executing: {Endpoint}", endpoint);

        var stopwatch = Stopwatch.StartNew();
        var result = await next(context);
        stopwatch.Stop();

        logger.LogInformation("Completed: {Endpoint} in {Duration}ms",
            endpoint, stopwatch.ElapsedMilliseconds);

        return result;
    }
}