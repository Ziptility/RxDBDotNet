// src\lib\replication.ts
import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
} from 'rxdb/plugins/replication-graphql';
import { RxCollection, GraphQLServerUrl, RxJsonSchema } from 'rxdb';
import { handleAsyncError, handleError } from '@/utils/errorHandling';
import { workspaceSchema, userSchema, liveDocSchema, Workspace, User, LiveDoc } from './schemas';
import { ReplicationCheckpoint, LiveDocsCollections } from '@/types';

const GRAPHQL_ENDPOINT = 'http://localhost:5414/graphql';
const WS_ENDPOINT = 'ws://localhost:5414/graphql';
const JWT_TOKEN =
  'eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJMaXZlRG9jcyIsImV4cCI6MTcyMjk1MTc2NCwiaWF0IjoxNzIyOTUxNzY0fQ.VP3WRdWB6R-lrHfsNM4o2AA95SgE2_PrJQSOoZTyYkg';

const BATCH_SIZE = 50;

const getGraphQLServerUrl = (): GraphQLServerUrl => {
  const url: GraphQLServerUrl = {
    http: GRAPHQL_ENDPOINT,
    ws: WS_ENDPOINT,
  };
  return url;
};

const setupReplicationForCollection = <T extends Workspace | User | LiveDoc>(
  collection: RxCollection<T>,
  schemaName: string,
  schema: RxJsonSchema<T>
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
    handleError(error, `Replication error in ${collection.name}`);
  });

  return replicationState;
};

interface ReplicationStates {
  workspaces: RxGraphQLReplicationState<Workspace, ReplicationCheckpoint>;
  users: RxGraphQLReplicationState<User, ReplicationCheckpoint>;
  livedocs: RxGraphQLReplicationState<LiveDoc, ReplicationCheckpoint>;
}

function isRxGraphQLReplicationState(state: unknown): state is RxGraphQLReplicationState<unknown, unknown> {
  return (
    typeof state === 'object' &&
    state !== null &&
    'cancel' in state &&
    'start' in state &&
    typeof (state as RxGraphQLReplicationState<unknown, unknown>).cancel === 'function' &&
    typeof (state as RxGraphQLReplicationState<unknown, unknown>).start === 'function'
  );
}

export const setupReplication = async (db: LiveDocsDatabase): Promise<ReplicationStates> => {
  const result = await handleAsyncError(async () => {
    const replicationStates: ReplicationStates = {
      workspaces: setupReplicationForCollection<Workspace>(db.workspaces, 'workspace', workspaceSchema),
      users: setupReplicationForCollection<User>(db.users, 'user', userSchema),
      livedocs: setupReplicationForCollection<LiveDoc>(db.livedocs, 'liveDoc', liveDocSchema),
    };

    try {
      await Promise.all(
        Object.values(replicationStates).map((state) => {
          if (isRxGraphQLReplicationState(state)) {
            return state.awaitInitialReplication();
          }
          throw new Error('Invalid replication state');
        })
      );
      console.log('Initial replication completed successfully');
    } catch (error) {
      handleError(error, 'Initial replication');
      // Even if initial replication fails, we still return the replication states
    }

    return replicationStates;
  }, 'Setting up replication');

  if (!result) {
    throw new Error('Failed to set up replication');
  }

  return result;
};

export const subscribeToCollectionChanges = <K extends keyof LiveDocsCollections>(
  collection: RxCollection<LiveDocsCollections[K]>,
  onChange: (docs: LiveDocsCollections[K][]) => void
): void => {
  collection.find().$.subscribe((docs) => {
    onChange(docs);
  });
};

export const cancelReplication = async (replicationStates: ReplicationStates): Promise<void> => {
  await handleAsyncError(async () => {
    await Promise.all(
      Object.values(replicationStates).map((state) => {
        if (isRxGraphQLReplicationState(state)) {
          return state.cancel();
        }
        return Promise.resolve();
      })
    );
    console.log('Replication cancelled successfully');
  }, 'Cancelling replication');
};

export const restartReplication = async (replicationStates: ReplicationStates): Promise<void> => {
  await handleAsyncError(async () => {
    for (const [collectionName, state] of Object.entries(replicationStates)) {
      if (isRxGraphQLReplicationState(state)) {
        try {
          await state.cancel();
          console.log(`Cancelled replication for ${collectionName}`);
          await state.start();
          console.log(`Restarted replication for ${collectionName}`);
        } catch (error) {
          handleError(error, `Restarting replication for ${collectionName}`);
        }
      } else {
        console.warn(`Invalid replication state for ${collectionName}`);
      }
    }
    console.log('Replication restarted successfully for all collections');
  }, 'Restarting replication');
};

export const checkReplicationStatus = (
  replicationStates: ReplicationStates
): { [K in keyof ReplicationStates]: boolean } => {
  return Object.entries(replicationStates).reduce<{ [K in keyof ReplicationStates]: boolean }>(
    (acc, [key, state]) => {
      acc[key as keyof ReplicationStates] = isRxGraphQLReplicationState(state) && !state.isStopped();
      return acc;
    },
    { workspaces: false, users: false, livedocs: false }
  );
};
