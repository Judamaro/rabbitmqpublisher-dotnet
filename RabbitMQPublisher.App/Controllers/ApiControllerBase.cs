namespace RabbitMQPublisher.App.Controllers;

/// <summary>
/// Base controller that translates <see cref="Result"/> values into <see cref="PetitionResponse"/> responses.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Gets the module or service reported in the responses of the controller.</summary>
    protected abstract string Module { get; }

    /// <summary>
    /// Creates the response of an operation without return information.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    /// <param name="successMessage">Message returned when the operation succeeds.</param>
    /// <returns>The petition response.</returns>
    protected ObjectResult Respond(Result result, string successMessage)
        => result.IsSuccess ? PetitionResponse.Ok(Module, successMessage).ToActionResult() : Failure(result.Error);

    /// <summary>
    /// Creates the response of an operation that returns a value.
    /// </summary>
    /// <typeparam name="TValue">Type of the returned value.</typeparam>
    /// <param name="result">Result of the operation.</param>
    /// <param name="successMessage">Message returned when the operation succeeds.</param>
    /// <param name="successStatusCode">HTTP status code returned when the operation succeeds.</param>
    /// <returns>The petition response.</returns>
    protected ObjectResult Respond<TValue>(Result<TValue> result, string successMessage, int successStatusCode = StatusCodes.Status200OK)
        => result.IsSuccess ? PetitionResponse.Ok(Module, successMessage, result.Value, successStatusCode).ToActionResult() : Failure(result.Error);

    /// <summary>
    /// Creates the response of a failed operation. The error metadata, when present, is returned as the result.
    /// </summary>
    private ObjectResult Failure(ResultError error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        return PetitionResponse.Fail(Module, error.Description, statusCode, error.Metadata).ToActionResult();
    }
}
