# RxDBDotNet Example Client App Roadmap

## V1 MVP Enhancements

### 1. Enhance Local-First Data Flow

#### Update: src/lib/database.ts

The current implementation is a good start, but can be enhanced to fully leverage RxDB's local-first capabilities:

- Add indexes to optimize local queries. Currently, no indexes are defined, which can slow down queries on larger datasets.
- Implement data validation in the schema definitions to ensure data integrity at the client-side.
- Add custom conflict handlers for each collection to better manage synchronization conflicts.

Example enhancement:

```typescript
const userSchema = {
  ...existingSchema,
  indexes: ['email', 'workspaceId'],
  validationMethods: {
    validateEmail: (user) => {
      // Implement email validation logic
    },
  },
};

const collections = await db.addCollections({
  users: {
    schema: userSchema,
    conflictHandler: customUserConflictHandler,
  },
  // Add similar enhancements for other collections
});
```

#### Update: src/hooks/useDocuments.ts

The current implementation can be optimized for better offline support:

- Implement optimistic updates to improve perceived performance.
- Add error handling for offline scenarios.
- Implement a local-first querying strategy that prioritizes local data and falls back to remote data when necessary.

Example enhancement:

```typescript
const useDocuments = <T extends Document>(collectionName: keyof LiveDocsDatabase) => {
  // ... existing code ...

  const upsertDocument = async (doc: T) => {
    try {
      // Optimistic update
      setDocuments((prevDocs) => [...prevDocs.filter((d) => d.id !== doc.id), doc]);
      await collection.upsert(doc);
    } catch (error) {
      // Revert optimistic update if failed
      setDocuments((prevDocs) => prevDocs.filter((d) => d.id !== doc.id));
      // Handle offline scenario
      if (!navigator.onLine) {
        // Queue operation for later or notify user
      }
    }
  };

  // ... implement similar optimistic updates for other operations ...
};
```

### 2. Improve Synchronization

#### Update: src/lib/replication.ts

The current implementation can be enhanced to provide more robust synchronization:

- Implement more sophisticated error handling and retry logic.
- Add support for partial synchronization to handle large datasets more efficiently.
- Implement a mechanism to pause and resume synchronization based on network conditions.

Example enhancement:

```typescript
export const setupReplication = (db: LiveDocsDatabase, jwtAccessToken: string) => {
  // ... existing code ...

  const replicationStates = {
    workspaces: createWorkspaceReplicator(token, db.workspace, {
      retryInterval: 5000,
      maxRetries: 3,
      batchSize: 50,
    }),
    // ... similar enhancements for other replicators ...
  };

  // Implement pause/resume logic
  window.addEventListener('online', () => resumeAllReplications(replicationStates));
  window.addEventListener('offline', () => pauseAllReplications(replicationStates));

  return replicationStates;
};
```

#### Update: src/lib/workspaceReplication.ts, src/lib/userReplication.ts, src/lib/liveDocReplication.ts

These files can be enhanced to provide more efficient and flexible replication:

- Implement more granular pull queries to reduce data transfer.
- Add support for selective push operations to minimize conflicts.
- Implement custom data transformations to optimize data for client-side storage.

Example enhancement for src/lib/workspaceReplication.ts:

```typescript
const pullQueryBuilder = (
  variables: WorkspaceFilterInput
): RxGraphQLReplicationPullQueryBuilder<ReplicationCheckpoint> => {
  return (checkpoint, limit) => ({
    query: `
      query PullWorkspace($checkpoint: WorkspaceInputCheckpoint, $limit: Int!, $where: WorkspaceFilterInput) {
        pullWorkspace(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            name
            updatedAt
            isDeleted
            # Add any other necessary fields, but be selective to reduce data transfer
          }
          checkpoint {
            lastDocumentId
            updatedAt
          }
        }
      }
    `,
    variables: { checkpoint, limit, where: variables },
  });
};

// Implement similar enhancements for push operations and other replication files
```

### 3. Implement Conflict Resolution

#### Update: src/lib/database.ts

The current implementation lacks custom conflict handlers. Add them to manage synchronization conflicts effectively:

```typescript
const customWorkspaceConflictHandler: RxConflictHandler<Workspace> = async (
  i: RxConflictHandlerInput<Workspace>
): Promise<RxConflictHandlerOutput<Workspace>> => {
  if (i.newDocumentState.updatedAt > i.realMasterState.updatedAt) {
    return { isEqual: false, documentData: i.newDocumentState };
  }
  return { isEqual: false, documentData: i.realMasterState };
};

const collections = await db.addCollections({
  workspace: {
    schema: workspaceSchema,
    conflictHandler: customWorkspaceConflictHandler,
  },
  // Add similar conflict handlers for other collections
});
```

#### Update: src/hooks/useDocuments.ts

Enhance the hook to handle conflicts in CRUD operations:

```typescript
const useDocuments = <T extends Document>(collectionName: keyof LiveDocsDatabase) => {
  // ... existing code ...

  const upsertDocument = async (doc: T) => {
    try {
      await collection.upsert(doc);
    } catch (error) {
      if (error.code === 'CONFLICT') {
        // Handle conflict, possibly by showing a conflict resolution UI
        const conflictedDoc = await collection.findOne(doc.id).exec();
        // Implement logic to resolve conflict
      }
      // Handle other types of errors
    }
  };

  // Implement similar conflict handling for other operations
};
```

### 4. Enhance Optimistic UI Updates

#### Update: src/components/WorkspacesPageContent.tsx, src/components/UsersPageContent.tsx, src/components/LiveDocsPageContent.tsx

These components can be enhanced to provide a more responsive user experience through optimistic updates:

```typescript
const WorkspacesPageContent: React.FC = () => {
  // ... existing code ...

  const handleSubmit = useCallback(
    async (workspaceData) => {
      // Optimistic update
      setWorkspaces((prev) => [...prev, { ...workspaceData, id: 'temp-id' }]);

      try {
        const result = await upsertDocument(workspaceData);
        // Update with actual data from server
        setWorkspaces((prev) => prev.map((w) => (w.id === 'temp-id' ? result : w)));
      } catch (error) {
        // Revert optimistic update
        setWorkspaces((prev) => prev.filter((w) => w.id !== 'temp-id'));
        // Handle error
      }
    },
    [upsertDocument]
  );

  // Implement similar optimistic updates for update and delete operations
};
```

### 5. Improve Error Handling

#### Update: src/utils/errorHandling.ts

The current error handling can be expanded to cover more scenarios and provide more informative error messages:

```typescript
export const handleError = (error: unknown, context: string): void => {
  console.error(`Error in ${context}:`, error);

  let errorMessage: string;

  if (error instanceof RxError) {
    switch (error.code) {
      case 'CONFLICT':
        errorMessage = 'A conflict occurred. Please refresh and try again.';
        break;
      case 'NETWORK':
        errorMessage = 'Network error. Please check your connection.';
        break;
      // Add more specific error types
      default:
        errorMessage = 'An unexpected error occurred.';
    }
  } else if (error instanceof Error) {
    errorMessage = error.message;
  } else {
    errorMessage = 'An unknown error occurred';
  }

  toast.error(errorMessage, {
    position: 'top-right',
    autoClose: 5000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
  });
};
```

### 6. Enhance Local-Aware UI/UX

#### Update: src/components/NavigationRail.tsx

Add an offline mode indicator to clearly show the current network status:

```typescript
import { useOnlineStatus } from '@/hooks/useOnlineStatus';

const NavigationRail: React.FC = () => {
  const isOnline = useOnlineStatus();

  return (
    <Paper /* existing props */>
      {/* existing code */}
      <Box sx={{ p: 2 }}>
        <Chip
          icon={isOnline ? <WifiIcon /> : <WifiOffIcon />}
          label={isOnline ? 'Online' : 'Offline'}
          color={isOnline ? 'success' : 'error'}
        />
      </Box>
    </Paper>
  );
};
```

#### Update: src/components/NetworkStatus.tsx

Enhance the NetworkStatus component to show more detailed sync status:

```typescript
const NetworkStatus: React.FC = () => {
  const isOnline = useOnlineStatus();
  const [syncStatus, setSyncStatus] = useState<Record<string, boolean>>({});

  useEffect(() => {
    // Subscribe to replication states and update syncStatus
  }, []);

  return (
    <Tooltip title={
      <Box>
        {Object.entries(syncStatus).map(([name, active]) => (
          <Typography key={name} variant="caption">
            {`${name}: ${active ? 'Syncing' : 'Synced'}`}
          </Typography>
        ))}
      </Box>
    }>
      <Chip
        icon={isOnline ? (Object.values(syncStatus).some(Boolean) ? <SyncIcon /> : <CheckCircleIcon />) : <WifiOffIcon />}
        label={isOnline ? (Object.values(syncStatus).some(Boolean) ? 'Syncing' : 'Synced') : 'Offline'}
        color={isOnline ? (Object.values(syncStatus).some(Boolean) ? 'warning' : 'success') : 'error'}
      />
    </Tooltip>
  );
};
```

### 7. Optimize Performance

#### Update: src/lib/database.ts

Implement proper indexing for all collections to improve query performance:

```typescript
const workspaceSchema = {
  // ... existing schema ...
  indexes: ['name', 'updatedAt'],
};

const userSchema = {
  // ... existing schema ...
  indexes: ['email', 'role', 'workspaceId', 'updatedAt'],
};

const liveDocSchema = {
  // ... existing schema ...
  indexes: ['ownerId', 'workspaceId', 'updatedAt'],
};
```

#### Update: src/components/WorkspaceList.tsx, src/components/UserList.tsx, src/components/LiveDocList.tsx

Implement virtualization for large lists to improve performance:

```typescript
import { FixedSizeList as List } from 'react-window';

const WorkspaceList: React.FC<WorkspaceListProps> = ({ workspaces, ...props }) => {
  const Row = ({ index, style }) => {
    const workspace = workspaces[index];
    return (
      <div style={style}>
        {/* Render workspace item */}
      </div>
    );
  };

  return (
    <List
      height={400}
      itemCount={workspaces.length}
      itemSize={50}
      width="100%"
    >
      {Row}
    </List>
  );
};

// Implement similar virtualization for UserList and LiveDocList
```

### 8. Enhance Authentication Flow

#### Update: src/pages/login.tsx

Improve the "Login As" functionality to better simulate real-world scenarios:

```typescript
const LoginPage: React.FC = () => {
  // ... existing code ...

  const handleLogin = async (userId: string, workspaceId: string) => {
    try {
      await login(userId, workspaceId);
      // Simulate token refresh
      setInterval(
        () => {
          refreshToken();
        },
        60 * 60 * 1000
      ); // Refresh every hour
    } catch (error) {
      handleError(error, 'Login');
    }
  };

  // ... render login form ...
};
```

#### Update: src/contexts/AuthContext.tsx

Enhance token management and offline authentication support:

```typescript
const AuthProvider: React.FC = ({ children }) => {
  // ... existing code ...

  const refreshToken = useCallback(async () => {
    // Implement token refresh logic
    const newToken = await fetchNewToken();
    setJwtAccessToken(newToken);
    await updateReplicationToken(newToken);
  }, []);

  const login = useCallback(async (userId: string, workspaceId: string) => {
    // ... existing login logic ...

    // Store authentication data for offline access
    localStorage.setItem('auth', JSON.stringify({ userId, workspaceId, token }));
  }, []);

  useEffect(() => {
    // Check for stored authentication data on init
    const storedAuth = localStorage.getItem('auth');
    if (storedAuth) {
      const { userId, workspaceId, token } = JSON.parse(storedAuth);
      login(userId, workspaceId);
    }
  }, []);

  // ... render provider ...
};
```

### 9. Improve Documentation

#### Update: README.md

Enhance setup instructions and usage guidelines:

```markdown
# RxDBDotNet Example Client App

## Setup Instructions

1. Clone the repository
2. Install dependencies: `npm install`
3. Configure environment variables (see `.env.example`)
4. Start the development server: `npm run dev`

## Usage Guidelines

- Offline-First Operations: ...
- Synchronization: ...
- Conflict Resolution: ...
- Performance Optimization: ...

## Troubleshooting

- Sync Issues: ...
- Performance Problems: ...
```

#### Add: docs/local-first-patterns.md

Document implemented local-first patterns with code examples:

````markdown
# Local-First Patterns in RxDBDotNet Example Client App

## Optimistic Updates

We implement optimistic updates to provide instant feedback to users:

```typescript
const handleCreate = async (data) => {
  // Optimistic update
  setItems((prev) => [...prev, { ...data, id: 'temp-id' }]);

  try {
    const result = await createItem(data);
    // Update with actual data from server
    setItems((prev) => prev.map((item) => (item.id === 'temp-id' ? result : item)));
  } catch (error) {
    // Revert optimistic update
    setItems((prev) => prev.filter((item) => item.id !== 'temp-id'));
    handleError(error, 'Create Item');
  }
};
```
````

## Offline Support

...

```markdown

### 10. Implement Basic Monitoring

#### Add: src/utils/logging.ts

Implement basic logging for RxDB operations:

```typescript
import { RxDatabase } from 'rxdb';

export const setupLogging = (db: RxDatabase) => {
  db.$.subscribe(event => {
    console.log('Database event:', event);
  });

  Object.values(db.collections).forEach(collection => {
    collection.$.subscribe(event => {
      console.log(`Collection ${collection.name} event:`, event);
    });
  });
};
````

This roadmap provides a clear path to enhance the existing codebase to fully embrace local-first principles while showcasing the capabilities of RxDBDotNet. Each suggestion is accompanied by an explanation of why it's needed and example code to illustrate the proposed changes.
