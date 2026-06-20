using MediatR;
using Microsoft.Extensions.Logging;

namespace Aynesil.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs command/query names and execution time.
/// Structured logging keys: RequestName, RequestData (commands only — queries excluded for brevity).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
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
        var requestName = typeof(TRequest).Name;

        _logger.LogDebug("Handling {RequestName}", requestName);

        var response = await next();

        _logger.LogDebug("Handled {RequestName}", requestName);

        return response;
    }
}
