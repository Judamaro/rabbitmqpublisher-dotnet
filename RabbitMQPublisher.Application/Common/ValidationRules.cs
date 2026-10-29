namespace RabbitMQPublisher.Application.Common;

/// <summary>
/// Reusable validation rules shared by several validators.
/// </summary>
internal static class ValidationRules
{
    /// <summary>Maximum length of RabbitMQ names (exchanges, queues and routing keys), in characters.</summary>
    public const int BrokerNameMaxLength = 255;

    /// <summary>Validates that a string is a non-empty RabbitMQ name.</summary>
    /// <typeparam name="T">Type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options.</returns>
    public static IRuleBuilderOptions<T, string> BrokerName<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder.NotEmpty().MaximumLength(BrokerNameMaxLength);

    /// <summary>Validates that a string contains well-formed JSON.</summary>
    /// <typeparam name="T">Type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options.</returns>
    public static IRuleBuilderOptions<T, string> Json<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder.Must(BeValidJson).WithMessage("'{PropertyName}' must be a valid serialized JSON.");

    private static bool BeValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            using JsonDocument _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
