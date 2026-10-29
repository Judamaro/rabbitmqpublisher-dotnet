namespace RabbitMQPublisher.Test.Errors;

public sealed class ErrorServiceTests
{
    private readonly IErrorRepository _errorRepository = Substitute.For<IErrorRepository>();
    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ErrorService _service;

    public ErrorServiceTests()
    {
        _service = new ErrorService(
            _errorRepository,
            _messageRepository,
            _unitOfWork,
            new AddErrorRequestValidator(),
            new FixedTimeProvider());
    }

    [Fact]
    public async Task AddAsync_WithExistingMessage_RegistersError()
    {
        _messageRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        Result<ErrorResponse> result = await _service.AddAsync(new AddErrorRequest(1, "Consumer failed"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MessageId);
        Assert.Equal(FixedTimeProvider.DefaultNow.UtcDateTime, result.Value.CreatedAt);
        _errorRepository.Received(1).Add(Arg.Is<Error>(error => error.MessageId == 1 && error.Text == "Consumer failed"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithUnknownMessage_ReturnsNotFound()
    {
        Result<ErrorResponse> result = await _service.AddAsync(new AddErrorRequest(99, "Consumer failed"));

        Assert.True(result.IsFailure);
        Assert.Equal("Message.NotFound", result.Error.Code);
        _errorRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData(0, "Consumer failed")]
    [InlineData(1, "")]
    public async Task AddAsync_WithInvalidRequest_ReturnsValidationError(int messageId, string text)
    {
        Result<ErrorResponse> result = await _service.AddAsync(new AddErrorRequest(messageId, text));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
