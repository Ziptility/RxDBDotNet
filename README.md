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

# RxDBDotNet

RxDBDotNet is a powerful .NET library that implements the RxDB replication protocol, enabling real-time data synchronization between RxDB clients and .NET servers using GraphQL and Hot Chocolate. It extends the standard RxDB replication protocol with .NET-specific enhancements.

## Key Features

- 🔄 Full RxDB Protocol Support
- 🔥 Hot Chocolate GraphQL Integration
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

4. Use the GraphQL API to interact with your documents:

   ```graphql
   # Push a new workspace (Push Replication)
   mutation PushWorkspace {
     pushWorkspace(input: {
       workspacePushRow: [{
         newDocumentState: {
           id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
           name: "New Workspace",
           updatedAt: "2023-07-18T12:00:00Z",
           isDeleted: false
         }
       }]
     }) {
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
         ... on ArgumentNullError {
           message
           paramName
         }
       }
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
      ... on ArgumentNullError {
        message
        paramName
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

RxDBDotNet supports policy-based security using the Microsoft.AspNetCore.Authorization infrastructure. This allows you to define and apply fine-grained access control to your replicated documents.

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

With this configuration, when these exceptions are thrown in your `UserService` during replication operations, RxDBDotNet will handle them appropriately and include them in the GraphQL response.

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
