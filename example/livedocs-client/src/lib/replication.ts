import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
} from 'rxdb/plugins/replication-graphql';
import { RxCollection, GraphQLServerUrl } from 'rxdb';
import { logError, notifyUser } from './errorHandling';
import { workspaceSchema, userSchema, liveDocSchema, WorkspaceDocType, UserDocType, LiveDocDocType } from './schemas';
import { ReplicationCheckpoint, LiveDocsCollections } from '@/types';

const GRAPHQL_ENDPOINT = 'http://localhost:5414/graphql';
const WS_ENDPOINT = 'ws://localhost:5414/graphql';
const JWT_TOKEN =
  'eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJMaXZlRG9jcyIsImV4cCI6MTcyMjk1MTc2NCwiaWF0IjoxNzIyOTUxNzY0fQ.VP3WRdWB6R-lrHfsNM4o2AA95SgE2_PrJQSOoZTyYkg';

const BATCH_SIZE = 50;

const getGraphQLServerUrl = (): GraphQLServerUrl => {
  const url: GraphQLServerUrl = {};
  url.http = GRAPHQL_ENDPOINT;
  url.ws = WS_ENDPOINT;
  return url;
};

const setupReplicationForCollection = <T extends WorkspaceDocType | UserDocType | LiveDocDocType>(
  collection: RxCollection<T>,
  schemaName: string,
  schema: typeof workspaceSchema | typeof userSchema | typeof liveDocSchema
): RxGraphQLReplicationState<T, ReplicationCheckpoint> => {
  const pullQueryBuilder = pullQueryBuilderFromRxSchema(schemaName, {
    schema,
    checkpointFields: ['lastDocumentId', 'updatedAt'],
    deletedField: 'isDeleted',
  });
  const pushQueryBuilder = pushQueryBuilderFromRxSchema(schemaName, {
    schema,
    checkpointFields: ['lastDocumentId', 'updatedAt'],
    deletedField: 'isDeleted',
  });
  const pullStreamBuilder = pullStreamBuilderFromRxSchema(schemaName, {
    schema,
    checkpointFields: ['lastDocumentId', 'updatedAt'],
    deletedField: 'isDeleted',
  });

  const replicationState = replicateGraphQL<T, ReplicationCheckpoint>({
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
  });

  replicationState.error$.subscribe((error) => {
    logError(error, `Replication error in ${collection.name}`);
    notifyUser(`Error in ${collection.name} synchronization. Please try again later.`, 'error');
  });

  return replicationState;
};

interface ReplicationStates {
  workspaces: RxGraphQLReplicationState<WorkspaceDocType, ReplicationCheckpoint>;
  users: RxGraphQLReplicationState<UserDocType, ReplicationCheckpoint>;
  livedocs: RxGraphQLReplicationState<LiveDocDocType, ReplicationCheckpoint>;
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<ReplicationStates> => {
  const replicationStates: ReplicationStates = {
    workspaces: setupReplicationForCollection<WorkspaceDocType>(db.workspaces, 'workspace', workspaceSchema),
    users: setupReplicationForCollection<UserDocType>(db.users, 'user', userSchema),
    livedocs: setupReplicationForCollection<LiveDocDocType>(db.livedocs, 'liveDoc', liveDocSchema),
  };

  try {
    await Promise.all(
      Object.values(replicationStates).map((state) =>
        (state as RxGraphQLReplicationState<unknown, unknown>).awaitInitialReplication()
      )
    );
    console.log('Initial replication completed successfully');
  } catch (error) {
    logError(
      error instanceof Error ? error : new Error('Unknown error during initial replication'),
      'Initial replication'
    );
    notifyUser('Failed to complete initial data sync. Some data may be outdated.');
  }

  return replicationStates;
};

export const subscribeToCollectionChanges = <K extends keyof LiveDocsCollections>(
  collection: RxCollection<LiveDocsCollections[K]>,
  onChange: (docs: LiveDocsCollections[K][]) => void
): void => {
  collection.find().$.subscribe((docs) => {
    onChange(docs);
  });
};
