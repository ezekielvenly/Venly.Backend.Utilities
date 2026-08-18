using System.Net;
using FluentValidation;
using MediatR;
using Venly.Backend.Common;
using Venly.Backend.Common.Pipelines;

namespace Venly.Backend.Utilities.Tests.Pipelines;

public record SampleRequest(string Value) : IRequest<RequestResponse<string>>;

public class SampleRequestValidator : AbstractValidator<SampleRequest>
{
    public SampleRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
    }
}

public class ValidatePipelineBehaviourTests
{
    [Fact]
    public async Task Handle_returns_validation_error_without_calling_next_when_invalid()
    {
        var behaviour = new ValidatePipelineBehaviour<SampleRequest, RequestResponse<string>>(
            new[] { new SampleRequestValidator() });
        var nextCalled = false;

        var result = await behaviour.Handle(
            new SampleRequest(""),
            _ => { nextCalled = true; return Task.FromResult(new RequestResponse<string>()); },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
        Assert.Contains("Value is required.", result.Errors!);
    }

    [Fact]
    public async Task Handle_calls_next_when_valid()
    {
        var behaviour = new ValidatePipelineBehaviour<SampleRequest, RequestResponse<string>>(
            new[] { new SampleRequestValidator() });

        var result = await behaviour.Handle(
            new SampleRequest("ok"),
            _ => Task.FromResult(new RequestResponse<string> { ResponseCode = 200 }),
            CancellationToken.None);

        Assert.Equal(200, result.ResponseCode);
    }
}
