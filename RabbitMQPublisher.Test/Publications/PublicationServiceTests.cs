namespace RabbitMQPublisher.Test.Publications;

public sealed class PublicationServiceTests
{
    private static readonly PublicationRequest ValidRequest = new("orders.created.queue", "orders.created", "orders");

    private readonly IPublicationRepository _publicationRepository = Substitute.For<IPublicationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMessageBroker _messageBroker = Substitute.For<IMessageBroker>();
    private readonly PublicationService _service;

    public PublicationServiceTests()
    {
        _service = new PublicationService(
            _publicationRepository,
            _unitOfWork,
            _messageBroker,
            new PublicationRequestValidator(),
            NullLogger<PublicationService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_DeclaresTopologyAndStoresPublication()
    {
        Result<PublicationResponse> result = await _service.CreateAsync(ValidRequest);

        Assert.True(result.IsSuccess);
        await _messageBroker.Received(1).DeclareTopologyAsync("orders", "orders.created.queue", "orders.created", Arg.Any<CancellationToken>());
        _publicationRepository.Received(1).Add(Arg.Is<Publication>(publication => publication.RoutingKey == "orders.created"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatedRoutingKey_ReturnsConflict()
    {
        _publicationRepository.RoutingKeyExistsAsync("orders.created", null, Arg.Any<CancellationToken>()).Returns(true);

        Result<PublicationResponse> result = await _service.CreateAsync(ValidRequest);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _messageBroker.DidNotReceiveWithAnyArgs().DeclareTopologyAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task CreateAsync_WithReservedExchange_ReturnsValidationError()
    {
        Result<PublicationResponse> result = await _service.CreateAsync(ValidRequest with { Exchange = "amq.direct" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WhenBrokerFails_DoesNotStorePublication()
    {
        _messageBroker.DeclareTopologyAsync(default!, default!, default!, default)
            .ThrowsAsyncForAnyArgs(new MessageBrokerException("Broker down"));

        Result<PublicationResponse> result = await _service.CreateAsync(ValidRequest);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        _publicationRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingPublication_UpdatesIt()
    {
        Publication publication = Publication.Create("old.queue", "old.key", "old");
        _publicationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(publication);

        Result<PublicationResponse> result = await _service.UpdateAsync(1, ValidRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal("orders.created", publication.RoutingKey);
        await _publicationRepository.Received(1).RoutingKeyExistsAsync("orders.created", 1, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownPublication_ReturnsNotFound()
    {
        Result<PublicationResponse> result = await _service.UpdateAsync(99, ValidRequest);

        Assert.True(result.IsFailure);
        Assert.Equal("Publication.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingPublication_RemovesIt()
    {
        Publication publication = Publication.Create("orders.created.queue", "orders.created", "orders");
        _publicationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(publication);

        Result result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _publicationRepository.Received(1).Remove(publication);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownPublication_ReturnsNotFound()
    {
        Result<PublicationResponse> result = await _service.GetByIdAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
