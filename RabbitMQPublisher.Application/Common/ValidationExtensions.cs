namespace RabbitMQPublisher.Application.Common;

/// <summary>
/// Extensions that translate FluentValidation results into <see cref="ResultError"/>.
/// </summary>
internal static class ValidationExtensions
{
    /// <summary>
    /// Converts a failed validation into a <see cref="ErrorType.Validation"/> error whose metadata
    /// contains an <c>errors</c> entry with the messages grouped by property name.
    /// </summary>
    /// <param name="validationResult">The failed validation result.</param>
    /// <returns>The validation error.</returns>
    public static ResultError ToResultError(this ValidationResult validationResult)
    {
        Dictionary<string, string[]> errors = validationResult.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return ResultError.Validation("Validation.Failed", "One or more validation errors occurred.", new Dictionary<string, object?> { ["errors"] = errors });
    }
}
