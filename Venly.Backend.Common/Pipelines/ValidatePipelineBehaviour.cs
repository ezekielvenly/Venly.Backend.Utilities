using System.Net;
using FluentValidation;
using MediatR;

namespace Venly.Backend.Common.Pipelines;

public class ValidatePipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IRequestResponse, new()
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidatePipelineBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var response = new TResponse();
                response.Errors = failures.Select(x => x.ErrorMessage).ToList();
                response.ResponseMessage = ResponseMessages.GetValidationMessage();
                response.ResponseCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
        }

        return await next();
    }
}
