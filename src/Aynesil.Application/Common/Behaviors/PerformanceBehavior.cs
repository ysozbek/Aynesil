using System.Diagnostics;
using Aynesil.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aynesil.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs a warning when a request exceeds the threshold.
/// Default threshold: 500ms. Adjust per environment via configuration if needed.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "Long-running request: {RequestName} ({ElapsedMs}ms) User={UserId}",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                _currentUserService.UserId);
        }

        return response;
    }
}
