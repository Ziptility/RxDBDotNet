<!-- ./README.md -->

# RxDBDotNet Example Client App

## Introduction

Welcome to the RxDBDotNet Example Client App! This sophisticated, offline-first JavaScript application demonstrates advanced integration between RxDB on the client-side and the RxDBDotNet GraphQL API on the server-side. Unlike basic RxDB examples, this app showcases how to leverage the enhanced features of RxDBDotNet, providing a comprehensive resource for developers looking to build robust, offline-first web applications.

## Key Features

- **Offline-First Architecture**: Full functionality without an internet connection, with seamless synchronization when online.
- **Advanced GraphQL Integration**:
  - Custom replicators leveraging RxDBDotNet's advanced features
  - GraphQL filtering for efficient data retrieval
  - Subscription topics for fine-grained real-time updates
  - Enhanced error handling using Hot Chocolate's mutation conventions
- **Custom Replication Logic**: Tailored strategies to fully leverage RxDBDotNet capabilities
- **Real-time Synchronization**: Bi-directional replication for all document types
- **Real-time Synchronization**: Bi-directional replication for all document types
- **Conflict Resolution**: Sophisticated strategies for handling offline-to-online sync conflicts
- **Optimistic UI Updates**: Immediate UI feedback with eventual consistency
- **Performance Optimized**: Efficient handling of large datasets and extended offline use

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
   git clone https://github.com/your-repo/rxdbdotnet-example-client.git
   ```

2. Navigate to the project directory:

   ```bash
   cd rxdbdotnet-example-client
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

## Usage Guide

1. **Authentication**: Use the "Login As" functionality to select a predefined user with a specific role within a workspace.

2. **Document Management**: Create, read, update, and delete Workspace, User, and LiveDoc documents. All operations work offline.

3. **Offline Mode**: Try disconnecting from the internet to see how the app functions offline. Observe the sync process when reconnecting.

4. **Sync Strategies**: Experience different synchronization behaviors for various offline scenarios.

5. **Performance Monitoring**: Use the built-in tools to monitor RxDB operations and performance in both online and offline modes.

## Architecture Overview

This application demonstrates sophisticated integration between client-side RxDB and server-side RxDBDotNet GraphQL API, with a focus on offline-first functionality. Key architectural components include:

- **Offline-First Data Layer**: Utilizes RxDB for local storage and offline operations.
- **Custom Replicators**: Located in `src/lib/workspaceReplication.ts`, `src/lib/userReplication.ts`, and `src/lib/liveDocReplication.ts`.
- **GraphQL Integration**: Customized queries and mutations in `src/lib/schemas.ts`.
- **State Management**: Uses RxDB for local state and synchronization with the server.
- **UI Components**: Material-UI components adapted for desktop experiences with offline indicators.

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

- **Offline-First Patterns**: Check `docs/offline-first-patterns.md` for detailed explanations of the offline-first strategies implemented in this app.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Acknowledgments

- RxDB team for the excellent reactive database
- Hot Chocolate for the robust GraphQL server implementation
- The RxDBDotNet community for their ongoing support and contributions

---

Happy coding with RxDBDotNet! Build amazing offline-first applications with confidence.
