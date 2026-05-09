using MediatR;
using FluentValidation;

namespace JobService.Application.Behaviours;

// Registered as a pipeline behaviour in DI.
// Runs BEFORE every IRequestHandler.
// If validation fails, handler never executes.
public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> _validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(ctx))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}