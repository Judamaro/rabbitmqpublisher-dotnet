namespace RabbitMQPublisher.App.Extensions;

/// <summary>
/// Middleware configuration that keeps every response in the <see cref="PetitionResponse"/> format.
/// </summary>
internal static class PetitionResponseApplicationBuilderExtensions
{
    /// <summary>
    /// Writes a <see cref="PetitionResponse"/> body for error status codes produced without a body,
    /// such as unknown routes (404), unsupported methods (405) or unsupported media types (415).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder, for chaining.</returns>
    public static IApplicationBuilder UsePetitionResponseStatusCodePages(this IApplicationBuilder app) =>
        app.UseStatusCodePages(async context =>
        {
            int statusCode = context.HttpContext.Response.StatusCode;
            string message = ReasonPhrases.GetReasonPhrase(statusCode);
            PetitionResponse response = PetitionResponse.Fail(ResponseModules.Api, string.IsNullOrEmpty(message) ? "The request could not be processed." : $"{message}.", statusCode);

            await context.HttpContext.Response.WriteAsJsonAsync(response, context.HttpContext.RequestAborted);
        });
}
