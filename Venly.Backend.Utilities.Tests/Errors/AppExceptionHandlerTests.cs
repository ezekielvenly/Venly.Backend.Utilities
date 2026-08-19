using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Venly.Backend.Common.Errors;

namespace Venly.Backend.Utilities.Tests.Errors;

public class AppExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_writes_the_exceptions_own_status_and_message_for_an_AppException()
    {
        ProblemDetailsContext? captured = null;
        var problemDetailsService = A.Fake<IProblemDetailsService>();
        A.CallTo(() => problemDetailsService.TryWriteAsync(A<ProblemDetailsContext>._))
            .Invokes((ProblemDetailsContext ctx) => captured = ctx)
            .Returns(true);

        var handler = new AppExceptionHandler(problemDetailsService, NullLogger<AppExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        var exception = ForbiddenException.ResourceAccessDenied("NotificationTemplate");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(403, httpContext.Response.StatusCode);
        Assert.NotNull(captured);
        Assert.Equal(403, captured!.ProblemDetails.Status);
        Assert.Equal("ForbiddenException", captured.ProblemDetails.Title);
        Assert.Contains("NotificationTemplate", captured.ProblemDetails.Detail);
        Assert.Equal("forbidden", captured.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public async Task TryHandleAsync_returns_a_generic_500_for_an_unmapped_exception_without_leaking_its_message()
    {
        ProblemDetailsContext? captured = null;
        var problemDetailsService = A.Fake<IProblemDetailsService>();
        A.CallTo(() => problemDetailsService.TryWriteAsync(A<ProblemDetailsContext>._))
            .Invokes((ProblemDetailsContext ctx) => captured = ctx)
            .Returns(true);

        var handler = new AppExceptionHandler(problemDetailsService, NullLogger<AppExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("connection string contains a password");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, httpContext.Response.StatusCode);
        Assert.NotNull(captured);
        Assert.Equal(500, captured!.ProblemDetails.Status);
        Assert.DoesNotContain("password", captured.ProblemDetails.Detail);
        Assert.Equal("An unexpected error occurred.", captured.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_swallows_cancellation_from_an_aborted_request_without_writing_a_response()
    {
        var problemDetailsService = A.Fake<IProblemDetailsService>();
        var handler = new AppExceptionHandler(problemDetailsService, NullLogger<AppExceptionHandler>.Instance);

        using var cts = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext { RequestAborted = cts.Token };
        cts.Cancel();

        var handled = await handler.TryHandleAsync(httpContext, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        A.CallTo(() => problemDetailsService.TryWriteAsync(A<ProblemDetailsContext>._)).MustNotHaveHappened();
    }
}
