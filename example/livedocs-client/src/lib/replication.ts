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

const GRAPHQL_ENDPOINT = process.env['NEXT_PUBLIC_GRAPHQL_ENDPOINT'];
const WS_ENDPOINT = process.env['NEXT_PUBLIC_WS_ENDPOINT'];

if (!GRAPHQL_ENDPOINT || !WS_ENDPOINT) {
  throw new Error('GRAPHQL_ENDPOINT and WS_ENDPOINT must be defined in environment variables');
}

const BATCH_SIZE = 50;

type SchemaConfig<T extends LiveDocsDocType> = GraphQLSchemaFromRxSchemaInputSingleCollection & {
  schema: RxJsonSchema<T>;
  deletedField: keyof T & string;
  headerFields: readonly string[];
};

const collectionNames = ['workspace', 'user', 'livedoc'] as const;
type CollectionName = (typeof collectionNames)[number];

type SchemaConfigMap = {
  [K in Capitalize<CollectionName>]: SchemaConfig<
    K extends 'Workspace' ? WorkspaceDocType : K extends 'User' ? UserDocType : LiveDocDocType
  >;
};

const schemaConfigs: SchemaConfigMap = {
  Workspace: {
    schema: workspaceSchema,
    checkpointFields: ['id', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const,
  },
  User: {
    schema: userSchema,
    checkpointFields: ['id', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const,
  },
  Livedoc: {
    schema: liveDocSchema,
    checkpointFields: ['id', 'updatedAt'] as const,
    deletedField: 'isDeleted',
    headerFields: ['Authorization'] as const,
  },
};

interface OperationNames {
  pullQueryName: `pull${Capitalize<CollectionName>}`;
  pushMutationName: `push${Capitalize<CollectionName>}`;
  subscriptionName: `stream${Capitalize<CollectionName>}`;
  baseNameForRxDB: keyof LiveDocsDatabase;
}

function getOperationNames(collectionName: CollectionName): OperationNames {
  const capitalizedName = (collectionName.charAt(0).toUpperCase() +
    collectionName.slice(1)) as Capitalize<CollectionName>;
  return {
    pullQueryName: `pull${capitalizedName}`,
    pushMutationName: `push${capitalizedName}`,
    subscriptionName: `stream${capitalizedName}`,
    baseNameForRxDB: `${collectionName}s` as keyof LiveDocsDatabase,
  };
}

function setupReplicationForCollection<T extends LiveDocsDocType>(
  collection: RxCollection<T>,
  collectionName: CollectionName
): RxGraphQLReplicationState<T, ReplicationCheckpoint> {
  if (!GRAPHQL_ENDPOINT || !WS_ENDPOINT) {
    throw new Error('GRAPHQL_ENDPOINT and WS_ENDPOINT must be defined in environment variables');
  }

  const capitalizedName = (collectionName.charAt(0).toUpperCase() +
    collectionName.slice(1)) as Capitalize<CollectionName>;
  const config = schemaConfigs[capitalizedName];

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
  if (!GRAPHQL_ENDPOINT || !WS_ENDPOINT) {
    throw new Error('GRAPHQL_ENDPOINT and WS_ENDPOINT must be defined in environment variables');
  }

  const replicationStates = collectionNames.map((name) => {
    const { baseNameForRxDB } = getOperationNames(name);
    const collection: RxCollection<LiveDocsDocType> | undefined = db[baseNameForRxDB] as
      | RxCollection<LiveDocsDocType>
      | undefined;

    if (!collection) {
      throw new Error(`Collection ${baseNameForRxDB} not found in database`);
    }

    return setupReplicationForCollection(collection, name);
  });

  try {
    await Promise.all(replicationStates.map((state) => retryWithBackoff(() => state.awaitInitialReplication())));
  } catch (error) {
    logError(
      error instanceof Error ? error : new Error('Unknown error during initial replication'),
      'Initial replication'
    );
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

      retryWithBackoff(
        () =>
          new Promise<void>((resolve) => {
            state.reSync();
            resolve();
          })
      )
        .catch((retryError) => {
          logError(
            retryError instanceof Error ? retryError : new Error('Unknown error during replication retry'),
            `Retry failed for ${collectionName}`
          );
          notifyUser(`Persistent error in ${collectionName} synchronization. Please contact support.`, 'error');
        })
        .catch((finalError) => {
          console.error('Failed to handle retry error:', finalError);
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
