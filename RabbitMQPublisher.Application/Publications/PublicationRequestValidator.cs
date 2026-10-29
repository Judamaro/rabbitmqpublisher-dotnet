namespace RabbitMQPublisher.Application.Publications;

/// <summary>
/// Validates <see cref="PublicationRequest"/>.
/// </summary>
internal sealed class PublicationRequestValidator : AbstractValidator<PublicationRequest>
{
    /// <summary>Initializes a new instance of the <see cref="PublicationRequestValidator"/> class.</summary>
    public PublicationRequestValidator()
    {
        RuleFor(request => request.Queue).BrokerName();
        RuleFor(request => request.RoutingKey).BrokerName();
        RuleFor(request => request.Exchange)
            .BrokerName()
            .Must(exchange => !exchange.StartsWith("amq.", StringComparison.Ordinal))
            .WithMessage("'{PropertyName}' cannot use the reserved 'amq.' prefix.");
    }
}
