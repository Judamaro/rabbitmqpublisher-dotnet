namespace RabbitMQPublisher.Application;

/// <summary>
/// Registers the services of the application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds the application services and validators to the container.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddScoped<IPublicationService, PublicationService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
