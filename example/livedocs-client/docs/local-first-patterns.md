<!-- docs/local-first-patterns.md -->

# Local-First Patterns in RxDBDotNet Example Client App

This document outlines the local-first patterns implemented in the RxDBDotNet Example Client App. These patterns leverage RxDB's built-in functionality to ensure a superior user experience with instant responsiveness, seamless offline capabilities, and efficient data synchronization. They demonstrate best practices for building robust local-first applications with RxDBDotNet while minimizing custom code.

## Table of Contents

- [Local-First Patterns in RxDBDotNet Example Client App](#local-first-patterns-in-rxdbdotnet-example-client-app)
  - [Table of Contents](#table-of-contents)
  - [Local-First Data Flow](#local-first-data-flow)
    - [Implementation](#implementation)
    - [Example](#example)
  - [Instant Responsiveness](#instant-responsiveness)
    - [Example](#example-1)
  - [Synchronization](#synchronization)
    - [Implementation](#implementation-1)
    - [Example](#example-2)
  - [Conflict Resolution](#conflict-resolution)
    - [Implementation](#implementation-2)
    - [Example](#example-3)
  - [Optimistic UI Updates](#optimistic-ui-updates)
    - [Implementation](#implementation-3)
    - [Example](#example-4)
  - [Error Handling](#error-handling)
    - [Implementation](#implementation-4)
    - [Example](#example-5)
  - [Local-Aware UI/UX](#local-aware-uiux)
    - [Implementation](#implementation-5)
    - [Example](#example-6)
  - [Performance Optimization](#performance-optimization)
    - [Implementation](#implementation-6)
    - [Example](#example-7)
  - [Offline-First Approach](#offline-first-approach)
    - [Implementation](#implementation-7)
    - [Example](#example-8)
  - [Network Status Management](#network-status-management)
    - [Implementation](#implementation-8)
    - [Example](#example-9)
  - [Sync Recovery](#sync-recovery)
    - [Implementation](#implementation-9)
    - [Example](#example-10)

## Local-First Data Flow

Design the data flow to prioritize local functionality, ensuring the app works seamlessly regardless of network status.

### Implementation

- Use RxDB as the primary data store for all operations.
- Perform all read and write operations directly on the local RxDB instance.
- Rely on RxDB's built-in replication mechanisms for synchronization with the server.

### Example

```typescript
import { createRxDatabase } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';

const db = await createRxDatabase({
  name: 'exampledb',
  storage: getRxStorageDexie(),
});

const collections = await db.addCollections({
  documents: {
    schema: mySchema,
  },
});

// All operations are performed on the local database
async function createDocument(data) {
  await collections.documents.insert(data);
}

async function readDocument(id) {
  return await collections.documents.findOne(id).exec();
}

async function updateDocument(id, data) {
  const doc = await collections.documents.findOne(id).exec();
  await doc.update(data);
}
```

## Instant Responsiveness

Implement patterns that prioritize immediate user feedback:

1. Local-First Data Operations: Perform all data operations on the local RxDB instance.
2. Optimistic UI Updates: Update the UI immediately based on local operations.
3. Background Synchronization: Rely on RxDB's built-in replication for background syncing.

### Example

```typescript
import { useRxCollection } from 'rxdb-hooks';

function DocumentEditor({ documentId }) {
  const collection = useRxCollection('documents');
  const [doc, setDoc] = useState(null);

  useEffect(() => {
    const subscription = collection.findOne(documentId).$.subscribe(setDoc);
    return () => subscription.unsubscribe();
  }, [documentId, collection]);

  const handleUpdate = async (newData) => {
    try {
      await collection.findOne(documentId).update(newData);
      // UI updates automatically due to subscription
    } catch (error) {
      console.error('Update failed:', error);
    }
  };

  // Render document editor
}
```

## Synchronization

Leverage RxDB's built-in replication mechanisms for efficient synchronization with the server.

### Implementation

- Use RxDB's GraphQL replication plugin for bi-directional synchronization.
- Configure replication settings in the database setup.
- Handle network disconnections and reconnections gracefully.

### Example

```typescript
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';

const replicationState = replicateGraphQL({
  collection: collections.documents,
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
  retry: {
    autoReconnect: true,
    maxRetries: Infinity,
    retryDelay: (attempt) => Math.min(attempt * 1000, 30000), // Exponential backoff with max 30s delay
  },
});

// Optionally observe replication state
replicationState.error$.subscribe((error) => {
  console.error('Replication error:', error);
});

replicationState.active$.subscribe((active) => {
  console.log('Replication active:', active);
});
```

## Conflict Resolution

Implement conflict resolution using RxDB's built-in mechanisms.

### Implementation

- Define a custom conflict handler when creating collections.
- Use RxDB's conflict detection to handle synchronization conflicts.

### Example

```typescript
const myConflictHandler = (conflict) => {
  if (conflict.newDocumentState.updatedAt > conflict.realMasterState.updatedAt) {
    return conflict.newDocumentState;
  } else {
    return conflict.realMasterState;
  }
};

const collections = await db.addCollections({
  documents: {
    schema: mySchema,
    conflictHandler: myConflictHandler,
  },
});
```

## Optimistic UI Updates

Leverage RxDB's reactive nature for optimistic UI updates.

### Implementation

- Use RxDB's change events to keep the UI in sync with local data changes.
- Rely on RxDB's conflict resolution for handling synchronization issues.

### Example

```typescript
function DocumentList() {
  const collection = useRxCollection('documents');
  const [documents, setDocuments] = useState([]);

  useEffect(() => {
    const subscription = collection.find().$.subscribe(setDocuments);
    return () => subscription.unsubscribe();
  }, [collection]);

  // Render document list
  // UI updates automatically when documents change
}
```

## Error Handling

Implement error handling for local operations and synchronization issues, distinguishing between actual errors and expected offline scenarios.

### Implementation

- Use try-catch blocks for local database operations.
- Subscribe to RxDB's replication error events for synchronization issues.
- Implement retry mechanisms for network-related errors.
- Treat offline scenarios as normal operations, not errors.

### Example

```typescript
async function handleDatabaseOperation() {
  try {
    await collection.documents.insert(newDocument);
  } catch (error) {
    if (error.name === 'OfflineError') {
      console.log('Operation queued for sync:', error);
      // Handle offline scenario (e.g., show offline indicator)
    } else {
      console.error('Database operation failed:', error);
      // Handle other types of errors (e.g., show error message)
    }
  }
}

// For replication errors
replicationState.error$.subscribe((error) => {
  if (error.name === 'NetworkError') {
    console.log('Network error occurred, will retry:', error);
    // Handle network error (e.g., show reconnecting indicator)
  } else {
    console.error('Replication error:', error);
    // Handle other types of replication errors
  }
});
```

## Local-Aware UI/UX

Design the user interface to provide clear indications of the app's sync status and the local state of data, without alarming users during offline periods.

### Implementation

- Use RxDB's replication state to display sync status.
- Leverage RxDB's change events to show real-time updates in the UI.
- Implement subtle indicators for online/offline status.

### Example

```typescript
function SyncStatusIndicator() {
  const [syncStatus, setSyncStatus] = useState('idle');
  const [isOnline, setIsOnline] = useState(navigator.onLine);

  useEffect(() => {
    const subscription = replicationState.active$.subscribe(
      isActive => setSyncStatus(isActive ? 'syncing' : 'idle')
    );

    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      subscription.unsubscribe();
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return (
    <div>
      <Chip
        icon={isOnline ? <SyncIcon /> : <CloudOffIcon />}
        label={isOnline ? (syncStatus === 'syncing' ? 'Syncing...' : 'Synced') : 'Offline'}
        color={isOnline ? 'primary' : 'default'}
        size="small"
      />
    </div>
  );
}
```

## Performance Optimization

Optimize performance for a smooth local-first experience, especially with large datasets.

### Implementation

- Use RxDB's indexing capabilities to optimize local queries.
- Implement pagination or virtual scrolling for large lists.
- Leverage RxDB's query caching for frequently accessed data.

### Example

```typescript
// Define indexes in the schema
const documentsSchema = {
  version: 0,
  type: 'object',
  properties: {
    id: {
      type: 'string',
      primary: true,
    },
    title: {
      type: 'string',
    },
    createdAt: {
      type: 'number',
    },
  },
  indexes: ['createdAt'],
};

// Use efficient querying
const recentDocuments = await collection
  .find({
    selector: {
      createdAt: {
        $gt: Date.now() - 7 * 24 * 60 * 60 * 1000, // Last 7 days
      },
    },
    sort: [{ createdAt: 'desc' }],
    limit: 50,
  })
  .exec();

// Implement virtual scrolling for large lists
import { FixedSizeList } from 'react-window';

function DocumentList({ documents }) {
  const Row = ({ index, style }) => (
    <div style={style}>
      <DocumentItem document={documents[index]} />
    </div>
  );

  return (
    <FixedSizeList
      height={400}
      itemCount={documents.length}
      itemSize={50}
      width="100%"
    >
      {Row}
    </FixedSizeList>
  );
}
```

## Offline-First Approach

Design the application to work seamlessly offline, treating offline scenarios as normal operations.

### Implementation

- Ensure all core functionality works offline without errors.
- Implement local queueing of operations performed while offline.
- Provide clear, non-intrusive feedback about the offline state.

### Example

```typescript
function OfflineAwareButton({ onClick, children }) {
  const [isOnline, setIsOnline] = useState(navigator.onLine);

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const handleClick = async () => {
    try {
      await onClick();
    } catch (error) {
      if (!isOnline) {
        // Queue the operation for later
        await queueOfflineOperation(onClick);
        // Provide feedback to the user
        showToast('Action will be performed when online');
      } else {
        // Handle other types of errors
        console.error('Operation failed:', error);
      }
    }
  };

  return (
    <Button onClick={handleClick} disabled={!isOnline}>
      {children}
      {!isOnline && <OfflineIcon fontSize="small" />}
    </Button>
  );
}
```

## Network Status Management

Implement robust network status detection and management to ensure smooth transitions between online and offline states.

### Implementation

- Use the browser's online/offline events to detect network status changes.
- Implement a custom hook for network status management.
- Provide clear visual feedback about the current network status.

### Example

```typescript
function useNetworkStatus() {
  const [isOnline, setIsOnline] = useState(navigator.onLine);

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return isOnline;
}

function NetworkStatusIndicator() {
  const isOnline = useNetworkStatus();

  return (
    <Chip
      icon={isOnline ? <WifiIcon /> : <WifiOffIcon />}
      label={isOnline ? 'Online' : 'Offline'}
      color={isOnline ? 'success' : 'default'}
    />
  );
}
```

## Sync Recovery

Implement robust sync recovery mechanisms for when the app comes back online after being offline.

### Implementation

- Use RxDB's replication mechanisms to handle sync recovery.
- Implement a queue for operations performed while offline.
- Provide clear, non-intrusive feedback during the sync recovery process.

### Example

```typescript
function useSyncRecovery(replicationState) {
  const [isSyncing, setIsSyncing] = useState(false);
  const isOnline = useNetworkStatus();

  useEffect(() => {
    if (isOnline) {
      setIsSyncing(true);
      replicationState.reSync().then(() => {
        setIsSyncing(false);
        showToast('All changes synced');
      }).catch(error => {
        console.error('Sync failed:', error);
        showToast('Sync failed, will retry later');
      });
    }
  }, [isOnline, replicationState]);

  return isSyncing;
}

function SyncRecoveryIndicator({ replicationState }) {
  const isSyncing = useSyncRecovery(replicationState);

  if (!isSyncing) return null;

  return (
    <LinearProgress variant="indeterminate" />
  );
}
```

By implementing these local-first patterns, the RxDBDotNet Example Client App demonstrates a robust local-first architecture that leverages RxDB's built-in capabilities. This approach provides a seamless user experience across various network conditions while minimizing custom code and complexity. These patterns can be easily adapted and extended to suit the specific needs of different applications built with RxDBDotNet.

Remember to continuously review and update these implementations as RxDBDotNet evolves and new best practices emerge in the local-first application development community.
