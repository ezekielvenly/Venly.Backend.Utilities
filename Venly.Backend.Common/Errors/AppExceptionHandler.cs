using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Venly.Backend.Common.Errors;

public sealed class AppExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is AppException appException)
        {
            LogException(appException, traceId);

            httpContext.Response.StatusCode = (int)appException.HttpStatusCode;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = (int)appException.HttpStatusCode,
                    Title = appException.GetType().Name,
                    Detail = appException.Message,
                    Extensions =
                    {
                        ["code"] = appException.Code,
                        ["traceId"] = traceId,
                    },
                },
            });
        }

        logger.LogError(exception, "Unhandled exception [{ExceptionType}] — {Message}",
            exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "InternalServerError",
                Detail = "An unexpected error occurred.",
                Extensions =
                {
                    ["code"] = "internalError",
                    ["traceId"] = traceId,
                },
            },
        });
    }

    private void LogException(AppException exception, string traceId)
    {
        if (string.IsNullOrEmpty(exception.CallerClass))
        {
            logger.LogError(exception, "Exception [{Code}] (trace {TraceId}) — {Message}",
                exception.Code, traceId, exception.Message);
            return;
        }

        logger.LogError(exception,
            "Exception [{Code}] thrown at {CallerClass}.{CallerMethod}:{CallerLine} (trace {TraceId}) — {Message}",
            exception.Code, exception.CallerClass, exception.CallerMethod, exception.CallerLine, traceId,
            exception.Message);
    }
}
