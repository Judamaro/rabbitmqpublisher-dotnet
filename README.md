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
| POST   | `/api/message/publish`                  | Stores and publishes `{ payload, routingKey, type }`; rejects duplicates |
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

Status codes: `200`/`201` success, `400` validation, `404` not found, `409` conflict (duplicate
message, already published/processed, duplicated routing key), `503` broker unavailable, `500` unexpected error.
When publishing fails the message stays stored with `published = false` and `result.messageId`
holds its id so it can be republished.

### Idempotency

Publish rejects a message whose **payload, type and routing key** match an existing message, at any
time. The payload is compared as JSON, so key order and whitespace do not matter. The response is
`409`, nothing is stored or published, and `result.messageId` holds the id of the existing message.
A unique index on the database (`UX_Message_Content`) guarantees it even for concurrent requests.

### Published message

Each message is published with this JSON body, where `Id` is the id the message got in the database
and `Payload` is the serialized JSON payload sent as a string:

```json
{"Id":7,"Payload":"{\"orderId\":7}","Type":"OrderCreated"}
```

Messages are published as persistent, `application/json`, with `messageId` and `type` properties,
`mandatory = true` and publisher confirms, so a successful response means RabbitMQ accepted and routed it.

## Sequence diagrams

The diagrams follow the services of the application layer (`MessageService`, `ErrorService` and
`PublicationService`). Every response is a `PetitionResponse` whose `statusCode` is shown on the
arrows back to the client.

### Message lifecycle

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL
    participant MQ as RabbitMQ
    participant Consumer

    Client->>API: POST /api/message/publish
    API->>DB: INSERT Message (Published = false)
    API->>MQ: Publish {Id, Payload, Type}
    MQ-->>API: Confirm
    API->>DB: UPDATE Published = true
    API-->>Client: 201 Created
    MQ->>Consumer: Deliver {Id, Payload, Type}
    alt Processed
        Consumer->>API: PATCH /api/message/{id}/updateasprocessed
        API->>DB: UPDATE Processed = true, ProcessedAt = now
    else Failed
        Consumer->>API: POST /api/error/add {messageId, text}
        API->>DB: INSERT Error
    end
```

### Publish

`POST /api/message/publish`

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL
    participant MQ as RabbitMQ

    Client->>API: POST /api/message/publish {payload, routingKey, type}
    alt Malformed body or invalid fields (payload is not JSON)
        API-->>Client: 400 Bad Request
    end
    API->>DB: SELECT Publication by RoutingKey
    alt No publication for the routing key
        API-->>Client: 404 Not Found
    end
    API->>DB: SELECT Message with same RoutingKey, Type and Payload (jsonb)
    alt Duplicate found
        API-->>Client: 409 Conflict (result.messageId = existing id)
    end
    API->>DB: INSERT Message (Published = false)
    alt UX_Message_Content violated by a concurrent request
        API->>DB: SELECT existing Message id
        API-->>Client: 409 Conflict (result.messageId = existing id)
    end
    API->>MQ: Publish {Id, Payload, Type} to exchange (mandatory, persistent)
    alt Broker unavailable or message not confirmed
        MQ--xAPI: Error
        API-->>Client: 503 Service Unavailable (result.messageId, stored as not published)
    else Confirmed
        MQ-->>API: Confirm
        API->>DB: UPDATE Published = true, PublishedAt = now
        API-->>Client: 201 Created (result = message)
    end
```

### Republish

`POST /api/message/{id}/republish`

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL
    participant MQ as RabbitMQ

    Client->>API: POST /api/message/{id}/republish
    API->>DB: SELECT Message by Id
    alt Message not found
        API-->>Client: 404 Not Found
    else Already published
        API-->>Client: 409 Conflict
    end
    API->>DB: SELECT Publication by RoutingKey
    alt No publication for the routing key
        API-->>Client: 404 Not Found
    end
    API->>MQ: Publish {Id, Payload, Type} to exchange
    alt Broker unavailable or message not confirmed
        MQ--xAPI: Error
        API-->>Client: 503 Service Unavailable (result.messageId)
    else Confirmed
        MQ-->>API: Confirm
        API->>DB: UPDATE Published = true, PublishedAt = now
        API-->>Client: 200 OK (result = message)
    end
```

### UpdateAsProcessed

`PATCH /api/message/{id}/updateasprocessed`

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL

    Client->>API: PATCH /api/message/{id}/updateasprocessed
    API->>DB: SELECT Message by Id
    alt Message not found
        API-->>Client: 404 Not Found
    else Already processed
        API-->>Client: 409 Conflict
    else Pending
        API->>DB: UPDATE Processed = true, ProcessedAt = now
        API-->>Client: 200 OK (result = message)
    end
```

### Add error

`POST /api/error/add`

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL

    Client->>API: POST /api/error/add {messageId, text}
    alt Malformed body or invalid fields
        API-->>Client: 400 Bad Request
    end
    API->>DB: SELECT EXISTS Message by Id
    alt Message not found
        API-->>Client: 404 Not Found
    else Message exists
        API->>DB: INSERT Error (CreatedAt = now)
        API-->>Client: 201 Created (result = error)
    end
```

### Create publication

`POST /api/publication`. Updating a publication (`PUT /api/publication/{id}`) follows the same steps
after checking that it exists (`404`), and keeps the previously declared broker objects.

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as Publisher API
    participant DB as PostgreSQL
    participant MQ as RabbitMQ

    Client->>API: POST /api/publication {queue, routingKey, exchange}
    alt Malformed body or invalid fields
        API-->>Client: 400 Bad Request
    end
    API->>DB: SELECT EXISTS Publication by RoutingKey
    alt Routing key already used
        API-->>Client: 409 Conflict
    end
    API->>MQ: Declare durable direct exchange
    API->>MQ: Declare durable queue
    API->>MQ: Bind queue to exchange with the routing key
    alt Broker unavailable or declaration rejected
        MQ--xAPI: Error
        API-->>Client: 503 Service Unavailable (nothing stored)
    else Topology declared
        API->>DB: INSERT Publication
        API-->>Client: 201 Created (result = publication)
    end
```

## Health checks

| Endpoint            | Checks                                   | Used by                          |
|---------------------|------------------------------------------|----------------------------------|
| `GET /health`       | The API is running (no dependencies)     | Docker/Coolify healthcheck       |
| `GET /health/ready` | PostgreSQL and RabbitMQ are reachable    | Monitoring and manual checks     |

Both answer with a `PetitionResponse` (module `HealthCheck`) whose `result` holds the overall status
and, for `/health/ready`, the status of each dependency: `200` when healthy, `503` when unhealthy.

The Docker healthcheck in both compose files calls `/health` with `curl` (installed in the image)
every 30 seconds. It deliberately ignores the dependencies: if RabbitMQ or PostgreSQL were down the
container would become unhealthy and the proxy would stop routing to the API, although it can still
store messages and republish them later. Successful health requests are not written to the logs.

## Deploying to Coolify

Use the Docker Compose build pack with `docker-compose.yml`. It only contains the API, so PostgreSQL
and RabbitMQ are external resources. Coolify lists every `${...}` reference in the **Environment
Variables** tab: the ones written as `${VAR:?}` are required, and Coolify highlights them and blocks
the deployment until they have a value. The rest have defaults.

| Variable                                   | Required | Default              | Setting                                          |
|--------------------------------------------|----------|----------------------|--------------------------------------------------|
| `CONNECTIONSTRINGS_PUBLISHER_MESSAGE`      | Yes      |                      | `ConnectionStrings:PublisherMessage` (key/value or `postgres://` URL) |
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

The RabbitMQ management UI is at `http://localhost:15672` (guest / guest).

## Configuration

`appsettings.json`:

- `ConnectionStrings:PublisherMessage`: PostgreSQL connection string. `appsettings.json` holds the
  `ConnectionStrings_Postgres_PublisherMessage` placeholder, injected at deploy time (in Coolify
  through `CONNECTIONSTRINGS_PUBLISHER_MESSAGE`, see [Deploying to Coolify](#deploying-to-coolify)).
  Both formats are accepted: `Host=...;Port=5432;Database=...;Username=...;Password=...` and the URL
  shown by Coolify, `postgres://user:password@host:5432/database` (URL-encoded values are decoded and
  query parameters such as `sslmode=require` are applied). The value is validated at startup: an
  invalid or missing value stops the API with exit code `1` and a message that never includes the
  password.
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
