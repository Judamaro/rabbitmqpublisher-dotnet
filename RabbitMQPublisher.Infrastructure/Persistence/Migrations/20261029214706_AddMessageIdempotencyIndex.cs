using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabbitMQPublisher.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Idempotency: rejects messages with the same routing key, type and payload. The payload is compared through
    /// its canonical jsonb text (sorted keys, normalized whitespace), hashed with md5 to keep index entries small.
    /// EF Core cannot model expression indexes, so the index is created with raw SQL.
    /// </summary>
    public partial class AddMessageIdempotencyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_Message_Content" ON "Message" ("RoutingKey", "Type", md5("Payload"::text));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX "UX_Message_Content";
                """);
        }
    }
}
