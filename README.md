<!-- README.md -->
# RxDBDotNet

[![NuGet Version](https://img.shields.io/nuget/v/RxDBDotNet.svg)](https://www.nuget.org/packages/RxDBDotNet/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/RxDBDotNet.svg)](https://www.nuget.org/packages/RxDBDotNet/)
[![codecov](https://codecov.io/github/Ziptility/RxDBDotNet/graph/badge.svg?token=VvuBJEsIHT)](https://codecov.io/github/Ziptility/RxDBDotNet)

RxDBDotNet is a powerful .NET library that implements the [RxDB replication protocol](https://rxdb.info/replication.html), enabling real-time data synchronization between RxDB clients and .NET servers using GraphQL and Hot Chocolate. It extends the standard RxDB replication protocol with .NET-specific enhancements.

## Table of Contents

- [RxDBDotNet](#rxdbdotnet)
  - [Table of Contents](#table-of-contents)
  - [Key Features](#key-features)
  - [Getting Started](#getting-started)
    - [Installation](#installation)
    - [Basic Usage](#basic-usage)
  - [Advanced Features](#advanced-features)
    - [Policy-Based Security](#policy-based-security)
    - [Subscription Topics](#subscription-topics)
    - [Custom Error Types](#custom-error-types)
    - [OpenID Connect (OIDC) Support for Subscription Authentication](#openid-connect-oidc-support-for-subscription-authentication)
  - [Example Application](#example-application)
    - [Prerequisites for Running the Example Application](#prerequisites-for-running-the-example-application)
    - [Running the Full Stack](#running-the-full-stack)
  - [RxDB Replication Protocol Details](#rxdb-replication-protocol-details)
  - [Custom Implementations for RxDB Clients](#custom-implementations-for-rxdb-clients)
  - [Security Considerations](#security-considerations)
    - [Server-Side Timestamp Overwriting](#server-side-timestamp-overwriting)
  - [Contributing](#contributing)
  - [License](#license)
  - [Code of Conduct](#code-of-conduct)
  - [Security](#security)
  - [Acknowledgments](#acknowledgments)

## Key Features

- 🔄 Full RxDB Protocol Support
- 🌶️ Hot Chocolate GraphQL Integration
- 🌐 Real-Time & Offline-First Capabilities
- ⚡ Quick Setup with Minimal Configuration
- 🧩 Extensible Design for Custom Types
- ⚠️ Structured Error Handling
- 🎯 Subscription Topic Filtering
- 🔒 ASP.NET Core Authorization Integration
- 🔍 GraphQL Filtering for Optimized Data Transfer
- 🚀 Actively Developed & Community-Driven

## Getting Started

### Installation

Install the RxDBDotNet package via NuGet:

```bash
dotnet add package RxDBDotNet
```

### Basic Usage

1. Implement `IReplicatedDocument` for your document type:

    ```csharp
    public class Workspace : IReplicatedDocument
    {
        public required Guid Id { get; init; }
        public required string Name { get; set; }
        public required DateTimeOffset UpdatedAt { get; set; }
        public required bool IsDeleted { get; set; }
        public List<string>? Topics { get; set; }
    }
    ```

2. Implement the document service:

    ```csharp
    public class WorkspaceService : IReplicatedDocumentService<Workspace>
    {
        // Implement the required methods
        // ...
    }
    ```

3. Configure services in `Program.cs`:

    ```csharp
    // Add your document service to the DI container
    builder.Services
        .AddSingleton<IReplicatedDocumentService<Workspace>, WorkspaceService>();

    // Configure the Hot Chocolate GraphQL server
    builder.Services
        .AddGraphQLServer()
         // Mutation conventions must be enabled for replication to work
        .AddMutationConventions()
        // Enable RxDBDotNet replication services
        .AddReplication()
        // Register the document to be replicated
        .AddReplicatedDocument<Workspace>()
        .AddInMemorySubscriptions();

    var app = builder.Build();

    app.UseWebSockets();
    app.MapGraphQL();

    app.Run();
    ```

## Advanced Features

### Policy-Based Security

RxDBDotNet supports policy-based security using the Microsoft.AspNetCore.Authorization infrastructure.

Configuration:

1. Define authorization policies:

    ```csharp
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("IsWorkspaceAdmin", policy =>
            policy.RequireClaim("WorkspaceRole", "Admin"));
        
        options.AddPolicy("CanReadWorkspace", policy =>
            policy.RequireClaim("WorkspaceRole", "Admin", "Reader"));
    });
    ```

2. Configure security options for documents:

    ```csharp
    builder.Services
        .AddGraphQLServer()
        .AddReplicatedDocument<Workspace>(options =>
        {
            options.Security = new SecurityOptions()
                .RequirePolicyToRead("CanReadWorkspace")
                .RequirePolicyToWrite("IsWorkspaceAdmin");
        });
    ```

### Subscription Topics

RxDBDotNet supports subscription topics for fine-grained control over real-time updates.

Usage:

1. Specify topics when creating or updating a document:

    ```csharp
    var liveDoc = new LiveDoc
    {
        Id = Guid.NewGuid(),
        Content = "New document content",
        UpdatedAt = DateTimeOffset.UtcNow,
        IsDeleted = false,
        WorkspaceId = workspaceId,
        Topics = new List<string> { $"workspace-{workspaceId}" }
    };

    await documentService.CreateAsync(liveDoc, CancellationToken.None);
    ```

2. Subscribe to specific topics:

    ```graphql
    subscription StreamLiveDocs {
      streamLiveDoc(topics: ["workspace-123e4567-e89b-12d3-a456-426614174000"]) {
        documents {
          id
          content
          updatedAt
          isDeleted
          workspaceId
        }
        checkpoint {
          updatedAt
          lastDocumentId
        }
      }
    }
    ```

### Custom Error Types

Configure custom error types to provide more detailed error information to clients:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddReplicatedDocument<User>(options =>
    {
        options.Errors = new List<Type>
        {
            typeof(UserNameTakenException),
            typeof(InvalidUserNameException)
        };
    });
```

### OpenID Connect (OIDC) Support for Subscription Authentication

RxDBDotNet supports OIDC configuration for JWT validation in GraphQL subscriptions.

Key Features:

- Dynamic retrieval of OIDC configuration, including signing keys
- Support for key rotation without requiring application restarts
- Seamless integration with existing JWT authentication setups

Usage:

1. Configure JWT Bearer authentication with OIDC support:

    ```csharp
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://your-oidc-provider.com";
            options.Audience = "your-api-audience";
            // Other JWT options...
        });
    ```

2. The SubscriptionJwtAuthInterceptor will automatically use the OIDC configuration for subscription authentication.

## Example Application

The `example` directory contains a full-stack application demonstrating RxDBDotNet usage in a real-world scenario:

- `example/livedocs-client`: A Next.js frontend app using RxDB with RxDBDotNet
- `example/LiveDocs.GraphQLApi`: A .NET backend API using RxDBDotNet
- `example/LiveDocs.AppHost`: A .NET Aspire AppHost for running the full stack

For more details on running and debugging the example client app, see the [livedocs-client README](example/livedocs-client/README.md).

### Prerequisites for Running the Example Application

To run the example application, you'll need the following installed:

- Node.js (v14 or later)
- npm (v6 or later)
- .NET 8.0 or later
- .NET Aspire workload
- Docker Desktop (latest stable version)

> Note:
>
> - Docker Desktop is required to run the Redis and SQL Server instances used by the example application.
> - For detailed instructions on installing .NET Aspire and its dependencies, please refer to the [official .NET Aspire setup documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling).

### Running the Full Stack

1. Ensure you have .NET Aspire installed and Docker Desktop is running on your machine.

2. Navigate to the `example/LiveDocs.AppHost` directory.

3. Run the following command:

    ```bash
    dotnet run --launch-profile full-stack
    ```

4. Open the .NET Aspire dashboard (typically at `http://localhost:15041`).

5. Access the frontend at `http://localhost:3001` and the GraphQL API at `http://localhost:5414/graphql`.

## RxDB Replication Protocol Details

RxDBDotNet implements the RxDB replication protocol with additional error handling conventions:

1. **Document-Level Replication**: Supports the git-like replication model for local changes and server state merging.

2. **Transfer-Level Protocol**:
   - **Pull Handler**: Implemented via the `Pull` query with checkpoint-based iteration.
   - **Push Handler**: Implemented via the `Push` mutation for client-side writes and conflict detection.
   - **Event Stream**: Implemented via the `Stream` subscription for real-time updates.

3. **Checkpoint Iteration**: Supports efficient data synchronization using checkpoints.

4. **Data Layout**:
   - Ensures documents are sortable by their last write time (`UpdatedAt`).
   - Uses soft deletes (`IsDeleted` flag) instead of physical deletion.

5. **Conflict Handling**: Implements server-side conflict detection during push operations.

6. **Offline-First Support**: Allows clients to continue operations offline and sync when back online.

7. **Advanced Error Handling**: Utilizes Hot Chocolate GraphQL mutation conventions for detailed error information.

## Custom Implementations for RxDB Clients

RxDBDotNet requires custom implementations for RxDB clients due to its advanced features and integration with Hot Chocolate GraphQL. These customizations include:

1. **Custom Query Builders**:
   - Pull Query Builder: Supports Hot Chocolate's filtering capabilities for efficient and selective data synchronization.
   - Push Query Builder: Adapts to Hot Chocolate's mutation conventions for sending local changes to the server.

2. **Subscription Topics and Pull Stream Builder**: Enables fine-grained control over real-time updates, allowing clients to subscribe to specific subsets of data.

These customizations enable RxDB clients to effectively communicate with RxDBDotNet backends, leveraging advanced features like filtered synchronization and topic-based real-time updates.

For detailed examples and explanations of these custom implementations, including code snippets and usage guidelines, please refer to the [Example Client App README](example/livedocs-client/README.md#advanced-rxdbdotnet-features).

## Security Considerations

### Server-Side Timestamp Overwriting

RxDBDotNet implements a [crucial security measure](https://rxdb.info/replication.html#security) to prevent potential issues with untrusted client-side clocks. The server always overwrites the `UpdatedAt` timestamp with its own server-side timestamp for document creation or update requests. This ensures:

1. The integrity of the document's timeline is maintained.
2. Potential time-based attacks or inconsistencies due to client clock discrepancies are mitigated.
3. The server maintains authoritative control over the timestamp for all document changes.

This security measure is implemented in the `MutationResolver<TDocument>` class. Developers should be aware that any client-provided `UpdatedAt` value will be ignored and replaced with the server's timestamp.

Important: While the `IReplicatedDocument` interface defines `UpdatedAt` with both a getter and a setter, developers should not manually set this property in their application code. Always rely on the server to set the correct `UpdatedAt` value during replication operations. The setter is present solely to allow the server to overwrite the timestamp as a security measure.

## Contributing

We welcome contributions to RxDBDotNet! Here's how you can contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/amazing-feature`).
3. Make your changes.
4. Commit your changes using [Conventional Commits](https://www.conventionalcommits.org/) syntax.
5. Push to the branch (`git push origin feature/amazing-feature`).
6. Open a Pull Request with a title that follows the Conventional Commits syntax.

Please ensure your code meets our coding standards and includes appropriate tests and documentation.

We use squash merges for all pull requests. The pull request title will be used as the commit message in the main branch, so it must follow the Conventional Commits syntax.

Please refer to our [Contributing Guide](CONTRIBUTING.md) for more detailed guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Code of Conduct

This project adheres to the Contributor Covenant [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Security

Please see our [Security Policy](SECURITY.md) for information on reporting security vulnerabilities and which versions are supported.

## Acknowledgments

- Thanks to the RxDB project for inspiring this .NET implementation.
- Thanks to the Hot Chocolate team for their excellent GraphQL server implementation.

---

Ready to dive in? [Get started](#getting-started) or [contribute](#contributing) to shape the future of .NET-based data replication!
