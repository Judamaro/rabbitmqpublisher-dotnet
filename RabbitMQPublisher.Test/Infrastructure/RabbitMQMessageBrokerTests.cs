namespace RabbitMQPublisher.Test.Infrastructure;

public sealed class RabbitMQMessageBrokerTests
{
    private const string Payload = """{"orderId":7,"total":99.5}""";

    private static readonly BrokerMessage OrderCreatedMessage = new("orders", "orders.created", 7, "OrderCreated", Payload);

    // Same settings the consumer uses to read messages (JsonSerializerDefaults.Web).
    private static readonly JsonSerializerOptions ConsumerJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SerializeBody_ContainsIdPayloadAndType()
    {
        byte[] body = RabbitMQMessageBroker.SerializeBody(OrderCreatedMessage);

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        Assert.Equal(["Id", "Payload", "Type"], root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(7, root.GetProperty("Id").GetInt32());
        Assert.Equal(JsonValueKind.String, root.GetProperty("Payload").ValueKind);
        Assert.Equal(Payload, root.GetProperty("Payload").GetString());
        Assert.Equal("OrderCreated", root.GetProperty("Type").GetString());
    }

    [Fact]
    public void SerializeBody_EscapesPayloadQuotesWithBackslash()
    {
        string body = Encoding.UTF8.GetString(RabbitMQMessageBroker.SerializeBody(OrderCreatedMessage));

        Assert.Equal("""{"Id":7,"Payload":"{\"orderId\":7,\"total\":99.5}","Type":"OrderCreated"}""", body);
    }

    [Fact]
    public void SerializeBody_IsReadableWithTheConsumerContract()
    {
        byte[] body = RabbitMQMessageBroker.SerializeBody(OrderCreatedMessage);

        ConsumerMessageDto? consumerMessage = JsonSerializer.Deserialize<ConsumerMessageDto>(body, ConsumerJsonOptions);

        Assert.Equal(new ConsumerMessageDto(7, Payload), consumerMessage);
    }

    /// <summary>Shape of the message read by rabbitmqconsumer-dotnet (<c>MessageDto</c>).</summary>
    private sealed record ConsumerMessageDto(int Id, string? Payload);
}
