<!-- ./REQUIREMENTS.md -->

# RxDBDotNet Example Client App Requirements

## 1. Overview

The RxDBDotNet Example Client App is a local-first JavaScript application designed to demonstrate the integration between RxDB on the client-side and the RxDBDotNet GraphQL API on the server-side. This web-based app showcases how to leverage the built-in features of RxDBDotNet while prioritizing local-first functionality and efficient synchronization.

Key aspects of this example app include:

1. **Local-First Architecture**: The app is designed to work primarily with local data, providing instant responsiveness and a seamless user experience regardless of network status.

2. **GraphQL Integration**: The app demonstrates how to use RxDB's built-in GraphQL replication for efficient server synchronization.

3. **Simplified Replication Logic**: The app illustrates how to use RxDB's built-in replication mechanisms to handle synchronization with the server.

This example app serves as a learning resource for software engineers looking to build robust, local-first web applications using RxDBDotNet.

## 2. Technology Stack

- Frontend: Next.js, TypeScript
- UI: Material-UI (with Material Design 3 principles)
- State Management: React hooks, RxDB
- API: GraphQL
- Local-First Support: RxDB's local storage and GraphQL replication protocol

## 3. Core Functionality

### 3.1 Document Types

- Implement CRUD operations for:
  - Workspace
  - User
  - LiveDoc

### 3.2 Local-First Operations

- Use RxDB as the primary data store for all operations.
- Ensure all CRUD operations work primarily on local data for instant responsiveness.
- Implement RxDB's observable queries to keep the UI in sync with local data changes.
- Provide optimistic UI updates for immediate user feedback.

### 3.3 Synchronization

- Implement bi-directional replication for all document types using RxDB's GraphQL replication plugin.
- Configure replication settings in the database setup.
- Provide clear indicators of synchronization status in the UI.

### 3.4 Authentication

- Implement simplified "Login As" functionality.
- Allow selection of defined users with specific roles within workspaces.
- Configure RxDB replicators with the correct JWT for the selected user.
- Ensure RxDB uses the provided JWT for all GraphQL replication operations (queries, mutations, and subscriptions).
- Implement a mechanism to update the JWT in RxDB replicators when it changes (e.g., on user switch or token refresh).

## 4. RxDBDotNet Features

### 4.1 Conflict Resolution

- Implement RxDB's built-in conflict resolution mechanism.
- Define custom conflict handlers for each collection if needed.

### 4.2 Error Handling

- Implement error handling for local operations using try-catch blocks.
- Use RxDB's replication error events for handling synchronization issues.

### 4.3 Subscription Management

- Utilize RxDB's built-in subscription capabilities for real-time updates.

### 4.4 Optimistic UI Updates

- Leverage RxDB's reactive nature for optimistic UI updates.
- Use RxDB's change events to keep the UI in sync with local data changes.

### 4.5 GraphQL Integration

- Use RxDB's GraphQL replication plugin for server synchronization.
- Configure GraphQL endpoints and query/mutation builders as per RxDB's requirements.

## 5. User Interface

### 5.1 Design Principles

- Adhere to Material Design 3 guidelines, adapted for desktop experiences.
- Optimize for 13" to 27" screens.

### 5.2 Responsiveness

- Ensure responsive design for various desktop/laptop screen sizes.

### 5.3 Performance

- Implement virtualization for long lists and large datasets.
- Use RxDB's indexing capabilities to optimize local queries.

### 5.4 Accessibility

- Ensure WCAG 2.1 AA compliance.

### 5.5 Sync Status Indicators

- Provide clear visual indicators for sync status using RxDB's replication state.

### 5.6 Network Status Indicators

- Develop clear visual indicators for the app's online/offline status.

## 6. Development Practices

### 6.1 Code Quality

- Strictly adhere to TypeScript and Next.js typing rules.
- Follow ESLint and Prettier configurations.
- Implement error handling for both local operations and synchronization issues.

### 6.2 Documentation

- Provide inline documentation for RxDB-specific implementations.
- Include the file location as a comment at the top of each file.
- Document local-first patterns and best practices used in the app.

### 6.3 Performance Monitoring

- Use RxDB's built-in logging and monitoring capabilities.

## 7. Developer Experience

### 7.1 Setup and Installation

- Provide clear, step-by-step documentation for setting up the development environment.
- Include instructions for testing local-first functionality.

### 7.2 Scripts and Commands

- Include and document npm scripts for common development tasks.

## 8. Learning Resources

### 8.1 README.md

- Create a comprehensive README.md with:
  - Project overview and architecture explanation, emphasizing local-first design
  - Setup and installation instructions
  - Available scripts and their purposes
  - Common troubleshooting steps

### 8.2 Code Examples

- Provide example usage patterns and code snippets for common RxDBDotNet scenarios, focusing on built-in features.

### 8.3 Local-First Patterns Guide

- Create documentation detailing the implemented local-first patterns using RxDB's built-in capabilities.
- Provide code examples and best practices for each local-first pattern.

## 9. Performance Optimization

### 9.1 Local Data Management

- Utilize RxDB's indexing capabilities for efficient local data access.
- Implement RxDB's query caching for frequently accessed data.

### 9.2 Synchronization Efficiency

- Leverage RxDB's built-in replication mechanisms for efficient synchronization.

## 10. Continuous Improvement

### 10.1 Updates

- Regularly review and update implementations to align with the latest RxDBDotNet features and best practices.

### 10.2 Community Engagement

- Include contribution guidelines to encourage community involvement.

This requirements document serves as a guide for developing the RxDBDotNet Example Client App, emphasizing its local-first nature and leveraging RxDB's built-in capabilities for synchronization and data management.
