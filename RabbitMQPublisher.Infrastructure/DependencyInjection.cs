namespace RabbitMQPublisher.Infrastructure;

/// <summary>
/// Registers the services of the infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Name of the PostgreSQL connection string.</summary>
    public const string ConnectionStringName = "PublisherMessage";

    /// <summary>Maximum time a dependency health check may take before it is reported as unhealthy.</summary>
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Adds persistence, messaging, health check and time services to the container.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        return services
            .AddPersistence(configuration)
            .AddMessaging()
            .AddDependencyHealthChecks();
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"The connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IErrorRepository, ErrorRepository>();
        services.AddScoped<IPublicationRepository, PublicationRepository>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddOptions<RabbitMQOptions>()
            .BindConfiguration(RabbitMQOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRabbitMQConnectionProvider, RabbitMQConnectionProvider>();
        services.AddSingleton<IMessageBroker, RabbitMQMessageBroker>();
        services.AddHostedService<RabbitMQConnectionInitializer>();

        return services;
    }

    private static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgresql", tags: [HealthCheckTags.Ready])
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: [HealthCheckTags.Ready], timeout: HealthCheckTimeout);

        return services;
    }
}
