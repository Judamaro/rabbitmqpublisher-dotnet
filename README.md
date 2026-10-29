# rabbitmqpublisher-dotnet

.NET 10 Web API that acts as a **RabbitMQ publisher**. Every message is stored in PostgreSQL (EF Core)
before being published, so it can be republished if the broker fails, and consumers can report it as
processed or register errors for it.

## Architecture

Clean Architecture, with dependencies pointing inwards:

```
rabbitmqpublisher-dotnet
├── .vscode/                          Build/test tasks and debug configuration
├── RabbitMQPublisher.App/            Controllers, PetitionResponse envelope, global exception handler, Swagger
├── RabbitMQPublisher.Application/    Use cases (services), DTOs, FluentValidation validators,
│                                     ports: repositories, IUnitOfWork, IMessageBroker
├── RabbitMQPublisher.Domain/         Entities (Message, Error, Publication), Result pattern, error catalogs
├── RabbitMQPublisher.Infrastructure/ EF Core + PostgreSQL (DbContext, repositories, migrations),
│                                     RabbitMQ adapter (RabbitMQ.Client 7)
├── RabbitMQPublisher.Test/           xUnit + NSubstitute
├── .dockerignore
├── .gitignore
├── docker-compose.yml                Coolify deployment (API only, required variables)
├── docker-compose.local.yml          Local development (API + PostgreSQL + RabbitMQ)
├── Dockerfile
├── RabbitMQPublisher.slnx
└── README.md
```

Design patterns: Repository, Unit of Work, Service layer, Result, Adapter (`IMessageBroker`),
Singleton connection provider, Options (validated on start), DTO mapping, DI extension methods.

## Data model

| Table         | Columns                                                                                  |
|---------------|------------------------------------------------------------------------------------------|
| `Message`     | Id, Payload (`jsonb`), Type, PublishedAt, Published, ProcessedAt, Processed, RoutingKey   |
| `Error`       | Id, MessageId (FK → Message), Text, CreatedAt                                            |
| `Publication` | Id, Queue, RoutingKey (**unique**), Exchange                                             |

A `Publication` is the routing configuration: the routing key of a message selects the publication,
which defines the exchange the message is published to. Creating or updating a publication declares
a durable direct exchange, a durable queue and their binding in RabbitMQ.

`PublishedAt` is not nullable: it holds the creation date while the message is pending and the
broker confirmation date once published. Dates are stored in UTC.

## Endpoints

| Method | Route                                   | Description                                                              |
|--------|-----------------------------------------|--------------------------------------------------------------------------|
| POST   | `/api/message/publish`                  | Stores and publishes `{ payload, routingKey, type }`                     |
| POST   | `/api/message/{id}/republish`           | Republishes a stored message whose `published` is `false`                |
| PATCH  | `/api/message/{id}/updateasprocessed`   | Sets `processed = true` and `processedAt` to now                         |
| POST   | `/api/error/add`                        | Registers `{ messageId, text }` for an existing message                  |
| GET    | `/api/publication`                      | Lists publications                                                       |
| GET    | `/api/publication/{id}`                 | Gets a publication                                                       |
| POST   | `/api/publication`                      | Creates `{ queue, routingKey, exchange }` and declares it in RabbitMQ    |
| PUT    | `/api/publication/{id}`                 | Updates a publication and declares the new topology                      |
| DELETE | `/api/publication/{id}`                 | Deletes a publication                                                    |

### Response format

Every response, successful or not, is a `PetitionResponse`, and its `statusCode` is also the HTTP
status code of the response:

```json
{
  "success": true,
  "message": "Message published successfully.",
  "module": "MessageService",
  "result": { "id": 1, "payload": "{\"orderId\":1}", "type": "OrderCreated", "...": "..." },
  "statusCode": 201
}
```

| Property     | Description                                                                                   |
|--------------|-----------------------------------------------------------------------------------------------|
| `success`    | Whether the petition was processed successfully                                               |
| `message`    | Message describing the processing                                                             |
| `module`     | Module or service that generated the message: `MessageService`, `ErrorService`, `PublicationService`, `RequestValidation` (malformed body), `GlobalExceptionHandler` (unexpected errors) or `Api` (unknown route, method or media type) |
| `result`     | Returned information, or `null` when there is none. On failures it holds error details when available, such as `errors` for validation failures |
| `statusCode` | HTTP status code                                                                              |

Status codes: `200`/`201` success, `400` validation, `404` not found, `409` conflict (already
published/processed, duplicated routing key), `503` broker unavailable, `500` unexpected error.
When publishing fails the message stays stored with `published = false` and `result.messageId`
holds its id so it can be republished.

### Published message

Each message is published with this JSON body, where `Id` is the id the message got in the database
and `Payload` is the serialized JSON payload sent as a string:

```json
{"Id":7,"Payload":"{\"orderId\":7}","Type":"OrderCreated"}
```

Messages are published as persistent, `application/json`, with `messageId` and `type` properties,
`mandatory = true` and publisher confirms, so a successful response means RabbitMQ accepted and routed it.

## Deploying to Coolify

Use the Docker Compose build pack with `docker-compose.yml`. It only contains the API, so PostgreSQL
and RabbitMQ are external resources. Coolify lists every `${...}` reference in the **Environment
Variables** tab: the ones written as `${VAR:?}` are required, and Coolify highlights them and blocks
the deployment until they have a value. The rest have defaults.

| Variable                                   | Required | Default              | Setting                                          |
|--------------------------------------------|----------|----------------------|--------------------------------------------------|
| `CONNECTIONSTRINGS_PUBLISHER_MESSAGE`      | Yes      |                      | `ConnectionStrings:PublisherMessage`             |
| `RABBITMQ_HOST`                            | Yes      |                      | `RabbitMQOptions:Host`                           |
| `RABBITMQ_USER`                            | Yes      |                      | `RabbitMQOptions:User`                           |
| `RABBITMQ_PASSWORD`                        | Yes      |                      | `RabbitMQOptions:Pwd`                            |
| `RABBITMQ_CLIENT_NAME`                     | No       | `rabbitmq-publisher` | `RabbitMQOptions:Name`                           |
| `RABBITMQ_PORT`                            | No       | `5672`               | `RabbitMQOptions:Port`                           |
| `RABBITMQ_VIRTUAL_HOST`                    | No       | `/`                  | `RabbitMQOptions:VirtualHost`                    |
| `RABBITMQ_PREFETCH_COUNT`                  | No       | `10`                 | `RabbitMQOptions:PrefetchCount`                  |
| `RABBITMQ_HEARTBEAT_SECONDS`               | No       | `30`                 | `RabbitMQOptions:HeartbeatSeconds`               |
| `RABBITMQ_NETWORK_RECOVERY_INTERVAL_SECONDS` | No     | `5`                  | `RabbitMQOptions:NetworkRecoveryIntervalSeconds` |
| `RABBITMQ_MAX_CONNECT_RETRY_DELAY_SECONDS` | No       | `60`                 | `RabbitMQOptions:MaxConnectRetryDelaySeconds`    |
| `DATABASE_APPLY_MIGRATIONS_ON_STARTUP`     | No       | `false`              | `Database:ApplyMigrationsOnStartup`              |
| `ASPNETCORE_ENVIRONMENT`                   | No       | `Production`         | Hosting environment                              |

The API listens on port `8080`.

## Running with Docker

Builds the API image and starts it together with PostgreSQL and RabbitMQ:

```bash
docker compose -f docker-compose.local.yml up -d --build
```

Swagger UI is available at `http://localhost:8080/swagger`.

## Running locally

Start only the dependencies:

```bash
docker compose -f docker-compose.local.yml up -d postgres rabbitmq
```

```bash
dotnet run --project RabbitMQPublisher.App
```

Or use the `RabbitMQPublisher.App` debug configuration in VS Code. In the Development environment
pending migrations are applied at startup (see `Database:ApplyMigrationsOnStartup`) and Swagger UI is available at `http://localhost:5245/swagger`.

The RabbitMQ management UI is at `http://localhost:15672` (guest / guest). Sample requests are in
`RabbitMQPublisher.App/RabbitMQPublisher.App.http`.

## Configuration

`appsettings.json`:

- `ConnectionStrings:PublisherMessage`: PostgreSQL connection string. `appsettings.json` holds the
  `ConnectionStrings_Postgres_PublisherMessage` placeholder, injected at deploy time (in Coolify
  through `CONNECTIONSTRINGS_PUBLISHER_MESSAGE`, see [Deploying to Coolify](#deploying-to-coolify)).
- `Database:ApplyMigrationsOnStartup`: applies pending EF Core migrations when the API starts
  (`false` in `appsettings.json`, `true` in `appsettings.Development.json`). Enable it in Coolify with
  `DATABASE_APPLY_MIGRATIONS_ON_STARTUP=true`.
- `RabbitMQOptions`: `Name` (connection name), `Host`, `User`, `Pwd`, `Port`, `VirtualHost`,
  `HeartbeatSeconds`, `NetworkRecoveryIntervalSeconds` (automatic recovery interval) and
  `MaxConnectRetryDelaySeconds` (cap of the exponential backoff used to establish the initial
  connection in the background, so the API starts even while the broker is down). `Queue` and
  `PrefetchCount` are part of the shared settings contract but are not used by the publisher:
  each message is routed through its `Publication`, and prefetch only applies to consumers.
  The `RabbitMQOptions_*` values are placeholders to be replaced per environment;
  `appsettings.Development.json` holds the local values.
- `Serilog`: logging configuration (levels, enrichers and sinks), read with `Serilog.Settings.Configuration`.
  Logs are written to the console and every HTTP request is logged by `UseSerilogRequestLogging`.

Environment variables override any value using `__` as separator, for example `RabbitMQOptions__Host`.

## Tests

```bash
dotnet test
```

## Migrations

Requires the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`).

```bash
dotnet ef migrations add <Name> --project RabbitMQPublisher.Infrastructure --startup-project RabbitMQPublisher.App --output-dir Persistence/Migrations
```
