// src/lib/replication.ts

import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
  RxGraphQLReplicationQueryBuilder,
  RxGraphQLReplicationPullStreamBuilder
} from 'rxdb/plugins/replication-graphql';
import { RxCollection, RxDocument } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDocType, ReplicationCheckpoint, RxReplicationState, WorkspaceDocType, UserDocType, LiveDocDocType } from '@/types';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT ?? 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT ?? 'ws://localhost:5414/graphql';

const BATCH_SIZE = 50;

interface SchemaConfig<T extends LiveDocsDocType> {
  schema: typeof workspaceSchema | typeof userSchema | typeof liveDocSchema;
  checkpointFields: readonly (keyof T)[];
  deletedField: keyof T & string;
  headerFields: readonly string[];
}

const collectionNames = ['Workspace', 'User', 'LiveDoc'] as const;
type CollectionName = typeof collectionNames[number];

type SchemaConfigMap = {
  [K in CollectionName]: SchemaConfig
    K extends 'Workspace' ? WorkspaceDocType :
    K extends 'User' ? UserDocType :
    K extends 'LiveDoc' ? LiveDocDocType :
    never
  >
};

const schemaConfigs: SchemaConfigMap = {
  Workspace: {
    schema: workspaceSchema,
    checkpointFields: ['lastDocumentId', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const
  },
  User: {
    schema: userSchema,
    checkpointFields: ['lastDocumentId', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const
  },
  LiveDoc: {
    schema: liveDocSchema,
    checkpointFields: ['lastDocumentId', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const
  }
};

function getOperationNames(collectionName: CollectionName): {
  pullQueryName: `pull${CollectionName}`;
  pushMutationName: `push${CollectionName}`;
  subscriptionName: `stream${CollectionName}`;
  baseNameForRxDB: keyof LiveDocsDatabase;
} {
  const baseName = collectionName.charAt(0).toLowerCase() + collectionName.slice(1);
  return {
    pullQueryName: `pull${collectionName}` as const,
    pushMutationName: `push${collectionName}` as const,
    subscriptionName: `stream${collectionName}` as const,
    baseNameForRxDB: `${baseName}s` as keyof LiveDocsDatabase
  };
}

function setupReplicationForCollection<T extends LiveDocsDocType>(
  collection: RxCollection<T>,
  collectionName: CollectionName
): RxGraphQLReplicationState<T, ReplicationCheckpoint> {
  const config = schemaConfigs[collectionName];
  if (!config) {
    throw new Error(`Unknown collection name: ${collectionName}`);
  }

  const { pullQueryName, pushMutationName, subscriptionName, baseNameForRxDB } = getOperationNames(collectionName);

  const pullQueryBuilder: RxGraphQLReplicationQueryBuilder<T> = pullQueryBuilderFromRxSchema(
    pullQueryName,
    config as SchemaConfig<T>
  );

  const pushQueryBuilder: RxGraphQLReplicationQueryBuilder<T> = pushQueryBuilderFromRxSchema(
    pushMutationName,
    config as SchemaConfig<T>
  );

  const pullStreamBuilder: RxGraphQLReplicationPullStreamBuilder<T> = pullStreamBuilderFromRxSchema(
    subscriptionName,
    config as SchemaConfig<T>
  );

  return replicateGraphQL<T, ReplicationCheckpoint>({
    collection,
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
    deletedField: config.deletedField,
    retryTime: 1000 * 30, // 30 seconds
    replicationIdentifier: `livedocs-${baseNameForRxDB}-replication`,
  });
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]> => {
  const replicationStates = collectionNames.map(name => {
    const { baseNameForRxDB } = getOperationNames(name);
    const collection = db[baseNameForRxDB];
    if (!collection) {
      throw new Error(`Collection ${baseNameForRxDB} not found in database`);
    }
    return setupReplicationForCollection(collection as RxCollection<LiveDocsDocType>, name);
  });

  try {
    await Promise.all(replicationStates.map(async (state) => {
      await retryWithBackoff(async () => {
        await state.awaitInitialReplication();
      });
    }));
    console.log('Initial replication completed successfully');
  } catch (error) {
    logError(error instanceof Error ? error : new Error(String(error)), 'Initial replication');
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

      retryWithBackoff(async () => {
        console.log(`Retrying ${error.parameters.direction} replication for ${collectionName}...`);
        await state.reSync();
      }).catch((retryError) => {
        logError(retryError instanceof Error ? retryError : new Error(String(retryError)), `Retry failed for ${collectionName}`);
        notifyUser(`Persistent error in ${collectionName} synchronization. Please contact support.`, 'error');
      });
    });
  });

  return replicationStates;
};

export const cancelAllReplications = (replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]): void => {
  replicationStates.forEach(state => state.cancel());
};

export const resumeAllReplications = (replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]): void => {
  replicationStates.forEach(state => state.reSync());
};
