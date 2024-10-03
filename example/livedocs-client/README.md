<!-- ./README.md -->

# RxDBDotNet Example Client App

## Introduction

Welcome to the RxDBDotNet Example Client App! This sophisticated, local-first JavaScript application demonstrates advanced integration between RxDB on the client-side and the RxDBDotNet GraphQL API on the server-side. Unlike basic RxDB examples, this app showcases how to leverage the enhanced features of RxDBDotNet, providing a comprehensive resource for developers looking to build robust, offline-first web applications.

## Key Features

- **Local-First Architecture**: Full functionality without an internet connection, with seamless synchronization when online.
- **Advanced GraphQL Integration**:
  - Custom replicators leveraging RxDBDotNet's advanced features
  - GraphQL filtering for efficient data retrieval
  - Subscription topics for fine-grained real-time updates
  - Enhanced error handling using Hot Chocolate's mutation conventions
- **Replication Logic**: Utilizes RxDBDotNet's built-in replication capabilities with custom query builders for each document type
- **Real-time Synchronization**: Bi-directional replication for all document types
- **Conflict Handling**: Utilizes RxDB's default conflict resolution mechanism
- **Optimistic UI Updates**: Immediate UI feedback with eventual consistency
- **Offline Capability**: Supports basic offline operations using RxDB's local storage capabilities

## Technology Stack

- Frontend: Next.js, TypeScript
- UI: Material-UI (with Material Design 3 principles)
- State Management: React hooks, RxDB
- API: GraphQL
- Offline Support: RxDB's local storage and GraphQL replication protocol

## Getting Started

### Prerequisites

- Node.js (v14 or later)
- npm (v6 or later)

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/Ziptility/RxDBDotNet.git
   ```

2. Navigate to the project directory:

   ```bash
   cd RxDBDotNet/example/livedocs-client
   ```

3. Install dependencies:

   ```bash
   npm install
   ```

### Running the Application

1. Start the development server:

   ```bash
   npm run dev
   ```

2. Open your browser and navigate to `http://localhost:3000`

### Available Scripts

- `npm run dev`: Starts the development server
- `npm run build`: Builds the application for production
- `npm start`: Runs the built application
- `npm run lint`: Runs ESLint to check for code quality issues
- `npm run format`: Formats the code using Prettier
- `npm run generate`: Generates TypeScript types from the GraphQL schema

### Debugging

1. Open the project in Visual Studio Code.
2. Go to the "Run and Debug" view (Ctrl+Shift+D or Cmd+Shift+D).
3. Select "Next.js: debug full stack" from the dropdown.
4. Press F5 or click the green play button to start debugging.

This will launch the Next.js development server with debugging enabled, allowing you to set breakpoints and inspect variables in VS Code.

## Usage Guide

1. **Authentication**: Use the "Login As" functionality to select a predefined user with a specific role within a workspace.

2. **Document Management**: Create, read, update, and delete Workspace, User, and LiveDoc documents. All operations work offline.

3. **Offline Mode**: Try disconnecting from the internet to see how the app functions offline. Observe the sync process when reconnecting.

4. **Sync Strategies**: Experience different synchronization behaviors for various offline scenarios.

5. **Performance Monitoring**: Use the built-in tools to monitor RxDB operations and performance in both online and offline modes.

## Architecture Overview

This application demonstrates sophisticated integration between client-side RxDB and server-side RxDBDotNet GraphQL API, with a focus on offline-first functionality. Key architectural components include:

- **Local-First Data Layer**: Utilizes RxDB for local storage and offline operations.
- **Custom Replicators**: Located in `src/lib/workspaceReplication.ts`, `src/lib/userReplication.ts`, and `src/lib/liveDocReplication.ts`.
- **GraphQL Integration**: Customized queries and mutations in `src/lib/schemas.ts`.
- **State Management**: Uses RxDB for local state and synchronization with the server.
- **UI Components**: Material-UI components adapted for desktop experiences with offline indicators.

## Advanced RxDBDotNet Features

### Custom Query Generation

We've implemented custom query builders for each document type to leverage RxDBDotNet's advanced filtering capabilities:

```typescript
// Example from src/lib/workspaceReplication.ts
const pullQueryBuilder = (variables?: WorkspaceFilterInput): RxGraphQLReplicationPullQueryBuilder<Checkpoint> => {
  return (checkpoint: Checkpoint | undefined, limit: number) => {
    const query = `
      query PullWorkspace($checkpoint: WorkspaceInputCheckpoint, $limit: Int!, $where: WorkspaceFilterInput) {
        pullWorkspace(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            name
            topics
            updatedAt
            isDeleted
          }
          checkpoint {
            lastDocumentId
            updatedAt
          }
        }
      }
    `;

    return {
      query,
      operationName: 'PullWorkspace',
      variables: {
        checkpoint,
        limit,
        where: variables,
      },
    };
  };
};
```

This custom query builder allows for efficient data retrieval and filtering directly from the server.

### Response Modifier

We use response modifiers to handle server responses and transform them into the format expected by RxDB:

```typescript
// Example from src/lib/workspaceReplication.ts
push: {
  queryBuilder: pushQueryBuilder,
  batchSize,
  responseModifier: (response: PushWorkspacePayload): ReplicationPushHandlerResult<Workspace> => {
    if (response.errors) {
      response.errors.forEach((error) => handleError(error, 'Workspace replication push'));
    }
    return response.workspace ?? [];
  },
},
```

This allows us to handle errors and transform the server response to match RxDB's expectations.

### Subscription Topics

We've implemented subscription topics to allow for fine-grained control over real-time updates:

```typescript
// Example from src/lib/workspaceReplication.ts
const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamWorkspace($headers: WorkspaceInputHeaders, $topics: [String!]) {
        streamWorkspace(headers: $headers, topics: $topics) {
          documents {
            id
            name
            topics
            updatedAt
            isDeleted
          }
          checkpoint {
            lastDocumentId
            updatedAt
          }
        }
      }
    `;

    return {
      query,
      variables: {
        headers,
        topics,
      },
    };
  };
};
```

This allows clients to subscribe only to the updates they're interested in, reducing unnecessary data transfer.

### Conflict Handling

In this example application, we use RxDB's default conflict resolution mechanism. We don't implement custom conflict handlers, but the architecture allows for easy extension if needed:

```typescript
// Example from src/lib/database.ts
const collections = await db.addCollections({
  workspace: {
    schema: workspaceSchema,
  },
  user: {
    schema: userSchema,
  },
  livedoc: {
    schema: liveDocSchema,
  },
});
```

RxDB's default mechanism will handle conflicts during synchronization based on the document's update timestamp.

## Error Handling

This example application uses simplified error handling for demonstration purposes. Instead of displaying errors in the UI, all errors are logged to the browser console. This approach is suitable for developers who are comfortable using browser developer tools for debugging.

To view errors:

1. Open your browser's developer tools (usually F12 or Ctrl+Shift+I).
2. Navigate to the "Console" tab.
3. Any errors or warnings will be displayed here with relevant context and information.

This simplified approach allows for a cleaner UI while still providing valuable debugging information for developers learning to use RxDBDotNet.

## Roadmap and Future Improvements

While this example application demonstrates many key features of RxDBDotNet, there are several areas where we plan to expand and improve functionality in future versions:

1. Enhanced Filtering and Replication Control
2. GraphQL Subscription Topics
3. Role-Based Access Control and Business Rules
4. Enhanced Error Handling and Conflict Resolution
5. Performance Optimization
6. Improved Documentation and Learning Resources

We welcome contributions and suggestions for these upcoming features. If you're interested in helping implement any of these improvements, please check our [Contribution Guidelines](../../CONTRIBUTING.md) and feel free to open an issue for discussion.

## Development

### Available Scripts

- `npm run dev`: Starts the development server.
- `npm run build`: Builds the application for production.
- `npm start`: Runs the built application.
- `npm run clean`: Removes build artifacts.
- `npm run format`: Formats the codebase using Prettier.

### Coding Standards

This project adheres to strict TypeScript and Next.js typing rules. Please refer to `.eslintrc.json` and `tsconfig.json` for specific configurations.

## Contributing

We welcome contributions to the RxDBDotNet Example Client App! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes, ensuring they adhere to the project's coding standards and offline-first principles.
4. Write or update tests as necessary, including offline scenario tests.
5. Submit a pull request with a clear description of your changes.

For more detailed information, please read our [Contribution Guidelines](../../CONTRIBUTING.md).

## Learning Resources

- **Local-First Patterns**: Check `docs/local-first-patterns.md` for detailed explanations of the local-first strategies implemented in this app.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Acknowledgments

- RxDB team for the excellent reactive database
- Hot Chocolate for the robust GraphQL server implementation
- The RxDBDotNet community for their ongoing support and contributions

---

Happy coding with RxDBDotNet! Build amazing offline-first applications with confidence.
