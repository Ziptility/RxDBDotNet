import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL
} from 'rxdb/plugins/replication-graphql';
import { lastValueFrom } from 'rxjs';
import { RxCollection, RxDocument } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { WorkspaceDocType, UserDocType, LiveDocDocType, workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { RxReplicationState } from 'rxdb/plugins/replication';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT || 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT || 'ws://localhost:5414/graphql';

// Checkpoint type used for replication
type Checkpoint = {
  id: string;
  updatedAt: string;
};

// Generic type for document types
type DocType = WorkspaceDocType | UserDocType | LiveDocDocType;

// Helper function to get the schema for a collection
function getSchemaForCollection(collectionName: string) {
  switch (collectionName) {
    case 'Workspace':
      return workspaceSchema;
    case 'User':
      return userSchema;
    case 'LiveDoc':
      return liveDocSchema;
    default:
      throw new Error(`Unknown collection name: ${collectionName}`);
  }
}

// Helper function to set up replication for a collection
function setupReplicationForCollection<T extends DocType>(
  collection: RxCollection<T>,
  collectionName: string
): RxReplicationState<RxDocument<T>, Checkpoint> {
  const schema = getSchemaForCollection(collectionName);
  const batchSize = 50;

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(
    collectionName.toLowerCase(),
    {
      schema,
      checkpointFields: ['id', 'updatedAt'],
      deletedField: 'isDeleted'
    },
    batchSize
  );

  const pushQueryBuilder = pushQueryBuilderFromRxSchema(
    collectionName.toLowerCase(),
    {
      schema,
      checkpointFields: ['id', 'updatedAt'],
      deletedField: 'isDeleted'
    }
  );

  const pullStreamBuilder = pullStreamBuilderFromRxSchema(
    collectionName.toLowerCase(),
    {
      schema,
      checkpointFields: ['id', 'updatedAt'],
      deletedField: 'isDeleted'
    }
  );

  return replicateGraphQL<T, Checkpoint>({
    collection,
    url: {
      http: GRAPHQL_ENDPOINT,
      ws: WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder,
      streamQueryBuilder: pullStreamBuilder,
      batchSize
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize: 5
    },
    live: true,
    liveInterval: 1000 * 60 * 10, // 10 minutes
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
  });
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<RxReplicationState<RxDocument<DocType>, Checkpoint>[]> => {
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
export const cancelAllReplications = (replicationStates: RxReplicationState<RxDocument<DocType>, Checkpoint>[]) => {
  replicationStates.forEach(state => state.cancel());
};

// Function to resume all replications
export const resumeAllReplications = (replicationStates: RxReplicationState<RxDocument<DocType>, Checkpoint>[]) => {
  replicationStates.forEach(state => state.reSync());
};