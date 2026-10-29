namespace RabbitMQPublisher.Test.Messages;

public sealed class MessageServiceTests
{
    private const string RoutingKey = "orders.created";
    private const string Payload = """{"orderId":1}""";

    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly IPublicationRepository _publicationRepository = Substitute.For<IPublicationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMessageBroker _messageBroker = Substitute.For<IMessageBroker>();
    private readonly FixedTimeProvider _timeProvider = new();
    private readonly MessageService _service;

    public MessageServiceTests()
    {
        _service = new MessageService(
            _messageRepository,
            _publicationRepository,
            _unitOfWork,
            _messageBroker,
            new PublishMessageRequestValidator(),
            _timeProvider,
            NullLogger<MessageService>.Instance);

        _publicationRepository.GetByRoutingKeyAsync(RoutingKey, Arg.Any<CancellationToken>())
            .Returns(Publication.Create("orders.created.queue", RoutingKey, "orders"));
    }

    [Fact]
    public async Task PublishAsync_WithValidRequest_StoresAndPublishesMessage()
    {
        Result<MessageResponse> result = await _service.PublishAsync(new PublishMessageRequest(Payload, RoutingKey, "OrderCreated"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Published);
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, result.Value.PublishedAt);
        _messageRepository.Received(1).Add(Arg.Is<Message>(message => message.Payload == Payload));
        await _messageBroker.Received(1).PublishAsync(
            Arg.Is<BrokerMessage>(message => message.Exchange == "orders" && message.RoutingKey == RoutingKey && message.Type == "OrderCreated"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("{\"unterminated\":")]
    public async Task PublishAsync_WithInvalidPayload_ReturnsValidationError(string payload)
    {
        Result<MessageResponse> result = await _service.PublishAsync(new PublishMessageRequest(payload, RoutingKey, "OrderCreated"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        _messageRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task PublishAsync_WithUnknownRoutingKey_ReturnsNotFound()
    {
        Result<MessageResponse> result = await _service.PublishAsync(new PublishMessageRequest(Payload, "unknown", "OrderCreated"));

        Assert.True(result.IsFailure);
        Assert.Equal("Publication.RoutingKeyNotFound", result.Error.Code);
        _messageRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task PublishAsync_WhenBrokerFails_KeepsMessageStoredAsNotPublished()
    {
        Message? storedMessage = null;
        _messageRepository.Add(Arg.Do<Message>(message => storedMessage = message));
        _messageBroker.PublishAsync(Arg.Any<BrokerMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MessageBrokerException("Broker down"));

        Result<MessageResponse> result = await _service.PublishAsync(new PublishMessageRequest(Payload, RoutingKey, "OrderCreated"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        Assert.Equal("Message.PublishFailed", result.Error.Code);
        Assert.NotNull(storedMessage);
        Assert.False(storedMessage.Published);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepublishAsync_WithPendingMessage_PublishesIt()
    {
        Message message = Message.Create(Payload, "OrderCreated", RoutingKey, DateTime.UtcNow);
        _messageRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(message);

        Result<MessageResponse> result = await _service.RepublishAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(message.Published);
        await _messageBroker.Received(1).PublishAsync(Arg.Any<BrokerMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepublishAsync_WithPublishedMessage_ReturnsConflict()
    {
        Message message = Message.Create(Payload, "OrderCreated", RoutingKey, DateTime.UtcNow);
        message.MarkAsPublished(DateTime.UtcNow);
        _messageRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(message);

        Result<MessageResponse> result = await _service.RepublishAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _messageBroker.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task RepublishAsync_WithUnknownMessage_ReturnsNotFound()
    {
        Result<MessageResponse> result = await _service.RepublishAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal("Message.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsProcessedAsync_WithPendingMessage_MarksItAsProcessed()
    {
        Message message = Message.Create(Payload, "OrderCreated", RoutingKey, DateTime.UtcNow);
        _messageRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(message);

        Result<MessageResponse> result = await _service.UpdateAsProcessedAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Processed);
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, result.Value.ProcessedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsProcessedAsync_WithProcessedMessage_ReturnsConflict()
    {
        Message message = Message.Create(Payload, "OrderCreated", RoutingKey, DateTime.UtcNow);
        message.MarkAsProcessed(DateTime.UtcNow);
        _messageRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(message);

        Result<MessageResponse> result = await _service.UpdateAsProcessedAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal("Message.AlreadyProcessed", result.Error.Code);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateAsProcessedAsync_WithUnknownMessage_ReturnsNotFound()
    {
        Result<MessageResponse> result = await _service.UpdateAsProcessedAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
