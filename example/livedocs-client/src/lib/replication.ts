// src\lib\replication.ts
import { LiveDocsDatabase } from '@/lib/database';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
  GraphQLSchemaFromRxSchemaInputSingleCollection,
} from 'rxdb/plugins/replication-graphql';
import { workspaceSchema, userSchema, liveDocSchema, Workspace, User, LiveDoc } from './schemas';
import { RxCollection, RxJsonSchema, RxError } from 'rxdb';
import { LiveDocsReplicationState } from '@/types';

const GRAPHQL_ENDPOINT = 'http://localhost:5414/graphql';
const WS_ENDPOINT = 'ws://localhost:5414/graphql';

const setupReplicationForCollection = <T extends Workspace | User | LiveDoc>(
  db: LiveDocsDatabase,
  collectionName: keyof LiveDocsDatabase,
  schema: RxJsonSchema<T>
): RxGraphQLReplicationState<T, unknown> => {
  const collection = db[collectionName] as RxCollection<T>;

  const schemaInput: GraphQLSchemaFromRxSchemaInputSingleCollection = {
    schema,
    checkpointFields: ['id', 'updatedAt'],
    deletedField: 'isDeleted',
  };

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(collectionName, schemaInput);
  const pushQueryBuilder = pushQueryBuilderFromRxSchema(collectionName, schemaInput);
  const pullStreamBuilder = pullStreamBuilderFromRxSchema(collectionName, schemaInput);

  return replicateGraphQL<T, unknown>({
    collection,
    url: {
      http: GRAPHQL_ENDPOINT,
      ws: WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder,
      streamQueryBuilder: pullStreamBuilder,
      batchSize: 100,
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize: 5,
    },
    live: true,
    deletedField: 'isDeleted',
    retryTime: 1000 * 30, // 30 seconds
    replicationIdentifier: `livedocs-${collectionName}-replication`,
  });
};

export const setupReplication = async (db: LiveDocsDatabase): Promise<LiveDocsReplicationState> => {
  const replicationStates: LiveDocsReplicationState = {
    workspaces: setupReplicationForCollection(db, 'workspaces', workspaceSchema),
    users: setupReplicationForCollection(db, 'users', userSchema),
    livedocs: setupReplicationForCollection(db, 'livedocs', liveDocSchema),
  };

  // Handle replication errors
  await Promise.all(
    Object.entries(replicationStates).map(([name, state]) => {
      (state as RxGraphQLReplicationState<unknown, unknown>).error$.subscribe((error: RxError) => {
        console.error(`Replication error in ${name}:`, error);
      });
    })
  );

  return replicationStates;
};

export const restartReplication = async (replicationStates: LiveDocsReplicationState): Promise<void> => {
  await Promise.all(
    Object.entries(replicationStates).map(async ([, state]) => {
      await (state as RxGraphQLReplicationState<unknown, unknown>).cancel();
      await (state as RxGraphQLReplicationState<unknown, unknown>).start();
    })
  );
};

export const cancelReplication = async (replicationStates: LiveDocsReplicationState): Promise<void> => {
  await Promise.all(
    Object.values(replicationStates).map((state) => (state as RxGraphQLReplicationState<unknown, unknown>).cancel())
  );
};
