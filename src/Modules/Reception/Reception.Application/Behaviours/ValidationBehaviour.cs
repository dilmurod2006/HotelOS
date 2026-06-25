using FluentValidation;
using MediatR;
using HotelOS.SharedKernel.Common;

namespace Reception.Application.Behaviours;

/// <summary>
/// MediatR Pipeline Behaviour: Validation.
///
/// This is the PROCEDURAL PROGRAMMING demonstration (BTEC Task 2.2):
/// A fixed sequence of steps executed in strict order for every command:
///   Step 1: Collect all registered validators for the request type.
///   Step 2: If none exist, skip validation and pass through.
///   Step 3: Run all validators in parallel (ValidateAsync).
///   Step 4: Aggregate any failures from all validators.
///   Step 5: If failures exist → return a validation error Result without
///            calling the handler at all.
///   Step 6: If no failures → call next() to proceed to the handler.
///
/// Placing this logic in a pipeline behaviour means it runs AUTOMATICALLY
/// for every IRequest — no handler ever needs to repeat validation code.
/// This is the Open/Closed Principle in practice: add new commands and they
/// are automatically validated without modifying this class.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Step 1–2: Short-circuit if no validators are registered for this command
        if (!_validators.Any())
            return await next();

        // Step 3: Run all validators concurrently
        var validationTasks = _validators
            .Select(v => v.ValidateAsync(request, cancellationToken));

        var validationResults = await Task.WhenAll(validationTasks);

        // Step 4: Aggregate failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        // Step 5: Short-circuit with validation error
        if (failures.Count > 0)
        {
            var errorMessages = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // We need to return a Result<T> failure — use reflection to construct
            // the appropriate Result type without hard-coupling to a specific return type.
            // This works because all commands return Result<T> or Result.
            if (TryCreateFailureResult<TResponse>(errorMessages, out var failureResult))
                return failureResult!;

            // Fallback for non-Result response types (should not occur in this system)
            throw new ValidationException(failures);
        }

        // Step 6: All validation passed — proceed to the actual handler
        return await next();
    }

    private static bool TryCreateFailureResult<T>(string errorMessage, out T? result)
    {
        result = default;
        var responseType = typeof(T);

        // Handle Result<TValue> generic type
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var error = new Error("Validation.Failed", errorMessage);
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), [typeof(Error)])!;

            result = (T)failureMethod.Invoke(null, [error])!;
            return true;
        }

        // Handle non-generic Result
        if (responseType == typeof(Result))
        {
            result = (T)(object)Result.Failure(new Error("Validation.Failed", errorMessage));
            return true;
        }

        return false;
    }
}
