namespace RabbitMQPublisher.App.Extensions;

/// <summary>
/// Registers the services of the presentation (API) layer.
/// </summary>
internal static class PresentationServiceCollectionExtensions
{
    /// <summary>Adds controllers, petition responses, exception handling and Swagger to the container.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse;
                options.SuppressMapClientErrors = true;
            });

        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RabbitMQ Publisher API",
                Version = "v1",
                Description = "Stores messages in PostgreSQL and publishes them to RabbitMQ."
            });

            IncludeXmlComments(options, Assembly.GetExecutingAssembly());
            IncludeXmlComments(options, typeof(IMessageService).Assembly);
        });

        return services;
    }

    /// <summary>
    /// Creates the response for requests that cannot be bound, such as a malformed JSON body.
    /// The validation errors, grouped by field, are returned as the result.
    /// </summary>
    private static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        Dictionary<string, string[]> errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return PetitionResponse.Fail(ResponseModules.RequestValidation, "One or more validation errors occurred.", StatusCodes.Status400BadRequest, new Dictionary<string, object?> { ["errors"] = errors }).ToActionResult();
    }

    private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
    {
        string path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path);
        }
    }
}
