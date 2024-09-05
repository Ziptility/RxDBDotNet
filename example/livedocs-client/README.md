# LiveDocs Example Client

This project is a **work-in-progress** example client for the LiveDocs system, demonstrating real-time collaborative document management using RxDB, GraphQL, and React. **Please note that this is not a fully functional client application yet.**

## Overview

LiveDocs is an example real-time collaborative document management system that allows users to create and manage workspaces, users, and live documents. This client application provides a user interface for interacting with the LiveDocs system, showcasing the following features:

- Real-time synchronization with the RxDBDotNet GraphQL backend
- Offline-first capabilities using RxDB
- CRUD operations for Workspaces, Users, and LiveDocs
- Responsive UI built with Material-UI

## Technologies Used

- [Next.js](https://nextjs.org/) - React framework for building server-side rendered and static web applications
- [React](https://reactjs.org/) - JavaScript library for building user interfaces
- [TypeScript](https://www.typescriptlang.org/) - Typed superset of JavaScript
- [RxDB](https://rxdb.info/) - Offline-first, reactive database for JavaScript applications
- [Material-UI](https://material-ui.com/) - React UI framework implementing Google's Material Design
- [GraphQL](https://graphql.org/) - Query language for APIs

## Prerequisites

Before running this application, make sure you have the following installed:

- Node.js (v14 or later)
- npm (v6 or later)

## Getting Started

1. Clone the repository:

```bash
git clone https://github.com/Ziptility/RxDBDotNet.git
cd example/livedocs-client
```

2. Install dependencies:

```bash
npm install
```

3. Run the development server:

```bash
npm run dev
```

5. Open [http://localhost:3000](http://localhost:3000) in your browser to see the application.

## Project Structure

- `src/components/`: React components for the application
- `src/lib/`: Utility functions, database setup, and replication logic
- `src/pages/`: Next.js pages and API routes
- `src/styles/`: Global styles
- `src/types/`: TypeScript type definitions
