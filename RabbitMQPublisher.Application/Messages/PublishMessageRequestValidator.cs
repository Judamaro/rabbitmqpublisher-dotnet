namespace RabbitMQPublisher.Application.Messages;

/// <summary>
/// Validates <see cref="PublishMessageRequest"/>.
/// </summary>
internal sealed class PublishMessageRequestValidator : AbstractValidator<PublishMessageRequest>
{
    /// <summary>Maximum length of the message type, in characters.</summary>
    public const int TypeMaxLength = 200;

    /// <summary>Initializes a new instance of the <see cref="PublishMessageRequestValidator"/> class.</summary>
    public PublishMessageRequestValidator()
    {
        RuleFor(request => request.Payload).Json();
        RuleFor(request => request.RoutingKey).BrokerName();
        RuleFor(request => request.Type).NotEmpty().MaximumLength(TypeMaxLength);
    }
}
