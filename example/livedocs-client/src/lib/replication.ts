import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
  GraphQLSchemaFromRxSchemaInputSingleCollection,
} from 'rxdb/plugins/replication-graphql';
import { RxCollection, RxJsonSchema } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { workspaceSchema, userSchema, liveDocSchema, WorkspaceDocType, UserDocType, LiveDocDocType } from './schemas';
import { LiveDocsDocType, ReplicationCheckpoint, RxReplicationState } from '@/types';

const GRAPHQL_ENDPOINT = process.env['NEXT_PUBLIC_GRAPHQL_ENDPOINT'] ?? 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env['NEXT_PUBLIC_WS_ENDPOINT'] ?? 'ws://localhost:5414/graphql';

const BATCH_SIZE = 50;

type SchemaConfig<T extends LiveDocsDocType> = GraphQLSchemaFromRxSchemaInputSingleCollection & {
  schema: RxJsonSchema<T>;
  deletedField: keyof T & string;
  headerFields: string[];
};

const collectionNames = ['Workspace', 'User', 'LiveDoc'] as const;
type CollectionName = (typeof collectionNames)[number];

type SchemaConfigMap = {
  [K in CollectionName]: SchemaConfig<
    K extends 'Workspace' ? WorkspaceDocType : K extends 'User' ? UserDocType : LiveDocDocType
  >;
};

const schemaConfigs: SchemaConfigMap = {
  Workspace: {
    schema: workspaceSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted',
    headerFields: ['Authorization'],
  },
  User: {
    schema: userSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted',
    headerFields: ['Authorization'],
  },
  LiveDoc: {
    schema: liveDocSchema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted',
    headerFields: ['Authorization'],
  },
};

function getOperationNames(collectionName: CollectionName): {
  pullQueryName: `pull${CollectionName}`;
  pushMutationName: `push${CollectionName}`;
  subscriptionName: `stream${CollectionName}`;
  baseNameForRxDB: keyof LiveDocsDatabase;
} {
  const baseName = collectionName.charAt(0).toLowerCase() + collectionName.slice(1);
  return {
    pullQueryName: `pull${collectionName}`,
    pushMutationName: `push${collectionName}`,
    subscriptionName: `stream${collectionName}`,
    baseNameForRxDB: `${baseName}s` as keyof LiveDocsDatabase,
  };
}

function setupReplicationForCollection<T extends LiveDocsDocType>(
  collection: RxCollection<T>,
  collectionName: CollectionName
): RxGraphQLReplicationState<T, ReplicationCheckpoint> {
  const config = schemaConfigs[collectionName];

  const { pullQueryName, pushMutationName, subscriptionName, baseNameForRxDB } = getOperationNames(collectionName);

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(pullQueryName, config);
  const pushQueryBuilder = pushQueryBuilderFromRxSchema(pushMutationName, config);
  const pullStreamBuilder = pullStreamBuilderFromRxSchema(subscriptionName, config);

  return replicateGraphQL<T, ReplicationCheckpoint>({
    collection,
    url: {
      http: GRAPHQL_ENDPOINT,
      ws: WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder,
      streamQueryBuilder: pullStreamBuilder,
      batchSize: BATCH_SIZE,
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize: 5,
    },
    live: true,
    deletedField: config.deletedField,
    retryTime: 1000 * 30, // 30 seconds
    replicationIdentifier: `livedocs-${baseNameForRxDB}-replication`,
  });
}

export const setupReplication = async (
  db: LiveDocsDatabase
): Promise<RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]> => {
  const replicationStates = collectionNames.map((name) => {
    const { baseNameForRxDB } = getOperationNames(name);
    const collection = db[baseNameForRxDB] as RxCollection<LiveDocsDocType>;
    if (!collection) {
      throw new Error(`Collection ${baseNameForRxDB} not found in database`);
    }
    return setupReplicationForCollection(collection, name);
  });

  try {
    await Promise.all(replicationStates.map((state) => retryWithBackoff(() => state.awaitInitialReplication())));
  } catch (error) {
    logError(error instanceof Error ? error : new Error(String(error)), 'Initial replication');
    notifyUser('Failed to complete initial data sync. Some data may be outdated.');
  }

  replicationStates.forEach((state, index) => {
    const collectionName = collectionNames[index];
    state.error$.subscribe((error) => {
      logError(new ReplicationError(`Replication error in ${String(collectionName)}`, error), 'Ongoing replication');

      if (error.parameters.direction === 'pull') {
        notifyUser(
          `Error fetching latest ${String(collectionName)} data. Some information may be outdated.`,
          'warning'
        );
      } else if (error.parameters.direction === 'push') {
        notifyUser(`Error saving ${String(collectionName)} changes. Please try again later.`, 'error');
      }

      void retryWithBackoff(() => {
        state.reSync();
        return Promise.resolve();
      }).catch((retryError) => {
        logError(
          retryError instanceof Error ? retryError : new Error(String(retryError)),
          `Retry failed for ${String(collectionName)}`
        );
        notifyUser(`Persistent error in ${String(collectionName)} synchronization. Please contact support.`, 'error');
      });
    });
  });

  return replicationStates;
};

export const cancelAllReplications = (
  replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]
): void => {
  replicationStates.forEach((state) => {
    void state.cancel();
  });
};

export const resumeAllReplications = (
  replicationStates: RxReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]
): void => {
  replicationStates.forEach((state) => {
    state.reSync();
  });
};
