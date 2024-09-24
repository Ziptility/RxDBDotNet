# Offline-First Patterns in RxDBDotNet Example Client App

This document outlines the offline-first patterns implemented in the RxDBDotNet Example Client App. These patterns ensure a seamless user experience regardless of network connectivity and demonstrate best practices for building robust offline-capable applications with RxDBDotNet.

## Table of Contents

1. [Single Source of Truth](#single-source-of-truth)
2. [Offline-First Data Flow](#offline-first-data-flow)
3. [Synchronization Strategies](#synchronization-strategies)
4. [Conflict Resolution](#conflict-resolution)
5. [Optimistic UI Updates](#optimistic-ui-updates)
6. [Error Handling and Recovery](#error-handling-and-recovery)
7. [Offline-Aware UI/UX](#offline-aware-uiux)
8. [Data Consistency and Integrity](#data-consistency-and-integrity)
9. [Performance Optimization](#performance-optimization)

## Single Source of Truth

Implement a single source of truth using RxDB as the local database, which serves as the primary data source for the application.

### Implementation

- Use RxDB collections as the authoritative source for all application data.
- Ensure all UI components read data exclusively from RxDB, not directly from API responses.
- Implement a repository pattern to abstract data access and manage the single source of truth.

### Example

```typescript
class UserRepository {
  private collection: RxCollection<User>;

  constructor(db: RxDatabase) {
    this.collection = db.users;
  }

  async getUser(id: string): Promise<User | null> {
    return this.collection.findOne(id).exec();
  }

  async updateUser(id: string, data: Partial<User>): Promise<void> {
    await this.collection.findOne(id).update(data);
    // Trigger synchronization
    await this.sync();
  }

  private async sync(): Promise<void> {
    // Implement synchronization logic
  }
}

// Usage in a component
const UserProfile: React.FC<{ userId: string }> = ({ userId }) => {
  const repository = useUserRepository();
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const subscription = repository.getUser(userId).$.subscribe(setUser);
    return () => subscription.unsubscribe();
  }, [userId, repository]);

  // Render user profile
};
```

## Offline-First Data Flow

Design the data flow to prioritize offline functionality, ensuring the app works seamlessly without an internet connection.

### Implementation

- Implement a write-through cache pattern using RxDB.
- Perform all write operations on the local database first.
- Queue changes for later synchronization with the server.
- Use RxDB's change detection to trigger synchronization attempts.

### Example

```typescript
async function createDocument(data: DocumentData): Promise<void> {
  // Insert into local RxDB first
  const newDoc = await documentsCollection.insert(data);

  // Queue for synchronization
  await syncQueue.add(async () => {
    try {
      await synchronize(newDoc);
    } catch (error) {
      // Handle sync error
      console.error('Sync failed:', error);
      // Mark document for retry
      await newDoc.patch({ syncStatus: 'failed' });
    }
  });
}

// Synchronization function
async function synchronize(doc: RxDocument): Promise<void> {
  const response = await fetch('/api/documents', {
    method: 'POST',
    body: JSON.stringify(doc.toJSON()),
  });

  if (!response.ok) {
    throw new Error('Sync failed');
  }

  // Update local document with server response if needed
  const serverData = await response.json();
  await doc.patch({ syncStatus: 'synced', ...serverData });
}
```

## Synchronization Strategies

Implement flexible synchronization strategies to efficiently handle various network conditions and data types.

### Implementation

- Use incremental synchronization to minimize data transfer.
- Implement different sync strategies based on data criticality:
  - Real-time sync for critical data
  - Background sync for non-critical updates
  - On-demand sync for large datasets or user-initiated actions
- Use RxDB's GraphQL replication for efficient bi-directional synchronization.

### Example

```typescript
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';

// Real-time sync for critical data
const criticalDataReplication = replicateGraphQL({
  collection: criticalDataCollection,
  url: 'https://api.example.com/graphql',
  pull: {
    queryBuilder: pullQueryBuilder,
    modifier: (doc) => doc,
  },
  push: {
    queryBuilder: pushQueryBuilder,
    modifier: (doc) => doc,
  },
  live: true,
  retryTime: 1000,
  waitForLeadership: true,
});

// Background sync for non-critical updates
const backgroundSync = async () => {
  const docsToSync = await nonCriticalCollection
    .find({
      selector: {
        syncStatus: 'pending',
      },
    })
    .exec();

  for (const doc of docsToSync) {
    try {
      await synchronize(doc);
    } catch (error) {
      console.error('Background sync failed:', error);
    }
  }
};

// Schedule background sync
setInterval(backgroundSync, 5 * 60 * 1000); // Every 5 minutes

// On-demand sync for large datasets
const syncLargeDataset = async () => {
  const replicationState = replicateGraphQL({
    collection: largeDatasetCollection,
    url: 'https://api.example.com/graphql',
    pull: {
      queryBuilder: pullQueryBuilder,
      modifier: (doc) => doc,
    },
    push: {
      queryBuilder: pushQueryBuilder,
      modifier: (doc) => doc,
    },
    live: false,
  });

  await replicationState.awaitInitialReplication();
  await replicationState.cancel();
};
```

## Conflict Resolution

Implement robust conflict resolution strategies to handle scenarios where offline changes conflict with server-side updates.

### Implementation

- Use RxDB's conflict resolution hooks to define custom merge strategies.
- Implement a "last write wins" strategy for simple conflicts.
- For complex conflicts, store both versions and prompt user for resolution.
- Use version vectors or timestamps to detect conflicts accurately.

### Example

```typescript
myCollection.setConflictHandler((documentInDatabase, documentInSync) => {
  if (documentInDatabase.updatedAt > documentInSync.updatedAt) {
    return documentInDatabase;
  } else if (documentInDatabase.updatedAt < documentInSync.updatedAt) {
    return documentInSync;
  } else {
    // Same timestamp, need manual resolution
    return {
      ...documentInDatabase,
      _conflicts: [documentInDatabase, documentInSync],
    };
  }
});

// In the UI, check for conflicts and prompt user if needed
const ConflictResolutionPrompt: React.FC<{ doc: RxDocument }> = ({ doc }) => {
  if (doc._conflicts) {
    return (
      <div>
        <h3>Conflict Detected</h3>
        <p>Please choose the correct version:</p>
        {doc._conflicts.map((version, index) => (
          <button key={index} onClick={() => resolveConflict(doc, version)}>
            Version {index + 1}
          </button>
        ))}
      </div>
    );
  }
  return null;
};

async function resolveConflict(doc: RxDocument, chosenVersion: any): Promise<void> {
  await doc.patch(chosenVersion);
  await doc.update({
    $unset: { _conflicts: true },
  });
}
```

## Optimistic UI Updates

Implement optimistic UI updates to provide immediate feedback to users, even when offline.

### Implementation

- Update the UI immediately upon user action.
- Store changes in a local queue for later synchronization.
- Implement a rollback mechanism in case of sync failures.
- Use RxDB's change events to keep the UI in sync with local data changes.

### Example

```typescript
async function updateDocument(id: string, newData: Partial<Document>): Promise<void> {
  const doc = await documentsCollection.findOne(id).exec();
  if (!doc) {
    throw new Error('Document not found');
  }

  // Optimistically update the document
  await doc.update(newData);

  try {
    // Attempt to sync with the server
    await syncDocument(doc);
  } catch (error) {
    // If sync fails, revert the change
    await doc.update({ $set: doc._rev });
    throw new Error('Failed to update document');
  }
}

// In a React component
const DocumentEditor: React.FC<{ documentId: string }> = ({ documentId }) => {
  const [doc, setDoc] = useState(null);

  useEffect(() => {
    const subscription = documentsCollection.findOne(documentId).$.subscribe(setDoc);
    return () => subscription.unsubscribe();
  }, [documentId]);

  const handleUpdate = async (newData: Partial<Document>) => {
    try {
      await updateDocument(documentId, newData);
    } catch (error) {
      // Show error to user
      console.error('Update failed:', error);
    }
  };

  // Render document editor
};
```

## Error Handling and Recovery

Implement comprehensive error handling to manage offline scenarios, synchronization issues, and data inconsistencies.

### Implementation

- Implement retry mechanisms with exponential backoff for failed network requests.
- Provide clear feedback to users about the status of their actions.
- Implement a way to manually trigger synchronization for failed operations.
- Use error boundaries in React to catch and handle errors gracefully.

### Example

```typescript
class SyncError extends Error {
  constructor(public readonly document: RxDocument, message?: string) {
    super(message);
    this.name = 'SyncError';
  }
}

async function syncWithRetry(doc: RxDocument, maxRetries = 5): Promise<void> {
  let retries = 0;
  while (retries < maxRetries) {
    try {
      await synchronize(doc);
      return;
    } catch (error) {
      retries++;
      if (retries >= maxRetries) {
        throw new SyncError(doc, 'Max retries reached');
      }
      // Exponential backoff
      await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, retries)));
    }
  }
}

// Error Boundary Component
class ErrorBoundary extends React.Component {
  state = { hasError: false, error: null };

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error('Uncaught error:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return <h1>Something went wrong. Please try again.</h1>;
    }

    return this.props.children;
  }
}
```

## Offline-Aware UI/UX

Design the user interface to provide clear indications of the app's online/offline status and the sync state of data.

### Implementation

- Display an offline indicator when the app is not connected.
- Show sync status for individual items or data sets.
- Provide visual cues for data that hasn't been synced yet.
- Implement a "retry" mechanism for failed operations.

### Example

```typescript
const OfflineIndicator: React.FC = () => {
  const isOnline = useOnlineStatus();
  return (
    <div className={`status-indicator ${isOnline ? 'online' : 'offline'}`}>
      {isOnline ? 'Online' : 'Offline'}
    </div>
  );
};

const SyncStatusIcon: React.FC<{ syncStatus: 'pending' | 'synced' | 'failed' }> = ({ syncStatus }) => {
  const iconMap = {
    pending: '🕒',
    synced: '✅',
    failed: '❌',
  };
  return <span title={`Sync status: ${syncStatus}`}>{iconMap[syncStatus]}</span>;
};

const DocumentList: React.FC = () => {
  const [documents, setDocuments] = useState([]);

  useEffect(() => {
    const subscription = documentsCollection
      .find()
      .$
      .subscribe(setDocuments);
    return () => subscription.unsubscribe();
  }, []);

  return (
    <ul>
      {documents.map(doc => (
        <li key={doc.id}>
          {doc.title} <SyncStatusIcon syncStatus={doc.syncStatus} />
          {doc.syncStatus === 'failed' && (
            <button onClick={() => syncWithRetry(doc)}>Retry Sync</button>
          )}
        </li>
      ))}
    </ul>
  );
};
```

## Data Consistency and Integrity

Ensure data consistency and integrity across offline and online states.

### Implementation

- Use schemas to validate data before insertion or update.
- Implement data migration strategies for schema changes.
- Use transactions for operations that require multiple changes.
- Implement periodic consistency checks and auto-healing mechanisms.

### Example

```typescript
const userSchema = {
  title: 'user schema',
  version: 0,
  type: 'object',
  properties: {
    id: {
      type: 'string',
      primary: true,
    },
    name: {
      type: 'string',
    },
    email: {
      type: 'string',
      format: 'email',
    },
    age: {
      type: 'integer',
      minimum: 0,
      maximum: 150,
    },
    syncStatus: {
      type: 'string',
      enum: ['pending', 'synced', 'failed'],
    },
  },
  required: ['id', 'name', 'email', 'syncStatus'],
};

const db = await createRxDatabase({
  name: 'exampledb',
  adapter: 'idb',
  schema: {
    users: userSchema,
  },
});

// Data migration
const migrationStrategies = {
  1: (oldDoc) => {
    oldDoc.age = oldDoc.age || 0;
    return oldDoc;
  },
  2: (oldDoc) => {
    oldDoc.syncStatus = oldDoc.syncStatus || 'pending';
    return oldDoc;
  },
};

// Consistency check
async function performConsistencyCheck(): Promise<void> {
  const inconsistentDocs = await db.users
    .find({
      selector: {
        $or: [{ age: { $lt: 0 } }, { age: { $gt: 150 } }, { syncStatus: { $nin: ['pending', 'synced', 'failed'] } }],
      },
    })
    .exec();

  for (const doc of inconsistentDocs) {
    await doc.update({
      $set: {
        age: Math.max(0, Math.min(doc.age, 150)),
        syncStatus: 'pending',
      },
    });
  }
}

// Run consistency check periodically
setInterval(performConsistencyCheck, 24 * 60 * 60 * 1000); // Daily
```

## Performance Optimization

Optimize performance for a smooth offline experience, especially with large datasets.

### Implementation

- Use RxDB's indexing capabilities to optimize local queries.
- Implement pagination or virtual scrolling for large lists.
- Use efficient querying techniques to minimize data processing on the client.
- Implement data pruning strategies to manage local storage size.
- Optimize the frequency and payload of synchronization operations.

### Example

```typescript
// Implementing pagination
async function getPaginatedDocuments(page: number, pageSize: number): Promise<Document[]> {
  return await documentsCollection
    .find()
    .skip(page * pageSize)
    .limit(pageSize)
    .exec();
}

// Using compound indexes for efficient querying
const userSchema = {
  // ... other schema properties
  indexes: ['name', ['email', 'age']],
};

// Efficient querying using indexes
const result = await usersCollection
  .find({
    selector: {
      email: 'user@example.com',
      age: { $gt: 18 },
    },
  })
  .exec();

// Data pruning strategy
async function pruneOldData(): Promise<void> {
  const oneMonthAgo = new Date();
  oneMonthAgo.setMonth(oneMonthAgo.getMonth() - 1);

  await documentsCollection
    .find({
      selector: {
        updatedAt: { $lt: oneMonthAgo.toISOString() },
        isImportant: false,
      },
    })
    .remove();
}

// Optimize synchronization payload
function prepareSyncPayload(doc: RxDocument): Record<string, unknown> {
  const { id, updatedAt, ...syncFields } = doc.toJSON();
  return {
    id,
    updatedAt,
    ...syncFields,
  };
}

async function synchronizeDocument(doc: RxDocument): Promise<void> {
  const payload = prepareSyncPayload(doc);
  await fetch(`/api/documents/${doc.id}`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  });
}
```

By implementing these patterns and testing strategies, the RxDBDotNet Example Client App demonstrates a robust offline-first architecture that provides a seamless user experience across various network conditions. These patterns can be adapted and extended to suit the specific needs of different applications built with RxDBDotNet.

Remember to continuously review and update these implementations as RxDBDotNet evolves and new best practices emerge in the offline-first application development community.
