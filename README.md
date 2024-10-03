# RxDBDotNet

[![NuGet Version](https://img.shields.io/nuget/v/RxDBDotNet.svg)](https://www.nuget.org/packages/RxDBDotNet/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/RxDBDotNet.svg)](https://www.nuget.org/packages/RxDBDotNet/)
[![codecov](https://codecov.io/github/Ziptility/RxDBDotNet/graph/badge.svg?token=VvuBJEsIHT)](https://codecov.io/github/Ziptility/RxDBDotNet)

RxDBDotNet is a powerful .NET library that implements the [RxDB replication protocol](https://rxdb.info/replication.html), enabling real-time data synchronization between RxDB clients and .NET servers using GraphQL and Hot Chocolate. It extends the standard RxDB replication protocol with .NET-specific enhancements.

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

Whether you're building a small offline-capable app or a large-scale distributed system, RxDBDotNet provides the tools for efficient, real-time data synchronization in your .NET projects.

Ready to dive in? [Get started](#getting-started) or [contribute](#contributing) to shape the future of .NET-based data replication!

## Table of Contents

- [RxDBDotNet](#rxdbdotnet)
  - [Key Features](#key-features)
  - [Table of Contents](#table-of-contents)
  - [Getting Started](#getting-started)
  - [Sample Implementation](#sample-implementation)
  - [RxDB Replication Protocol Details](#rxdb-replication-protocol-details)
  - [Important Note for RxDB GraphQL Plugin Users](#important-note-for-rxdb-graphql-plugin-users)
  - [Advanced Features](#advanced-features)
    - [Policy-Based Security](#policy-based-security)
      - [Configuration](#configuration)
    - [Subscription Topics](#subscription-topics)
      - [Usage](#usage)
    - [Custom Error Types](#custom-error-types)
    - [OpenID Connect (OIDC) Support for Subscription Authentication](#openid-connect-oidc-support-for-subscription-authentication)
      - [Key Features:](#key-features-1)
      - [Usage:](#usage-1)
  - [Security Considerations](#security-considerations)
    - [Server-Side Timestamp Overwriting](#server-side-timestamp-overwriting)
  - [Contributing](#contributing)
  - [Code of Conduct](#code-of-conduct)
  - [License](#license)
  - [Security](#security)
  - [Acknowledgments](#acknowledgments)

## Getting Started

Here are the minimial steps to get you up and running with RxDBDotNet in your existing project:

1. Install the package:
   ```bash
   dotnet add package RxDBDotNet
   ```

2. Implement `IReplicatedDocument` for the type of document you want to replicate:
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

3. Configure services in `Program.cs`:
   ```csharp
    // Implement and add your document service to the DI container
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
    app.MapGraphQL().WithOptions(new GraphQLServerOptions
    {
        // To display the BananaCakePop UI
        Tool = { Enable = true },
    });

    app.Run();
   ```

4. Run your application and start using the GraphQL API for replication!

## Sample Implementation

Here's a more detailed example that demonstrates how to set up a simple document type and implement the required services to enable replication.

1. Create a new ASP.NET Core Web API project:
```
dotnet new webapi -minimal -n RxDBDotNetExample --no-openapi
cd RxDBDotNetExample
```

2. Add the required NuGet packages:
```
dotnet add package RxDBDotNet
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Data
```

3. Create a new file named `Workspace.cs` in the project root and add the following content:
```csharp
using RxDBDotNet.Documents;

namespace RxDBDotNetExample;

public class Workspace : IReplicatedDocument
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required bool IsDeleted { get; set; }
    public List<string>? Topics { get; set; }
}
```

4. Create a new file named `WorkspaceService.cs` in the project root and add the following content:
```csharp
using System.Collections.Concurrent;
using RxDBDotNet.Services;

namespace RxDBDotNetExample;

public class WorkspaceService : IReplicatedDocumentService<Workspace>
{
    private readonly ConcurrentDictionary<Guid, Workspace> _documents = new();
    private readonly IEventPublisher _eventPublisher;

    public WorkspaceService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public Task<Workspace?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }

    public Task<List<Workspace>> ExecuteQueryAsync(IQueryable<Workspace> query, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.ToList());
    }

    public async Task<Workspace> CreateDocumentAsync(Workspace document, CancellationToken cancellationToken)
    {
        if (_documents.TryAdd(document.Id, document))
        {
            await _eventPublisher.PublishDocumentChangedEventAsync(document, cancellationToken);
            return document;
        }
        throw new InvalidOperationException("Failed to add document");
    }

    public async Task<Workspace> UpdateDocumentAsync(Workspace document, CancellationToken cancellationToken)
    {
        if (_documents.TryGetValue(document.Id, out var existingDocument))
        {
            if (_documents.TryUpdate(document.Id, document, existingDocument))
            {
                await _eventPublisher.PublishDocumentChangedEventAsync(document, cancellationToken);
                return document;
            }
        }
        throw new InvalidOperationException("Failed to update document");
    }

    public Task<Workspace> MarkAsDeletedAsync(Workspace document, CancellationToken cancellationToken)
    {
        document.IsDeleted = true;
        return UpdateDocumentAsync(document, cancellationToken);
    }

    public bool AreDocumentsEqual(Workspace document1, Workspace document2)
    {
        return document1.Id == document2.Id &&
                document1.Name == document2.Name &&
                document1.UpdatedAt == document2.UpdatedAt &&
                document1.IsDeleted == document2.IsDeleted;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory implementation doesn't need to save changes
        return Task.CompletedTask;
    }

    public IQueryable<Workspace> GetQueryableDocuments()
    {
        return _documents.Values.AsQueryable();
    }
}
```

3. Open `Program.cs` and replace its content with the following:

```csharp
using HotChocolate.AspNetCore;
using RxDBDotNet.Extensions;
using RxDBDotNet.Services;
using RxDBDotNetExample;

var builder = WebApplication.CreateBuilder(args);

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
    // Register a type of document to be replicated
    .AddReplicatedDocument<Workspace>()
    .AddInMemorySubscriptions();

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    // To display the BananaCakePop UI
    Tool = { Enable = true },
});

app.Run();
```

5. Open `launchSettings.json` and replace its content with the following:

```json
{
    "profiles": {
    "http": {
        "commandName": "Project",
        "dotnetRunMessages": true,
        "launchBrowser": true,
        "launchUrl": "graphql",
        "applicationUrl": "http://localhost:5200",
        "environmentVariables": {
            "ASPNETCORE_ENVIRONMENT": "Development"
        }
    }
    }
}
```

6. Run the application:
   ```
   dotnet run --launch-profile http
   ```

7. Open a web browser and navigate to [http://localhost:5200/graphql](http://localhost:5200/graphql). You should see the BananaCakePop GraphQL UI.

8. Use BananaCakePop to interact with your documents:

   ```graphql
    # Push a new doc (Push Replication)
    mutation CreateWorkspace {
      pushWorkspace(
        input: {
          workspacePushRow: [
            {
              newDocumentState: {
                id: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                name: "New Workspace"
                updatedAt: "2023-07-18T12:00:00Z"
                isDeleted: true
              }
            }
          ]
        }
      ) {
        workspace {
          id
          isDeleted
          name
          updatedAt
        }
      }
    }

    # Pull documents with filtering (Pull Replication)
    query PullWorkspaces {
      pullWorkspace(limit: 10, where: { name: { eq: "New Workspace" } }) {
        documents {
          id
          name
          updatedAt
          isDeleted
        }
        checkpoint {
          updatedAt
          lastDocumentId
        }
      }
    }

   # Subscribe to workspace updates (Event Observation)
   subscription StreamWorkspaces {
     streamWorkspace() {
       documents {
         id
         name
         updatedAt
         isDeleted
       }
       checkpoint {
         updatedAt
         lastDocumentId
       }
     }
   }
   
   # After initiating the subscription above, to see real-time updates, open a new tab and run the following mutation:
   mutation CreateWorkspace {
      pushWorkspace(
        input: {
          workspacePushRow: [
            {
              newDocumentState: {
                id: "7732fc9e-cd32-45dc-a991-dce92b8e7183"
                name: "Another Workspace"
                updatedAt: "2023-07-19T12:00:00Z"
                isDeleted: true
              }
            }
          ]
        }
      ) {
        workspace {
          id
          isDeleted
          name
          updatedAt
        }
      }
    }

    # The StreamWorkspaces Response window should then display the streamed result:
    {
      "data": {
        "streamWorkspace": {
          "checkpoint": {
            "lastDocumentId": "7732fc9e-cd32-45dc-a991-dce92b8e7183",
            "updatedAt": "2023-07-19T12:00:00.000Z"
          },
          "documents": [
            {
              "id": "7732fc9e-cd32-45dc-a991-dce92b8e7183",
              "isDeleted": true,
              "name": "Another Workspace",
              "topics": null,
              "updatedAt": "2023-07-19T12:00:00.000Z"
            }
          ]
        }
      }
    }
   ```

These GraphQL operations demonstrate the core components of the RxDB replication protocol: push replication, pull replication with checkpoint management, and event observation through subscriptions.

## RxDB Replication Protocol Details

RxDBDotNet thoroughly implements the RxDB replication protocol with additional error handling conventions:

1. **Document-Level Replication**: Supports the git-like replication model where clients can make local changes and merge them with the server state.

2. **Transfer-Level Protocol**:
   - **Pull Handler**: Implemented via the `PullWorkspace` query, supporting checkpoint-based iteration.
   - **Push Handler**: Implemented via the `PushWorkspace` mutation, handling client-side writes and conflict detection.
   - **Pull Stream**: Implemented using GraphQL subscriptions (`StreamWorkspace`), enabling real-time updates.

3. **Checkpoint Iteration**: Supports efficient data synchronization using checkpoints, allowing clients to catch up with server state after being offline.

4. **Event Observation**: Utilizes GraphQL subscriptions for real-time event streaming from the server to the client.

5. **Data Layout**: 
   - Ensures documents are sortable by their last write time (`UpdatedAt`).
   - Uses soft deletes (`IsDeleted` flag) instead of physical deletion.

6. **Conflict Handling**: Implements server-side conflict detection during push operations.

7. **Offline-First Support**: Allows clients to continue operations offline and sync when back online.

8. **Advanced Error Handling**: Utilizes GraphQL mutation conventions to provide detailed error information and maintain a consistent error structure across all mutations.

This implementation ensures that RxDBDotNet is compatible with RxDB clients, providing a robust, efficient, and real-time replication solution for .NET backends with enhanced error reporting capabilities.

## Important Note for RxDB GraphQL Plugin Users

RxDBDotNet implements advanced error handling using GraphQL mutation conventions. This approach differs from the default RxDB GraphQL plugin expectations. As a result, clients using the standard RxDB GraphQL plugin will need to be modified to work with RxDBDotNet's API.

For example, the `pushWorkspace` mutation in RxDBDotNet has the following structure:

```graphql
mutation PushWorkspace($input: PushWorkspaceInput!) {
  pushWorkspace(input: $input) {
    workspace {
      id
      name
      updatedAt
      isDeleted
    }
    errors {
      ... on AuthenticationError {
        message
      }
      ... on UnauthorizedAccessError {
        message
      }
    }
  }
}
```

This differs from the standard RxDB GraphQL plugin expectation:

```graphql
mutation PushWorkspace($workspacePushRow: [WorkspaceInputPushRow]) {
  pushWorkspace(workspacePushRow: $workspacePushRow) {
    id
    name
    updatedAt
    isDeleted
  }
}
```

To use RxDBDotNet with RxDB clients, you'll need to create a custom GraphQL client that adapts to this new structure. This involves modifying the query builders and response handlers to work with the mutation convention format.

## Advanced Features

### Policy-Based Security

RxDBDotNet supports policy-based security using the Microsoft.AspNetCore.Authorization infrastructure. This allows you to define and apply fine-grained access control to your documents.

#### Configuration

1. Define your authorization policies in your `Program.cs` or startup code:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsWorkspaceAdmin", policy =>
        policy.RequireClaim("WorkspaceRole", "Admin"));
    
    options.AddPolicy("CanReadWorkspace", policy =>
        policy.RequireClaim("WorkspaceRole", "Admin", "Reader"));
});
```

2. When configuring your GraphQL server, add security options for your documents:

```csharp
builder.Services
    .AddGraphQLServer()
    // ... other configuration ...
    .AddReplicatedDocument<Workspace>(options =>
    {
        options.Security = new SecurityOptions()
            .RequirePolicyToRead("CanReadWorkspace")
            .RequirePolicyToWrite("IsWorkspaceAdmin");
    });
```

### Subscription Topics

RxDBDotNet supports subscription topics, allowing clients to subscribe to specific subsets of documents based on their topics.

#### Usage

1. When creating or updating a document, specify one or more topics:

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

This subscription will only receive updates for LiveDocs that have "workspace-123e4567-e89b-12d3-a456-426614174000" as one of their topics.

### Custom Error Types

RxDBDotNet allows you to configure custom error types through the ReplicationOptions.Errors property when setting up your documents. This feature enables you to define specific exception types that can be handled during replication operations, providing more detailed error information to your clients.

```csharp
// Define your document type
public class User : IReplicatedDocument
{
    public required Guid Id { get; init; }
    public required string Username { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required bool IsDeleted { get; set; }
}

// Define custom exceptions
public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
        : base($"The username {username} is already taken.")
    {
    }
}

public class InvalidUserNameException : Exception
{
    public InvalidUserNameException(string username)
        : base($"The username {username} is invalid.")
    {
    }
}

// Implement your document service
public class UserService : IReplicatedDocumentService<User>
{
    // ... other methods ...

    public async Task<User> CreateAsync(User document, CancellationToken cancellationToken)
    {
        // Check if username is valid
        if (string.IsNullOrWhiteSpace(document.Username))
        {
            throw new InvalidUserNameException(document.Username);
        }

        // Check if username is already taken
        if (await IsUsernameTaken(document.Username, cancellationToken))
        {
            throw new UserNameTakenException(document.Username);
        }

        // Create user
        // ... implementation details ...

        return document;
    }

    // ... other methods ...
}

// Configure services in Program.cs
builder.Services
    .AddSingleton<IReplicatedDocumentService<User>, UserService>()
    .AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Configure the GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddMutationType()
    .AddSubscriptionType()
    // Mutation conventions must be enabled for replication to work
    .AddMutationConventions()
    .AddReplication()
    .AddReplicatedDocument<User>(options =>
    {
        options.Errors = new List<Type>
        {
            typeof(UserNameTakenException),
            typeof(InvalidUserNameException)
        };
    })
    .AddInMemorySubscriptions();
```

With this configuration, when these exceptions are thrown in your `UserService` during replication operations, RxDBDotNet will handle them appropriately and include them in the GraphQL response.

### OpenID Connect (OIDC) Support for Subscription Authentication

RxDBDotNet now supports OpenID Connect (OIDC) configuration for JWT validation in GraphQL subscriptions. This enhancement allows for more flexible and secure authentication setups, especially when working with OIDC-compliant identity providers.

#### Key Features:

- Dynamic retrieval of OIDC configuration, including signing keys
- Support for key rotation without requiring application restarts
- Seamless integration with existing JWT authentication setups

#### Usage:

1. Ensure your application is configured to use JWT Bearer authentication with OIDC support:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-oidc-provider.com";
        options.Audience = "your-api-audience";
        // Other JWT options...
    });
```

2. The WebSocketJwtAuthInterceptor will automatically use the OIDC configuration when validating tokens for GraphQL subscriptions.

3. No additional configuration is needed in your GraphQL setup. The OIDC support is automatically applied to subscription authentication when available.

This feature allows for more robust and flexible authentication scenarios, particularly in environments where signing keys may change dynamically or where you're integrating with external OIDC providers like IdentityServer.

## Security Considerations

### Server-Side Timestamp Overwriting

RxDBDotNet implements a [crucial security measure](https://rxdb.info/replication.html#security) to prevent potential issues with untrusted client-side clocks. When the server receives a document creation or update request, it always overwrites the `UpdatedAt` timestamp with its own server-side timestamp. This approach ensures that:

1. The integrity of the document's timeline is maintained.
2. Potential time-based attacks or inconsistencies due to client clock discrepancies are mitigated.
3. The server maintains authoritative control over the timestamp for all document changes.

This security measure is implemented in the `MutationResolver<TDocument>` class, which handles document push operations. Developers using RxDBDotNet should be aware that any client-provided `UpdatedAt` value will be ignored and replaced with the server's timestamp.

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

## Code of Conduct

This project adheres to the Contributor Covenant [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Security

Please see our [Security Policy](./SECURITY.md) for information on reporting security vulnerabilities and which versions are supported.

## Acknowledgments

- Thanks to the RxDB project for inspiring this .NET implementation.
- Thanks to the Hot Chocolate team for their excellent GraphQL server implementation.

