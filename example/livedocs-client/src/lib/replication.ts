import { LiveDocsDatabase } from '@/lib/database';
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { createClient } from 'graphql-ws';
import { lastValueFrom, Subject } from 'rxjs';
import { RxCollection, RxReplicationState, ReplicationPullHandler, ReplicationPushHandler } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { WorkspaceDocType, UserDocType, LiveDocDocType } from './schemas';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT || 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT || 'ws://localhost:5414/graphql';

// Checkpoint type used for replication
type Checkpoint = {
  id: string;
  updatedAt: string;
};

// Generic type for document types
type DocType = WorkspaceDocType | UserDocType | LiveDocDocType;

// Helper function to create a pull handler
function createPullHandler<T extends DocType>(collectionName: string): ReplicationPullHandler<T, Checkpoint> {
  return {
    async handler(lastCheckpoint: Checkpoint | undefined, batchSize: number) {
      try {
        const response = await fetch(GRAPHQL_ENDPOINT, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            query: `
              query Pull${collectionName}($checkpoint: ${collectionName}InputCheckpoint, $limit: Int!) {
                pull${collectionName}(checkpoint: $checkpoint, limit: $limit) {
                  documents {
                    id
                    updatedAt
                    isDeleted
                    # Add other fields specific to this collection
                    ${getCollectionSpecificFields(collectionName)}
                  }
                  checkpoint {
                    id
                    updatedAt
                  }
                }
              }
            `,
            variables: {
              checkpoint: lastCheckpoint,
              limit: batchSize,
            },
          }),
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        if (result.errors) {
          throw new Error(result.errors[0].message);
        }

        return {
          documents: result.data[`pull${collectionName}`].documents,
          checkpoint: result.data[`pull${collectionName}`].checkpoint,
        };
      } catch (error) {
        logError(error as Error, `Pull handler for ${collectionName}`);
        throw error;
      }
    },
    batchSize: 50,
    modifier: (doc) => doc,
  };
}

// Helper function to create a push handler
function createPushHandler<T extends DocType>(collectionName: string): ReplicationPushHandler<T, any> {
  return {
    async handler(docs) {
      try {
        const response = await fetch(GRAPHQL_ENDPOINT, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            query: `
              mutation Push${collectionName}($input: Push${collectionName}Input!) {
                push${collectionName}(input: $input) {
                  ${collectionName.toLowerCase()} {
                    id
                    updatedAt
                    isDeleted
                    # Add other fields specific to this collection
                    ${getCollectionSpecificFields(collectionName)}
                  }
                }
              }
            `,
            variables: {
              input: {
                [`${collectionName.toLowerCase()}PushRow`]: docs.map((d) => ({
                  assumedMasterState: null,
                  newDocumentState: d,
                })),
              },
            },
          }),
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        if (result.errors) {
          throw new Error(result.errors[0].message);
        }

        return result.data[`push${collectionName}`][collectionName.toLowerCase()];
      } catch (error) {
        logError(error as Error, `Push handler for ${collectionName}`);
        throw error;
      }
    },
    batchSize: 5,
    modifier: (doc) => doc,
  };
}

// Helper function to get collection-specific fields
function getCollectionSpecificFields(collectionName: string): string {
  switch (collectionName) {
    case 'Workspace':
      return 'name';
    case 'User':
      return 'firstName lastName email role workspaceId';
    case 'LiveDoc':
      return 'content ownerId workspaceId';
    default:
      return '';
  }
}

// Helper function to set up replication for a collection
function setupReplicationForCollection<T extends DocType>(
  collection: RxCollection<T>,
  collectionName: string
): RxReplicationState<T, Checkpoint> {
  return replicateGraphQL<T, Checkpoint>({
    collection,
    url: {
      http: GRAPHQL_ENDPOINT,
      ws: WS_ENDPOINT,
    },
    pull: createPullHandler<T>(collectionName),
    push: createPushHandler<T>(collectionName),
    live: true,
    liveInterval: 1000 * 60 * 10, // 10 minutes
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
  });
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<RxReplicationState<DocType, Checkpoint>[]> => {
  const wsClient = createClient({
    url: WS_ENDPOINT,
    connectionParams: {
      // Add any authentication headers here
    },
  });

  const pullStream$ = new Subject<any>();

  wsClient.subscribe(
    {
      query: `
        subscription {
          streamWorkspace { documents { id name updatedAt isDeleted } checkpoint { id updatedAt } }
          streamUser { documents { id firstName lastName email role workspaceId updatedAt isDeleted } checkpoint { id updatedAt } }
          streamLiveDoc { documents { id content ownerId workspaceId updatedAt isDeleted } checkpoint { id updatedAt } }
        }
      `,
    },
    {
      next: (data) => {
        if (data.data.streamWorkspace) pullStream$.next(data.data.streamWorkspace);
        if (data.data.streamUser) pullStream$.next(data.data.streamUser);
        if (data.data.streamLiveDoc) pullStream$.next(data.data.streamLiveDoc);
      },
      error: (err) => {
        logError(err, 'WebSocket subscription');
        notifyUser('Error in live updates. Please refresh the page.');
      },
      complete: () => console.log('Subscription completed'),
    }
  );

  const replicationStates = [
    setupReplicationForCollection<WorkspaceDocType>(db.workspaces, 'Workspace'),
    setupReplicationForCollection<UserDocType>(db.users, 'User'),
    setupReplicationForCollection<LiveDocDocType>(db.liveDocs, 'LiveDoc'),
  ];

  try {
    await Promise.all(replicationStates.map((state) => 
      retryWithBackoff(() => lastValueFrom(state.awaitInitialReplication()))
    ));
    console.log('Initial replication completed successfully');
  } catch (error) {
    logError(error as Error, 'Initial replication');
    notifyUser('Failed to complete initial data sync. Some data may be outdated.');
  }

  // Set up error handling for ongoing replication
  replicationStates.forEach((state, index) => {
    const collectionNames = ['Workspace', 'User', 'LiveDoc'];
    state.error$.subscribe((error) => {
      const collectionName = collectionNames[index];
      logError(new ReplicationError(`Replication error in ${collectionName}`, error), 'Ongoing replication');

      if (error.parameters.direction === 'pull') {
        notifyUser(`Error fetching latest ${collectionName} data. Some information may be outdated.`, 'warning');
      } else if (error.parameters.direction === 'push') {
        notifyUser(`Error saving ${collectionName} changes. Please try again later.`, 'error');
      }

      // Implement retry logic
      retryWithBackoff(() => {
        console.log(`Retrying ${error.parameters.direction} replication for ${collectionName}...`);
        return lastValueFrom(state.reSync());
      }).catch((retryError) => {
        logError(retryError, `Retry failed for ${collectionName}`);
        notifyUser(`Persistent error in ${collectionName} synchronization. Please contact support.`, 'error');
      });
    });
  });

  return replicationStates;
};

// Function to cancel all replications
export const cancelAllReplications = (replicationStates: RxReplicationState<DocType, Checkpoint>[]) => {
  replicationStates.forEach(state => state.cancel());
};

// Function to resume all replications
export const resumeAllReplications = (replicationStates: RxReplicationState<DocType, Checkpoint>[]) => {
  replicationStates.forEach(state => state.reSync());
};