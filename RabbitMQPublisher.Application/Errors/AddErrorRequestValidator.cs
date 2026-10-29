namespace RabbitMQPublisher.Application.Errors;

/// <summary>
/// Validates <see cref="AddErrorRequest"/>.
/// </summary>
internal sealed class AddErrorRequestValidator : AbstractValidator<AddErrorRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AddErrorRequestValidator"/> class.</summary>
    public AddErrorRequestValidator()
    {
        RuleFor(request => request.MessageId).GreaterThan(0);
        RuleFor(request => request.Text).NotEmpty();
    }
}
