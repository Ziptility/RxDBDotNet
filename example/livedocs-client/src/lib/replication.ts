import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
  GraphQLSchemaFromRxSchemaInputSingleCollection,
} from 'rxdb/plugins/replication-graphql';
import { RxCollection, RxJsonSchema, GraphQLServerUrl, SyncOptionsGraphQL } from 'rxdb';
import { logError, notifyUser, retryWithBackoff, ReplicationError } from './errorHandling';
import { workspaceSchema, userSchema, liveDocSchema, WorkspaceDocType, UserDocType, LiveDocDocType } from './schemas';
import { LiveDocsDocType, ReplicationCheckpoint } from '@/types';

const GRAPHQL_ENDPOINT = process.env['NEXT_PUBLIC_GRAPHQL_ENDPOINT'];
const WS_ENDPOINT = process.env['NEXT_PUBLIC_WS_ENDPOINT'];
const JWT_TOKEN = process.env['NEXT_PUBLIC_JWT_TOKEN'];

if (!JWT_TOKEN) {
  throw new Error('NEXT_PUBLIC_JWT_TOKEN must be defined in environment variables');
}

const BATCH_SIZE = 50;

const getGraphQLServerUrl = (): GraphQLServerUrl => {
  const url: GraphQLServerUrl = {};
  if (GRAPHQL_ENDPOINT) {
    url.http = GRAPHQL_ENDPOINT;
  }
  if (WS_ENDPOINT) {
    url.ws = WS_ENDPOINT;
  }
  if (!url.http && !url.ws) {
    throw new Error('At least one of GRAPHQL_ENDPOINT or WS_ENDPOINT must be defined');
  }
  return url;
};

interface CollectionConfig {
  name: keyof LiveDocsDatabase;
  schema: RxJsonSchema<WorkspaceDocType> | RxJsonSchema<UserDocType> | RxJsonSchema<LiveDocDocType>;
  graphqlType: string;
  queryName: string;
}

const collections: readonly CollectionConfig[] = [
  { name: 'workspaces', schema: workspaceSchema, graphqlType: 'Workspace', queryName: 'workspace' },
  { name: 'users', schema: userSchema, graphqlType: 'User', queryName: 'user' },
  { name: 'livedocs', schema: liveDocSchema, graphqlType: 'LiveDoc', queryName: 'liveDoc' },
] as const;

function setupReplicationForCollection(
  collection: RxCollection<LiveDocsDocType>,
  config: CollectionConfig
): RxGraphQLReplicationState<LiveDocsDocType, ReplicationCheckpoint> {
  const schemaConfig: GraphQLSchemaFromRxSchemaInputSingleCollection = {
    schema: collection.schema.jsonSchema,
    checkpointFields: ['lastDocumentId', 'updatedAt'] as const,
    deletedField: 'isDeleted',
  };

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(config.queryName, schemaConfig);
  const pushQueryBuilder = pushQueryBuilderFromRxSchema(config.queryName, schemaConfig);
  const pullStreamBuilder = pullStreamBuilderFromRxSchema(config.queryName, schemaConfig);

  const replicationOptions: SyncOptionsGraphQL<LiveDocsDocType, ReplicationCheckpoint> = {
    collection,
    url: getGraphQLServerUrl(),
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
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
    replicationIdentifier: `livedocs-${collection.name}-replication`,
    headers: {
      Authorization: `Bearer ${JWT_TOKEN}`,
    },
  };

  const replicationState = replicateGraphQL(replicationOptions);

  replicationState.error$.subscribe((error) => {
    logError(new ReplicationError(`Replication error in ${config.graphqlType}`, error), 'Ongoing replication');
    notifyUser(`Error in ${config.graphqlType} synchronization. Please try again later.`, 'error');

    retryWithBackoff(
      () =>
        new Promise<void>((resolve) => {
          replicationState.reSync();
          resolve();
        })
    ).catch((retryError) => {
      logError(
        retryError instanceof Error ? retryError : new Error('Unknown error during replication retry'),
        `Retry failed for ${config.graphqlType}`
      );
      notifyUser(`Persistent error in ${config.graphqlType} synchronization. Please contact support.`, 'error');
    });
  });

  replicationState.received$.subscribe((doc) => {
    console.log(`Received document for ${config.graphqlType}:`, doc);
  });

  replicationState.sent$.subscribe((doc) => {
    console.log(`Sent document for ${config.graphqlType}:`, doc);
  });

  return replicationState;
}

export const setupReplication = async (
  db: LiveDocsDatabase
): Promise<RxGraphQLReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]> => {
  const replicationStates = collections.map((config) => {
    const collection: RxCollection<LiveDocsDocType> | undefined = db[config.name] as
      | RxCollection<LiveDocsDocType>
      | undefined;

    if (!collection) {
      throw new Error(`Collection ${config.name} not found in database`);
    }
    return setupReplicationForCollection(collection, config);
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

  return replicationStates;
};

export const cancelAllReplications = (
  replicationStates: RxGraphQLReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]
): void => {
  replicationStates.forEach((state) => {
    void state.cancel();
  });
};

export const resumeAllReplications = (
  replicationStates: RxGraphQLReplicationState<LiveDocsDocType, ReplicationCheckpoint>[]
): void => {
  replicationStates.forEach((state) => {
    state.reSync();
  });
};
