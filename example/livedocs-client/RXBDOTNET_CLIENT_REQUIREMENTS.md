# RxDBDotNet Example Client App Requirements

## 1. Overview

The RxDBDotNet Example Client App is an offline-first JavaScript application designed to demonstrate advanced integration between RxDB on the client-side and the RxDBDotNet GraphQL API on the server-side. This web-based app, which can be used in various environments including desktop browsers, mobile devices, and potentially Electron or other JavaScript runtimes, goes beyond the basic examples provided in the RxDB project documentation. It showcases how to leverage the enhanced features of RxDBDotNet while prioritizing offline functionality and seamless synchronization.

Key aspects of this example app include:

1. **Offline-First Architecture**: The app is designed to work fully offline, with all core functionalities available without an internet connection. Data is stored locally and synced when a connection is available.

2. **Advanced GraphQL Integration**: The app demonstrates how to customize replicators to take full advantage of RxDBDotNet's advanced features, including:

   - Custom GraphQL queries and mutations implemented in dedicated replication files
   - GraphQL filtering for efficient server-side data retrieval
   - Subscription topics for fine-grained real-time updates
   - Enhanced error handling utilizing Hot Chocolate's mutation conventions

3. **Custom Replication Logic**: The app illustrates how to write custom replication logic (as seen in `workspaceReplication.ts`, `userReplication.ts`, and `liveDocReplication.ts`) to fully leverage RxDBDotNet's capabilities, going beyond the basic usage of RxDB's built-in GraphQL plugin methods.

This example app serves as a comprehensive learning resource for software engineers looking to build robust, offline-first web applications using RxDBDotNet. It bridges the gap between basic RxDB GraphQL examples and the advanced features offered by RxDBDotNet, providing practical implementations and best practices for offline-first development.

## 2. Technology Stack

- Frontend: Next.js, TypeScript, Material-UI (with Material Design 3 principles)
- State Management: React hooks, RxDB
- API: GraphQL
- Offline Support: RxDB's local storage and GraphQL replication protocol

## 3. Core Functionality

### 3.1 Document Types

- Implement CRUD operations for:
  - Workspace
  - User
  - LiveDoc

### 3.2 Offline-First Operations

- Implement local-first data storage using RxDB for all document types
- Ensure all CRUD operations work offline and are performed on local data first
- Implement RxDB's observable queries to keep the UI in sync with local data changes
- Provide a mechanism to queue offline changes for later synchronization
- Implement optimistic UI updates for immediate user feedback
- Develop a strategy to revert or retry changes if synchronization fails

### 3.3 Synchronization

- Implement bi-directional replication for all document types using custom GraphQL queries and mutations
- Develop custom replication logic in dedicated files (`workspaceReplication.ts`, `userReplication.ts`, `liveDocReplication.ts`) to handle complex synchronization scenarios
- Implement custom `pullQueryBuilder`, `pushQueryBuilder`, and `pullStreamBuilder` functions for each document type
- Ensure custom replication logic handles GraphQL filtering, subscription topics, and enhanced error handling
- Implement batch synchronization to optimize network usage
- Provide clear indicators of synchronization status in the UI
- Implement a robust retry mechanism for failed synchronizations
- Ensure the app can handle intermittent connectivity gracefully

### 3.4 Authentication

- Implement simplified "Login As" functionality
- Allow selection of predefined users with specific roles within workspaces
- Use JWT tokens for API requests and subscriptions when online
- Handle authentication state persistently for offline use
- Implement secure storage of authentication tokens for offline use
- Ensure the app can re-authenticate smoothly when transitioning from offline to online

## 4. Advanced RxDBDotNet Features

### 4.1 Conflict Resolution

- Implement RxDB's conflict resolution hooks to define custom merge strategies
- Develop a "last write wins" strategy for simple conflicts
- For complex conflicts, implement a system to store both versions and prompt user for resolution
- Provide clear UI feedback during conflict resolution processes

### 4.2 Error Handling

- Implement comprehensive error catching and logging for both online and offline scenarios
- Develop clear user feedback mechanisms for the status of offline actions
- Create a system to queue and retry failed operations when the app comes online
- Implement specific error handling for synchronization failures

### 4.3 Subscription Management

- Demonstrate subscription filtering
- Implement efficient batching strategies
- Handle subscription reconnection after offline periods
- Implement a mechanism to handle subscription data locally when offline and resync when online

### 4.4 Optimistic UI Updates

- Implement immediate UI updates for user actions, regardless of online status
- Develop a local queue system for storing changes that need to be synchronized
- Create a mechanism to revert optimistic updates if server synchronization fails
- Provide visual indicators for data that has not yet been synced with the server

### 4.5 Custom GraphQL Integration

- Implement custom GraphQL queries for pulling data in each replication file
- Develop custom GraphQL mutations for pushing data changes to the server
- Create custom GraphQL subscriptions for real-time updates
- Ensure all custom GraphQL operations align with the RxDBDotNet server-side implementation
- Implement proper error handling and type safety in all custom GraphQL operations

## 5. User Interface

### 5.1 Design Principles

- Adhere to Material Design 3 guidelines, adapted for desktop experiences
- Optimize for 13" to 27" screens

### 5.2 Responsiveness

- Ensure responsive design for various desktop/laptop screen sizes

### 5.3 Performance

- Implement virtualization for long lists and large datasets
- Optimize for extended use sessions, including offline periods

### 5.4 Accessibility

- Ensure WCAG 2.1 AA compliance

### 5.5 Offline Indicators

- Provide clear visual indicators for offline status
- Show sync status and pending changes

### 5.6 Offline Mode Indicators

- Develop clear visual indicators for the app's online/offline status
- Implement item-level sync status indicators for data that hasn't been synchronized
- Create intuitive UI elements to show pending offline changes
- Design user-friendly prompts for conflict resolution scenarios

## 6. Development Practices

### 6.1 Code Quality

- Strictly adhere to TypeScript and Next.js typing rules
- Follow ESLint and Prettier configurations
- Implement comprehensive error handling and logging for both online and offline scenarios

### 6.2 Documentation

- Provide inline documentation for complex RxDBDotNet-specific implementations, especially in custom replication files
- Include file locations as comments at the top of each file
- Document offline-first patterns and best practices used in the app
- Create detailed documentation explaining the custom GraphQL queries, mutations, and subscriptions in each replication file

### 6.3 Performance Monitoring

- Integrate performance monitoring tools or custom logging for RxDB operations
- Provide guidance on identifying and resolving performance bottlenecks, especially for offline use cases

### 6.4 Offline-First Development Workflow

- Establish development practices that prioritize offline functionality from the outset
- Implement tools and scripts to simulate various network conditions during development
- Create guidelines for developers to consistently implement offline-first patterns

## 7. Developer Experience

### 7.1 Setup and Installation

- Provide clear, step-by-step documentation for setting up the development environment
- Include instructions for testing offline functionality

### 7.2 Scripts and Commands

- Include and document npm scripts for common development tasks
- Provide scripts for simulating offline scenarios

### 7.3 Offline Testing Tools

- Develop and document tools for simulating offline scenarios and network transitions
- Create scripts to generate test data for large offline datasets
- Implement automated tests for offline functionality and synchronization processes

## 8. Learning Resources

### 8.1 README.md

- Create a comprehensive README.md with:
  - Project overview and architecture explanation, emphasizing offline-first design
  - Setup and installation instructions
  - Available scripts and their purposes
  - Common troubleshooting steps, including offline-related issues

### 8.2 Code Examples

- Provide example usage patterns and code snippets for common RxDBDotNet scenarios, with emphasis on offline-first patterns and custom GraphQL integration

### 8.3 Performance Optimization Guide

- Document best practices for optimizing RxDB usage in large-scale, offline-first applications

### 8.4 Offline-First Patterns Guide

- Create comprehensive documentation (offline-first-patterns.md) detailing the implemented offline-first patterns
- Provide code examples and best practices for each offline-first pattern
- Include troubleshooting guides for common offline-related issues

### 8.5 Custom Replication Guide

- Create a comprehensive guide explaining the custom replication logic implemented in `workspaceReplication.ts`, `userReplication.ts`, and `liveDocReplication.ts`
- Provide detailed explanations of custom `pullQueryBuilder`, `pushQueryBuilder`, and `pullStreamBuilder` functions
- Include best practices for implementing custom GraphQL operations in the context of RxDBDotNet

## 9. Performance Optimization

### 9.1 Local Data Management

- Implement efficient indexing strategies for RxDB collections to optimize offline query performance
- Develop data pruning mechanisms to manage the size of offline storage
- Implement lazy loading or pagination techniques for large offline datasets

### 9.2 Synchronization Efficiency

- Optimize the synchronization process to minimize data transfer
- Implement delta updates to sync only changed data when possible
- Develop efficient batch processing for bulk synchronization operations

## 10. Testing and Quality Assurance

### 10.1 Unit Testing

- Implement unit tests for critical components and RxDB operations
- Include specific tests for offline functionality

### 10.2 Integration Testing

- Create integration tests to ensure proper interaction between components and with the RxDBDotNet API
- Develop tests that simulate offline scenarios and synchronization

### 10.3 Performance Testing

- Conduct performance tests to ensure the application can handle large datasets and extended use sessions, including offline periods

### 10.4 Offline Scenario Testing

- Develop a comprehensive test suite covering various offline scenarios
- Implement automated tests that simulate network transitions and offline data operations
- Create performance tests to ensure the app functions smoothly with large offline datasets

## 11. Continuous Improvement

### 11.1 Updates

- Regularly review and update implementations to align with the latest RxDBDotNet features and best practices
- Stay updated with evolving offline-first web application patterns

### 11.2 Community Engagement

- Include contribution guidelines to encourage community involvement
- Encourage contributions that enhance offline capabilities

### 11.3 Roadmap

- Provide a roadmap for future enhancements and features, including advanced offline capabilities

## 12. Deployment and Distribution

### 12.1 Build Process

- Document the build process for creating production-ready versions of the application
- Ensure offline capabilities are properly bundled and configured

### 12.2 Deployment Guide

- Provide instructions for deploying the application in various environments
- Include considerations for offline-first deployment strategies

This requirements document serves as a comprehensive guide for developing the RxDBDotNet Example Client App, with a strong emphasis on its offline-first nature and advanced synchronization capabilities.
