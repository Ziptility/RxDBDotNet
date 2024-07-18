# RxDBDotNet

RxDBDotNet is an open-source library that facilitates real-time data replication and synchronization between RxDB clients and .NET backends using GraphQL and Hot Chocolate. It implements the server-side of the RxDB replication protocol, enabling seamless offline-first capabilities and real-time updates for any client application that supports RxDB, while providing a robust .NET backend implementation.

## Key Points

- **Backend**: Implements the server-side of the RxDB replication protocol in .NET
- **Frontend**: Compatible with any client that supports the RxDB replication protocol (JavaScript, TypeScript, React Native, etc.)
- **Protocol**: Uses GraphQL for communication, leveraging the Hot Chocolate library for .NET
- **Features**: Supports real-time synchronization, offline-first capabilities, and conflict resolution

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

3. In your `Program.cs` or `Startup.cs`, add the following configuration:

   ```csharp
   using RxDBDotNet.Extensions;

   var builder = WebApplication.CreateBuilder(args);

   builder.Services
       .AddSingleton<IDocumentRepository<YourDocumentType>, YourRepositoryImplementation>();

   builder.Services
       .AddGraphQLServer()
       .AddReplicationServer()
       .AddReplicatedDocument<YourDocumentType>();

   var app = builder.Build();

   app.MapGraphQL();

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

- **RxDB Replication Protocol**: Implements the full RxDB replication protocol, including pull, push, and real-time subscriptions.
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
4. Commit your changes (`git commit -m 'Add some amazing feature'`).
5. Push to the branch (`git push origin feature/amazing-feature`).
6. Open a Pull Request.

Please ensure your code adheres to our coding standards and includes appropriate tests and documentation.

For more detailed guidelines, refer to our [Contributing Guide](CONTRIBUTING.md).

## Code of Conduct

This project adheres to the Contributor Covenant [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Thanks to the RxDB project for inspiring this .NET implementation.
- Thanks to the Hot Chocolate team for their excellent GraphQL server implementation.

## Contact Information

For any questions, concerns, or support requests, please open an issue on our [GitHub repository](https://github.com/Ziptility/RxDBDotNet/issues).
