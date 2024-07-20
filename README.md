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

RxDBDotNet is an open-source library that facilitates real-time data replication and synchronization between RxDB clients and .NET backends using GraphQL and Hot Chocolate. It implements the server side of the RxDB replication protocol, enabling seamless offline-first capabilities and real-time updates for any client application that supports RxDB while providing a robust .NET backend implementation.

## Key Points

- **Backend**: Implements the server-side of the [RxDB replication protocol](https://rxdb.info/replication.html) in .NET
- **Frontend**: Compatible with any client that supports the RxDB replication protocol (JavaScript, TypeScript, React Native, etc.)
- **Protocol**: Uses GraphQL for communication, leveraging the Hot Chocolate library for .NET
- **Features**: 
  - Supports real-time synchronization
  - Enables offline-first capabilities
  - Detects conflicts and reports them to the client
  - Provides efficient data pull and push operations
- **Conflict Handling**: Detects conflicts during push operations and returns them to the client for resolution
- **Flexibility**: Can be integrated with various backend storage solutions through a repository pattern

This library bridges the gap between RxDB-powered frontend applications and .NET backend services, allowing developers to build robust, real-time, offline-first applications with a .NET backend infrastructure.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Features](#features)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [License](#license)
- [Acknowledgments](#acknowledgments)
- [Contact Information](#contact-information)

## Installation

To install and set up RxDBDotNet in your project, follow these steps:

1. Install the RxDBDotNet NuGet package:
   ```bash
   dotnet add package RxDBDotNet
   ```

2. Install the required dependencies:
   ```bash
   dotnet add package HotChocolate.AspNetCore
   dotnet add package HotChocolate.Data
   ```

3. In your `Program.cs`, add the following configuration:

   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   // Add services to the container
   builder.Services
       .AddSingleton<IDocumentRepository<YourDocumentType>, YourRepositoryImplementation>();

   // Configure the GraphQL server
   builder.Services.AddGraphQLServer()
       .ModifyRequestOptions(o =>
       {
           // Enable debugging features in development
           o.IncludeExceptionDetails = true;
       })
       // Add RxDBDotNet replication support
       .AddReplicationServer()
       // Configure replication for your document type
       .AddReplicatedDocument<YourDocumentType>()
       // Enable pub/sub for GraphQL subscriptions
       .AddInMemorySubscriptions();

   // Configure CORS to allow requests from your RxDB client
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(corsPolicyBuilder =>
       {
           corsPolicyBuilder
               // Replace with your RxDB client's origin
               .WithOrigins("http://localhost:1337")
               .AllowAnyHeader()
               .AllowAnyMethod()
               // Required for WebSocket connections
               .AllowCredentials();
       });
   });

   var app = builder.Build();

   // Enable CORS
   app.UseCors();

   // Enable WebSockets (required for subscriptions)
   app.UseWebSockets();

   // Configure the GraphQL endpoint
   app.MapGraphQL()
       .WithOptions(new GraphQLServerOptions
       {
           // Configure GraphQL Playground or Banana Cake Pop
           Tool =
           {
               Enable = true,
           },
       });

   app.Run();
   ```

## Usage

Here's a basic example of how to use RxDBDotNet in your application:

1. Define your document type:

   ```csharp
   public class Hero : IReplicatedDocument
   {
       public Guid Id { get; init; }
       public string Name { get; set; }
       public DateTimeOffset UpdatedAt { get; set; }
       public bool IsDeleted { get; set; }
   }
   ```

2. Implement the `IDocumentRepository<T>` interface for your document type:

   ```csharp
   public class HeroRepository : BaseDocumentRepository<Hero>
   {
       // Implement the required methods
   }
   ```

3. Configure your GraphQL schema:

   ```csharp
   builder.Services
       .AddGraphQLServer()
       .AddReplicationServer()
       .AddReplicatedDocument<Hero>();
   ```

4. Use the GraphQL API to interact with your documents:

   ```graphql
   query PullHeroes {
     pullHero(checkpoint: null, limit: 10) {
       documents {
         id
         name
       }
       checkpoint {
         updatedAt
         lastDocumentId
       }
     }
   }

   mutation PushHero {
     pushHero(heroPushRow: [{
       newDocumentState: {
         id: "new-hero-id",
         name: "New Hero",
         updatedAt: "2023-07-18T12:00:00Z",
         isDeleted: false
       }
     }]) {
       id
       name
     }
   }

   subscription StreamHeroes {
     streamHero {
       documents {
         id
         name
       }
       checkpoint {
         updatedAt
         lastDocumentId
       }
     }
   }
   ```

## Features

- **RxDB Replication Protocol**: Implements the full [RxDB replication protocol](https://rxdb.info/replication.html), including pull, push, and real-time subscriptions.
- **GraphQL Integration**: Seamlessly integrates with GraphQL using the Hot Chocolate library.
- **Offline-First**: Supports offline-first application architecture.
- **Real-Time Updates**: Provides real-time updates through GraphQL subscriptions.
- **Conflict Resolution**: Implements server-side conflict detection and resolution.
- **Extensible**: Easily extendable to support different storage backends.
- **Type-Safe**: Fully supports C# nullable reference types for improved type safety.

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

## Contact Information

If you have any questions, concerns, or support requests, please open an issue on our [GitHub repository](https://github.com/Ziptility/RxDBDotNet/issues).
