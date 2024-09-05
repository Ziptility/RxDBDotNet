# RxDBDotNet

<p align="left">
  <a href="https://www.nuget.org/packages/RxDBDotNet/" style="text-decoration:none;">
    <img src="https://img.shields.io/nuget/v/RxDBDotNet.svg" alt="NuGet" style="margin-right: 10px;">
  </a>
  <a href="https://codecov.io/github/Ziptility/RxDBDotNet" style="text-decoration:none;">
    <img src="https://codecov.io/github/Ziptility/RxDBDotNet/graph/badge.svg?token=VvuBJEsIHT" alt="codecov">
  </a>
  <a href="https://github.com/Ziptility/RxDBDotNet/actions/workflows/main.yml" style="text-decoration:none;">
    <img src="https://github.com/Ziptility/RxDBDotNet/actions/workflows/main.yml/badge.svg" alt="CI">
  </a>
</p>

RxDBDotNet is a .NET library that implements the server-side of the RxDB replication protocol, enabling real-time data synchronization between RxDB clients and .NET servers using GraphQL and Hot Chocolate.

## Why RxDBDotNet?

- Seamless integration with RxDB clients
- Built on top of the popular Hot Chocolate GraphQL server
- Supports offline-first capabilities and real-time updates
- Easy to set up and use with minimal configuration
- Flexible and extensible for custom document types and storage solutions

## Table of Contents

- [Getting Started](#getting-started)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [RxDB Replication Protocol Implementation](#rxdb-replication-protocol-implementation)
- [Advanced Features](#advanced-features)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [License](#license)
- [Acknowledgments](#acknowledgments)
- [Contact](#contact)

## Getting Started

Here's a minimal example to get you up and running with RxDBDotNet:

1. Install the package:
   ```bash
   dotnet add package RxDBDotNet
   ```

2. Define a document type:
   ```csharp
   public class SimpleDoc : IReplicatedDocument
   {
       public required Guid Id { get; init; }
       public required string Content { get; set; }
       public required DateTimeOffset UpdatedAt { get; set; }
       public required bool IsDeleted { get; set; }
   }
   ```

3. Configure services in `Program.cs`:
   ```csharp
   builder.Services
       .AddSingleton<IDocumentService<SimpleDoc>, InMemoryDocumentService<SimpleDoc>>()
       .AddSingleton<IEventPublisher, InMemoryEventPublisher>();

   builder.Services
       .AddGraphQLServer()
       .AddQueryType()
       .AddMutationType()
       .AddSubscriptionType()
       .AddReplicationServer()
       .AddReplicatedDocument<SimpleDoc>()
       .AddInMemorySubscriptions();
   ```

4. Run your application and start using the GraphQL API for replication!

For more detailed setup and usage, see the [Quick Start](#quick-start) section.

## Features

RxDBDotNet fully supports the RxDB replication protocol, offering:

- **Pull Replication**: Efficiently retrieve data from the server using checkpoint iteration and event observation.
- **Push Replication**: Send local changes to the server with conflict detection.
- **Real-time Updates**: Subscribe to data changes using GraphQL subscriptions for event observation.
- **Conflict Detection**: Server-side conflict detection during push operations.
- **Checkpoint Management**: Track synchronization progress using checkpoints for efficient data transfer.
- **Offline-First Support**: Continue local operations when offline and sync when back online.
- **Custom Document Types**: Define and use your own document types that implement `IReplicatedDocument`.
- **Flexible Storage**: Implement your own storage solution or use provided ones like `InMemoryDocumentService`.
- **Policy-Based Security**: Apply fine-grained access control to your replicated documents using ASP.NET Core Authorization policies.
- **Subscription Topics**: Filter real-time updates based on specific criteria or organizational structures.
- **Custom Error Types**: Define and handle specific exception types during replication operations for more detailed error reporting.
- **Hot Chocolate Integration**: Built on top of the popular Hot Chocolate GraphQL server for .NET.

## Installation

1. Install the RxDBDotNet NuGet package:
   ```bash
   dotnet add package RxDBDotNet
   ```

2. Install required dependencies:
   ```bash
   dotnet add package HotChocolate.AspNetCore
   dotnet add package HotChocolate.Data
   ```

## Quick Start

Here's a step-by-step guide to get you started with RxDBDotNet:

1. Define your document type:

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

   This document type implements `IReplicatedDocument`, which ensures it has the necessary properties for replication, including `Id`, `UpdatedAt`, and `IsDeleted`.

2. Implement the `IDocumentService<T>` interface for your document type. For this quick start, we'll use a simple in-memory implementation:

   ```csharp
   using System.Collections.Concurrent;
   using System.Linq.Expressions;

   public class InMemoryWorkspaceService : IDocumentService<Workspace>
   {
       private readonly ConcurrentDictionary<Guid, Workspace> _workspaces = new();
       private readonly IEventPublisher _eventPublisher;

       public InMemoryWorkspaceService(IEventPublisher eventPublisher)
       {
           _eventPublisher = eventPublisher;
       }

       public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
       {
           _workspaces.TryGetValue(id, out var workspace);
           return Task.FromResult(workspace);
       }

       public Task<IReadOnlyList<Workspace>> GetManyAsync(Expression<Func<Workspace, bool>> predicate, int limit, CancellationToken cancellationToken)
       {
           var result = _workspaces.Values
               .AsQueryable()
               .Where(predicate)
               .Take(limit)
               .ToList();

           return Task.FromResult<IReadOnlyList<Workspace>>(result);
       }

       public async Task<Workspace> CreateAsync(Workspace document, CancellationToken cancellationToken)
       {
           if (_workspaces.TryAdd(document.Id, document))
           {
               await _eventPublisher.PublishAsync(document, cancellationToken);
               return document;
           }
           throw new InvalidOperationException("Failed to add workspace");
       }

       public async Task<Workspace> UpdateAsync(Workspace document, CancellationToken cancellationToken)
       {
           if (_workspaces.TryGetValue(document.Id, out var existingWorkspace))
           {
               if (_workspaces.TryUpdate(document.Id, document, existingWorkspace))
               {
                   await _eventPublisher.PublishAsync(document, cancellationToken);
                   return document;
               }
           }
           throw new InvalidOperationException("Failed to update workspace");
       }

       public Task<IReadOnlyList<Workspace>> GetUpdatedAsync(DateTimeOffset since, Guid checkpointToken, int limit, CancellationToken cancellationToken)
       {
           var result = _workspaces.Values
               .Where(w => w.UpdatedAt > since && w.Id.CompareTo(checkpointToken) > 0)
               .OrderBy(w => w.UpdatedAt)
               .ThenBy(w => w.Id)
               .Take(limit)
               .ToList();

           return Task.FromResult<IReadOnlyList<Workspace>>(result);
       }
   }
   ```

   This implementation provides a simple in-memory storage for workspaces using a `ConcurrentDictionary`. It supports the core operations required by the RxDB replication protocol, including checkpoint-based querying and event publishing.

3. In your `Program.cs`, configure the services and GraphQL schema:

   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   // Add services to the container
   builder.Services
       .AddSingleton<IDocumentService<Workspace>, InMemoryWorkspaceService>()
       .AddSingleton<IEventPublisher, InMemoryEventPublisher>();

   // Configure the GraphQL server
   builder.Services
       .AddGraphQLServer()
       .AddQueryType()
       .AddMutationType()
       .AddSubscriptionType()
       .AddReplicationServer()
       .AddReplicatedDocument<Workspace>()
       .AddInMemorySubscriptions();

   var app = builder.Build();

   app.UseWebSockets();
   app.MapGraphQL();

   app.Run();
   ```

   This setup configures the GraphQL server to support the RxDB replication protocol, including query, mutation, and subscription types necessary for pull replication, push replication, and real-time updates.

4. Use the GraphQL API to interact with your documents:

   ```graphql
   # Push a new workspace (Push Replication)
   mutation PushWorkspace {
     pushWorkspace(workspacePushRow: [{
       newDocumentState: {
         id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
         name: "New Workspace",
         updatedAt: "2023-07-18T12:00:00Z",
         isDeleted: false
       }
     }]) {
       id
       name
       updatedAt
       isDeleted
     }
   }

   # Pull workspaces with filtering (Pull Replication)
   query PullWorkspaces {
     pullWorkspace(
       limit: 10
     ) {
       documents(where: { name: { eq: "New Workspace" } }) {
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
     streamWorkspace(headers: { Authorization: "Bearer your-auth-token" }) {
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
   ```

   These GraphQL operations demonstrate the core components of the RxDB replication protocol: push replication, pull replication with checkpoint management, and event observation through subscriptions.

## RxDB Replication Protocol Implementation

RxDBDotNet thoroughly implements the RxDB replication protocol:

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

This implementation ensures that RxDBDotNet is fully compatible with RxDB clients, providing a robust, efficient, and real-time replication solution for .NET backends.

## Advanced Features

### Policy-Based Security

RxDBDotNet supports policy-based security using the Microsoft.AspNetCore.Authorization infrastructure. This allows you to define and apply fine-grained access control to your replicated documents.

#### How It Works

1. You define policies using the standard ASP.NET Core authorization mechanisms.
2. You configure which policies apply to which document types and operations using RxDBDotNet's `SecurityOptions`.
3. RxDBDotNet automatically enforces these policies during replication operations.

#### Configuration

1. First, define your authorization policies in your `Program.cs` or startup code:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsWorkspaceAdmin", policy =>
        policy.RequireClaim("WorkspaceRole", "Admin"));
    
    options.AddPolicy("CanReadWorkspace", policy =>
        policy.RequireClaim("WorkspaceRole", "Admin", "Reader"));
});
```

2. When configuring your GraphQL server, add security options for your replicated documents:

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

This configuration ensures that:
- Only users with the "Admin" or "Reader" role can read workspaces.
- Only users with the "Admin" role can create, update, or delete workspaces.

#### Usage

With this configuration in place, RxDBDotNet will automatically enforce these policies:

- During pull replication, only documents that the user is authorized to read will be returned.
- During push replication, writes will only be accepted if the user is authorized to write.
- For subscriptions, events will only be sent for documents the user is authorized to read.

#### Example

Here's how you might use this in practice:

1. Set up your authentication mechanism (e.g., JWT).
2. Configure your policies and RxDBDotNet security as shown above.
3. When making GraphQL requests, include the authentication token.

RxDBDotNet will then use the claims in the authentication token to enforce your policies automatically.

For more detailed examples and advanced scenarios, please refer to our unit tests in the `SecurityTests` class.

### Subscription Topics

RxDBDotNet supports subscription topics, allowing clients to subscribe to specific subsets of documents based on their topics. This feature is particularly useful for scenarios where you want to filter real-time updates based on certain criteria, such as tenant, organization, or any relevant topic for which you want to receive updates.

#### How It Works

1. When creating or updating a document, you can specify one or more topics for that document.
2. When subscribing to document updates, clients can specify which topics they're interested in.
3. Clients will only receive updates for documents that match their specified topics.

#### Configuration

To use subscription topics, your document type should include a property to store topics. For example:

```csharp
public class LiveDoc : IReplicatedDocument
{
    public required Guid Id { get; init; }
    public required string Content { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required bool IsDeleted { get; set; }
    public required Guid WorkspaceId { get; set; }
    public List<string>? Topics { get; set; }
}
```

#### Usage

1. Setting topics when creating or updating a document:

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

2. Subscribing to specific topics:

In your GraphQL subscription, you can specify the topics you're interested in:

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

#### Example Scenario

Let's consider a scenario where you have multiple workspaces, and clients should only receive updates for the LiveDocs within a specific workspace:

1. When creating a LiveDoc, set its workspace ID as a topic:

```csharp
var liveDoc = new LiveDoc
{
    Id = Guid.NewGuid(),
    Content = "Document in Workspace A",
    UpdatedAt = DateTimeOffset.UtcNow,
    IsDeleted = false,
    WorkspaceId = workspaceId,
    Topics = new List<string> { $"workspace-{workspaceId}" }
};

await documentService.CreateAsync(liveDoc, CancellationToken.None);
```

2. Clients can then subscribe only to LiveDocs within their specific workspace:

```graphql
subscription StreamWorkspaceLiveDocs {
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

This way, each client will only receive updates for the LiveDocs within their specific workspace, improving performance and maintaining data isolation between workspaces.

By leveraging subscription topics, RxDBDotNet provides a powerful mechanism for filtering real-time updates, enabling efficient and secure data synchronization within specific workspaces or other organizational structures.

### Custom Error Types

RxDBDotNet allows you to configure custom error types through the ReplicationOptions.Errors property when setting up your replicated documents. This feature enables you to define specific exception types that can be handled during replication operations, providing more detailed error information to your clients.

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
public class UserService : IDocumentService<User>
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
    .AddSingleton<IDocumentService<User>, UserService>()
    .AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Configure the GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddMutationType()
    .AddSubscriptionType()
    .AddReplicationServer()
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

In this example:

1. We define a `User` document type that implements `IReplicatedDocument`.
2. We create custom exceptions `UserNameTakenException` and `InvalidUserNameException`.
3. We implement a `UserService` that throws these exceptions in appropriate scenarios.
4. When configuring the GraphQL server, we add these exception types to the `ReplicationOptions.Errors` list when calling `AddReplicatedDocument<User>()`.

Now, when these exceptions are thrown in your `UserService` during replication operations, RxDBDotNet will handle them appropriately. For example, during a push operation:

```graphql
mutation PushUser {
  pushUser(userPushRow: [{
    newDocumentState: {
      id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      username: "takenUsername",
      updatedAt: "2023-07-18T12:00:00Z",
      isDeleted: false
    }
  }]) {
    id
    username
    updatedAt
    isDeleted
  }
}
```

If the username is already taken, the operation might return:

```json
{
  "data": {
    "pushUser": null
  },
  "errors": [
    {
      "message": "The username takenUsername is already taken.",
      "extensions": {
        "code": "USER_NAME_TAKEN"
      }
    }
  ]
}
```

By leveraging these custom exception types, you provide more detailed error information to your clients, making it easier to handle specific error scenarios during replication processes.

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

## Acknowledgments

- Thanks to the RxDB project for inspiring this .NET implementation.
- Thanks to the Hot Chocolate team for their excellent GraphQL server implementation.

## Contact

If you have any questions, concerns, or support requests, please open an issue on our [GitHub repository](https://github.com/Ziptility/RxDBDotNet/issues).