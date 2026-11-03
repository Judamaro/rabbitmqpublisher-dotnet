namespace RabbitMQPublisher.Test.Infrastructure;

public sealed class PostgresConnectionStringTests
{
    private const string Name = "PublisherMessage";

    [Fact]
    public void Normalize_WithKeyValueFormat_KeepsTheValues()
    {
        string result = PostgresConnectionString.Normalize(Name, "Host=db;Port=5433;Database=publisher;Username=app;Password=secret");

        NpgsqlConnectionStringBuilder builder = new(result);
        Assert.Equal("db", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("publisher", builder.Database);
        Assert.Equal("app", builder.Username);
        Assert.Equal("secret", builder.Password);
    }

    [Theory]
    [InlineData("postgres://app:p%40ss%3Bword@db-host:5433/publisher")]
    [InlineData("postgresql://app:p%40ss%3Bword@db-host:5433/publisher")]
    public void Normalize_WithUrlFormat_ConvertsToKeyValueFormat(string url)
    {
        string result = PostgresConnectionString.Normalize(Name, url);

        NpgsqlConnectionStringBuilder builder = new(result);
        Assert.Equal("db-host", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("publisher", builder.Database);
        Assert.Equal("app", builder.Username);
        Assert.Equal("p@ss;word", builder.Password);
    }

    [Fact]
    public void Normalize_WithUrlWithoutPortAndWithQuery_UsesDefaultPortAndQueryParameters()
    {
        string result = PostgresConnectionString.Normalize(Name, "postgres://app:secret@db-host/publisher?sslmode=require");

        NpgsqlConnectionStringBuilder builder = new(result);
        Assert.Equal(5432, builder.Port);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Theory]
    [InlineData("ConnectionStrings_Postgres_PublisherMessage")]
    [InlineData("Port=5432;Database=publisher")]
    [InlineData("postgres:///publisher")]
    public void Normalize_WithInvalidValue_ThrowsWithoutExposingTheValue(string value)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Normalize(Name, value));

        Assert.Contains(Name, exception.Message);
        Assert.DoesNotContain(value, exception.Message);
    }
}
