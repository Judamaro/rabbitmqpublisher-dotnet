# Build stage: restore and publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is cached while only source code changes.
COPY RabbitMQPublisher.Domain/RabbitMQPublisher.Domain.csproj RabbitMQPublisher.Domain/
COPY RabbitMQPublisher.Application/RabbitMQPublisher.Application.csproj RabbitMQPublisher.Application/
COPY RabbitMQPublisher.Infrastructure/RabbitMQPublisher.Infrastructure.csproj RabbitMQPublisher.Infrastructure/
COPY RabbitMQPublisher.App/RabbitMQPublisher.App.csproj RabbitMQPublisher.App/
RUN dotnet restore RabbitMQPublisher.App/RabbitMQPublisher.App.csproj

COPY RabbitMQPublisher.Domain/ RabbitMQPublisher.Domain/
COPY RabbitMQPublisher.Application/ RabbitMQPublisher.Application/
COPY RabbitMQPublisher.Infrastructure/ RabbitMQPublisher.Infrastructure/
COPY RabbitMQPublisher.App/ RabbitMQPublisher.App/
RUN dotnet publish RabbitMQPublisher.App/RabbitMQPublisher.App.csproj -c Release -o /app/publish --no-restore

# Runtime stage: run the API as the image's non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# curl is used by the Docker healthcheck (GET /health).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RabbitMQPublisher.App.dll"]
