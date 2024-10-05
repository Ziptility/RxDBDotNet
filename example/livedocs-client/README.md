<!-- example/livedocs-client/README.md -->
# RxDBDotNet Example Client App

## Introduction

Welcome to the RxDBDotNet Example Client App! This local-first JavaScript application demonstrates advanced integration between RxDB on the client-side and the RxDBDotNet GraphQL API on the server-side. Unlike basic RxDB examples, this app showcases how to leverage the enhanced features of RxDBDotNet, providing a comprehensive resource for developers looking to build robust, local-first web applications.

## Table of Contents

- [RxDBDotNet Example Client App](#rxdbdotnet-example-client-app)
  - [Introduction](#introduction)
  - [Table of Contents](#table-of-contents)
  - [Key Features](#key-features)
  - [Technology Stack](#technology-stack)
  - [Running the Example](#running-the-example)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
    - [Running the Application](#running-the-application)
      - [Development/Debug Mode](#developmentdebug-mode)
      - [Production Mode (Full Stack)](#production-mode-full-stack)
  - [Available Scripts](#available-scripts)
  - [Usage Guide](#usage-guide)
  - [Architecture Overview](#architecture-overview)
  - [Advanced RxDBDotNet Features](#advanced-rxdbdotnet-features)
    - [Custom Query Builders](#custom-query-builders)
      - [Pull Query Builder](#pull-query-builder)
      - [Push Query Builder](#push-query-builder)
    - [Subscription Topics and Pull Stream Builder](#subscription-topics-and-pull-stream-builder)
    - [Response Modifier](#response-modifier)
    - [Implementing Filtered Replication](#implementing-filtered-replication)
    - [Multi-Collection Replication Setup](#multi-collection-replication-setup)
    - [Database Initialization with Replication](#database-initialization-with-replication)
  - [Error Handling](#error-handling)
  - [Contributing](#contributing)
  - [License](#license)
  - [Acknowledgments](#acknowledgments)

## Key Features

- **Local-First Architecture**: Full functionality without an internet connection, with seamless synchronization when online.
- **Advanced GraphQL Integration**:
  - Custom replicators leveraging RxDBDotNet's advanced features
  - GraphQL filtering for efficient data retrieval
  - Subscription topics for fine-grained real-time updates
  - Enhanced error handling using Hot Chocolate's mutation conventions
- **Replication Logic**: Utilizes RxDBDotNet's built-in replication capabilities with custom query builders for each document type
- **Real-time Synchronization**: Bi-directional replication for all document types
- **Optimistic UI Updates**: Immediate UI feedback with eventual consistency
- **Offline Capability**: Supports basic offline operations using RxDB's local storage capabilities

## Technology Stack

- Frontend: Next.js, TypeScript
- UI: Material-UI (with Material Design 3 principles)
- State Management: React hooks, RxDB
- API: GraphQL
- Offline Support: RxDB's local storage and GraphQL replication protocol

## Running the Example

This section provides instructions for setting up and running the RxDBDotNet Example Client App.

### Prerequisites

To run this example application, you'll need the following installed:

- Node.js (v14 or later)
- npm (v6 or later)
- .NET 8.0 or later
- .NET Aspire workload
- Docker Desktop (latest stable version)

> Note:
>
> - Docker Desktop is required to run the Redis and SQL Server instances used by the backend API.
> - The backend API requires .NET Aspire to be installed. For detailed instructions on installing .NET Aspire and its dependencies, please refer to the [official .NET Aspire setup documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling).

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

Note: The frontend application requires a running backend API to function properly.

#### Development/Debug Mode

To run and debug the application in development mode:

1. Start the backend API:
    a. Navigate to the `example/LiveDocs.AppHost` directory.
    b. Run the following command:

    ```bash
    dotnet run --launch-profile api-only
    ```

    This will start the backend API without launching the production build of the frontend.

2. In a new terminal, navigate to the `example/livedocs-client` directory.

3. Open the project in VS Code:

   ```bash
   code .
   ```

4. Set up debugging in VS Code:
   a. Go to the "Run and Debug" view (Ctrl+Shift+D or Cmd+Shift+D).
   b. Select "Next.js: debug full stack" from the dropdown.
   c. Press F5 or click the green play button to start debugging.

   This will launch the Next.js development server with debugging enabled, allowing you to:
   - Set breakpoints in your code
   - Inspect variables and state
   - Use the VS Code debug console for logging and testing

5. Once the development server starts, VS Code should automatically open your default browser to `http://localhost:3000`. If it doesn't, you can manually navigate to this URL.

#### Production Mode (Full Stack)

To run the full stack, including both the backend API and the production build of the frontend:

1. Navigate to the `example/LiveDocs.AppHost` directory.
2. Run the following command:

   ```bash
   dotnet run --launch-profile full-stack
   ```

3. Open the .NET Aspire dashboard (typically at `http://localhost:15041`).
4. Access the frontend at `http://localhost:3001` and the GraphQL API at `http://localhost:5414/graphql`.

This mode is useful for testing the entire application in a production-like environment.

## Available Scripts

In the project directory, you can run:

- `npm run dev`: Starts the development server on port 3000.
- `npm run build`: Builds the application for production.
- `npm run start`: Runs the built application on port 3000.
- `npm run run`: Builds the application and then starts it (combination of `build` and `start`).
- `npm run clean`: Removes the `.next` and `out` directories.
- `npm run format`: Formats all supported files in the project using Prettier.
- `npm run generate`: Generates TypeScript types from the GraphQL schema using GraphQL Code Generator.

Additional scripts for development:

- `npm run lint`: Runs ESLint to check for code quality issues (this script is implied by the presence of ESLint in devDependencies).

These scripts help you develop, build, and maintain your Next.js application with RxDB and GraphQL integration.

## Usage Guide

1. **Authentication**: Use the "Login As" functionality to select a predefined user with a specific role within a workspace.

2. **Document Management**: Create, read, update, and delete Workspace, User, and LiveDoc documents. All operations work offline.

3. **Offline Mode**: Try disconnecting from the internet to see how the app functions offline. Observe the sync process when reconnecting.

4. **Sync Strategies**: Experience different synchronization behaviors for various offline scenarios.

5. **Performance Monitoring**: Use the built-in tools to monitor RxDB operations and performance in both online and offline modes.

## Architecture Overview

This application demonstrates sophisticated integration between client-side RxDB and server-side RxDBDotNet GraphQL API, with a focus on local-first functionality. Key architectural components include:

- **Local-First Data Layer**: Utilizes RxDB for local storage and offline operations.
- **Custom Replicators**: Located in `src/lib/workspaceReplication.ts`, `src/lib/userReplication.ts`, and `src/lib/liveDocReplication.ts`.
- **GraphQL Integration**: Customized queries and mutations in `src/lib/schemas.ts`.
- **State Management**: Uses RxDB for local state and synchronization with the server.
- **UI Components**: Material-UI components adapted for desktop experiences with offline indicators.

## Advanced RxDBDotNet Features

RxDBDotNet introduces several advanced features that require custom implementations for RxDB clients. This section details these customizations and their purposes.

### Custom Query Builders

#### Pull Query Builder

The pull query builder supports Hot Chocolate's filtering capabilities for efficient and selective data synchronization:

```typescript
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

This custom pull query builder allows for:

- Efficient data retrieval using checkpoints
- Selective synchronization with filters (`where` clause)
- Limiting the number of documents pulled in a single request

In practice, you would typically align the pull filter with the subscription topic filter to support consistent syncing and real-time updates for a subset of documents. For example, in the LiveDocs client app, a user with the `StandardUser` role would only replicate (pull) and get real-time updates (subscribe to) LiveDocs within their workspace.

#### Push Query Builder

The push query builder creates a mutation to send local changes to the server, accommodating Hot Chocolate's mutation conventions:

```typescript
const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (
  pushRows: RxReplicationWriteToMasterRow<Workspace>[]
) => {
  const query = `
    mutation PushWorkspace($input: PushWorkspaceInput!) {
      pushWorkspace(input: $input) {
        workspace {
          id
          name
          topics
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
  `;

  return {
    query,
    operationName: 'PushWorkspace',
    variables: {
      input: {
        workspacePushRow: pushRows,
      },
    },
  };
};
```

### Subscription Topics and Pull Stream Builder

RxDBDotNet supports subscription topics for fine-grained control over real-time updates:

```typescript
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

This allows clients to:

- Subscribe only to updates they're interested in
- Receive real-time updates for specific subsets of data
- Improve efficiency by filtering updates at the source

### Response Modifier

Response modifiers handle server responses and transform them into the format expected by RxDB:

```typescript
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

This allows us to:

1. Extract and handle any push errors
2. Transform the server response to match RxDB's expectations

### Implementing Filtered Replication

To use these custom implementations, configure your replication as follows:

```typescript
export const replicateWorkspaces = (
  token: string,
  collection: RxCollection<Workspace>,
  filter?: WorkspaceFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<Workspace> => {
  const replicationState = replicateGraphQL({
    collection,
    url: {
      http: API_CONFIG.GRAPHQL_ENDPOINT,
      ws: API_CONFIG.WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder(filter),
      streamQueryBuilder: pullStreamBuilder(topics),
      batchSize,
      includeWsHeaders: true,
    },
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
    live: true,
    deletedField: 'isDeleted',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    replicationIdentifier: 'workspace-replication',
    autoStart: true,
  });

  initializeLoggingAndErrorHandlers(replicationState);

  return replicationState;
};
```

This setup incorporates:

- Filtered pull queries for selective data synchronization
- Topic-based subscriptions for targeted real-time updates
- Custom push query builder to work with Hot Chocolate's mutation conventions
- Response modifier to handle errors and transform server responses
- Error handling and logging

### Multi-Collection Replication Setup

To set up replication for multiple collections:

```typescript
export const setupReplication = (db: LiveDocsDatabase, jwtAccessToken: string): LiveDocsReplicationStates => {
  const token = jwtAccessToken || API_CONFIG.DEFAULT_JWT_TOKEN;

  try {
    console.log('Setting up replication with token:', token);
    const replicationStates: LiveDocsReplicationStates = {
      workspaces: replicateWorkspaces(token, db.workspace),
      users: replicateUsers(token, db.user),
      livedocs: replicateLiveDocs(token, db.livedoc),
    };
    console.log('Replication setup completed');
    return replicationStates;
  } catch (error) {
    handleError(error, 'setupReplication', { jwtAccessToken });
    throw error;
  }
};
```

### Database Initialization with Replication

When initializing your database, set up replication like this:

```typescript
const initializeDatabase = async (): Promise<LiveDocsDatabase> => {
  try {
    const db = await createRxDatabase<LiveDocsCollections>({
      name: 'livedocsdb',
      storage: getRxStorageDexie(),
      multiInstance: false,
      ignoreDuplicate: false,
      eventReduce: true,
    });

    console.log('Database created successfully');

    await db.addCollections(createCollections());
    console.log('Collections added successfully');

    const replicationStates = setupReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);
    console.log('Replication set up successfully');

    setupReplicationLogging(replicationStates);

    return Object.assign(db, { replicationStates }) as LiveDocsDatabase;
  } catch (error) {
    handleError(error, 'initializeDatabase');
    throw error;
  }
};
```

By implementing these custom query builders, response modifiers, and leveraging Hot Chocolate's filtering capabilities, your RxDB client can efficiently synchronize and receive real-time updates for specific subsets of data, significantly improving performance and reducing unnecessary data transfer.

## Error Handling

This example application uses simplified error handling for demonstration purposes. Instead of displaying errors in the UI, all errors are logged to the browser console. This approach is suitable for developers who are comfortable using browser developer tools for debugging.

To view errors:

1. Open your browser's developer tools (usually F12 or Ctrl+Shift+I).
2. Navigate to the "Console" tab.
3. Any errors or warnings will be displayed here with relevant context and information.

This simplified approach allows for a cleaner UI while still providing valuable debugging information for developers learning to use RxDBDotNet.

## Contributing

We welcome contributions to the RxDBDotNet Example Client App! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes, ensuring they adhere to the project's coding standards and local-first principles.
4. Submit a pull request with a clear description of your changes.

For more detailed information, please read our [Contribution Guidelines](../../CONTRIBUTING.md).

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Acknowledgments

- RxDB team for the excellent reactive database
- Hot Chocolate for the robust GraphQL server implementation
- The RxDBDotNet community for their ongoing support and contributions

---

Happy coding with RxDBDotNet! Build amazing local-first applications with confidence.
