namespace RabbitMQPublisher.Application.Abstractions.Persistence;

/// <summary>
/// Exception raised by <see cref="IUnitOfWork.SaveChangesAsync"/> when the changes violate a unique constraint,
/// for example when a concurrent request stored the same data first.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="UniqueConstraintViolationException"/> class.</summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">The exception raised by the database provider.</param>
    public UniqueConstraintViolationException(string message, Exception? innerException = null) : base(message, innerException) { }
}
