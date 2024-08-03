import { LiveDocsDatabase } from '@/lib/database';
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { createClient } from 'graphql-ws';
import { lastValueFrom, Subject } from 'rxjs';
import { RxCollection, RxReplicationState, ReplicationPullHandler, ReplicationPushHandler } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT || 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT || 'ws://localhost:5414/graphql';

// Define types for our document models
type WorkspaceDocument = {
  id: string;
  name: string;
  updatedAt: string;
  isDeleted: boolean;
};

type UserDocument = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: 'User' | 'Admin' | 'SuperAdmin';
  workspaceId: string;
  updatedAt: string;
  isDeleted: boolean;
};

type LiveDocDocument = {
  id: string;
  content: string;
  ownerId: string;
  workspaceId: string;
  updatedAt: string;
  isDeleted: boolean;
};

// Type for the checkpoint used in replication
type Checkpoint = {
  id: string;
  updatedAt: string;
};

// Helper function to create a pull handler
function createPullHandler<T>(collectionName: string): ReplicationPullHandler<T, Checkpoint> {
  return {
    async handler(lastCheckpoint: Checkpoint | undefined, batchSize: number) {
      const response = await fetch(GRAPHQL_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          query: `
            query Pull${collectionName}($checkpoint: ${collectionName}InputCheckpoint, $limit: Int!) {
              pull${collectionName}(checkpoint: $checkpoint, limit: $limit) {
                documents {
                  id
                  # Add other fields specific to this collection
                  updatedAt
                  isDeleted
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

      const result = await response.json();
      return {
        documents: result.data[`pull${collectionName}`].documents,
        checkpoint: result.data[`pull${collectionName}`].checkpoint,
      };
    },
    batchSize: 100,
    modifier: (doc) => doc,
  };
}

// Helper function to create a push handler
function createPushHandler<T>(collectionName: string): ReplicationPushHandler<T, any> {
  return {
    async handler(docs) {
      const response = await fetch(GRAPHQL_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          query: `
            mutation Push${collectionName}($${collectionName.toLowerCase()}PushRow: [${collectionName}InputPushRow!]) {
              push${collectionName}(${collectionName.toLowerCase()}PushRow: $${collectionName.toLowerCase()}PushRow) {
                id
                # Add other fields specific to this collection
                updatedAt
                isDeleted
              }
            }
          `,
          variables: {
            [`${collectionName.toLowerCase()}PushRow`]: docs.map((d) => ({
              assumedMasterState: null,
              newDocumentState: d,
            })),
          },
        }),
      });

      const result = await response.json();
      return result.data[`push${collectionName}`];
    },
    batchSize: 5,
    modifier: (doc) => doc,
  };
}

// Helper function to set up replication for a collection
function setupReplicationForCollection<T>(
  collection: RxCollection<T>,
  collectionName: string
): RxReplicationState<T, Checkpoint> {
  return replicateGraphQL<T, Checkpoint>({
    collection,
    url: GRAPHQL_ENDPOINT,
    pull: createPullHandler<T>(collectionName),
    push: createPushHandler<T>(collectionName),
    live: true,
    liveInterval: 1000 * 60 * 10, // 10 minutes
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
  });
}

export const setupReplication = async (db: LiveDocsDatabase) => {
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
    setupReplicationForCollection<WorkspaceDocument>(db.workspaces, 'Workspace'),
    setupReplicationForCollection<UserDocument>(db.users, 'User'),
    setupReplicationForCollection<LiveDocDocument>(db.liveDocs, 'LiveDoc'),
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