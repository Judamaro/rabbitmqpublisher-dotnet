namespace RabbitMQPublisher.Application.Abstractions.Messaging;

/// <summary>
/// Exception raised by <see cref="IMessageBroker"/> implementations when the broker operation fails.
/// </summary>
public sealed class MessageBrokerException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="MessageBrokerException"/> class.</summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">The exception raised by the broker client.</param>
    public MessageBrokerException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
