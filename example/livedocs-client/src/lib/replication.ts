import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL
} from 'rxdb/plugins/replication-graphql';
import { RxCollection } from 'rxdb';
import { lastValueFrom } from 'rxjs';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDocType, ReplicationCheckpoint } from '@/types';
import { RxReplicationState } from 'rxdb/plugins/replication';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT || 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT || 'ws://localhost:5414/graphql';

const BATCH_SIZE = 50;

interface SchemaConfig {
  schema: any;
  checkpointFields: string[];
  deletedField: string;
}

const schemaConfigs: Record<string, SchemaConfig> = {
  Workspace: {
    schema: workspaceSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted'
  },
  User: {
    schema: userSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted'
  },
  LiveDoc: {
    schema: liveDocSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted'
  }
};

function setupReplicationForCollection<T extends LiveDocsDocType>(
  collection: RxCollection<T>,
  collectionName: string
): RxReplicationState<T, ReplicationCheckpoint> {
  const config = schemaConfigs[collectionName];
  if (!config) {
    throw new Error(`Unknown collection name: ${collectionName}`);
  }

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(
    collectionName.toLowerCase(),
    config
  );

  const pushQueryBuilder = pushQueryBuilderFromRxSchema(
    collectionName.toLowerCase(),
    config
  );

  const pullStreamBuilder = pullStreamBuilderFromRxSchema(
    collectionName.toLowerCase(),
    config
  );

  return replicateGraphQL<T, ReplicationCheckpoint>({
    collection: collection as any, // Type assertion to avoid the mismatch
    url: {
      http: GRAPHQL_ENDPOINT,
      ws: WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder,
      streamQueryBuilder: pullStreamBuilder,
      batchSize: BATCH_SIZE
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize: 5
    },
    live: true,
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
    replicationIdentifier: `livedocs-${collectionName.toLowerCase()}-replication`,
  }) as RxReplicationState<T, ReplicationCheckpoint>; // Type assertion to match the return type
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]> => {
  const collectionNames = ['Workspace', 'User', 'LiveDoc'];
  const replicationStates = collectionNames.map(name => 
    setupReplicationForCollection(db[name.toLowerCase() + 's' as keyof LiveDocsDatabase] as RxCollection<LiveDocsDocType>, name)
  );

  try {
    await Promise.all(replicationStates.map((state) => 
      retryWithBackoff(() => lastValueFrom(state.awaitInitialReplication()))
    ));
    console.log('Initial replication completed successfully');
  } catch (error) {
    logError(error as Error, 'Initial replication');
    notifyUser('Failed to complete initial data sync. Some data may be outdated.');
  }

  replicationStates.forEach((state, index) => {
    const collectionName = collectionNames[index];
    state.error$.subscribe((error) => {
      logError(new ReplicationError(`Replication error in ${collectionName}`, error), 'Ongoing replication');

      if (error.parameters.direction === 'pull') {
        notifyUser(`Error fetching latest ${collectionName} data. Some information may be outdated.`, 'warning');
      } else if (error.parameters.direction === 'push') {
        notifyUser(`Error saving ${collectionName} changes. Please try again later.`, 'error');
      }

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

export const cancelAllReplications = (replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]) => {
  replicationStates.forEach(state => state.cancel());
};

export const resumeAllReplications = (replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]) => {
  replicationStates.forEach(state => state.reSync());
};