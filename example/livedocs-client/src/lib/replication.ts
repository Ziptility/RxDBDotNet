// src\lib\replication.ts
import { type RxCollection, type RxJsonSchema, RxError } from 'rxdb';
import {
  pullQueryBuilderFromRxSchema,
  pushQueryBuilderFromRxSchema,
  pullStreamBuilderFromRxSchema,
  replicateGraphQL,
  RxGraphQLReplicationState,
  type GraphQLSchemaFromRxSchemaInputSingleCollection,
} from 'rxdb/plugins/replication-graphql';
import type { LiveDocsDatabase } from '@/lib/database';
import type { LiveDocsReplicationStates, LiveDocsReplicationOptions, ReplicationCheckpoint } from '@/types';
import { workspaceSchema, userSchema, liveDocSchema, type Workspace, type User, type LiveDoc } from './schemas';

export const GRAPHQL_ENDPOINT = 'http://localhost:5414/graphql';
export const WS_ENDPOINT = 'ws://localhost:5414/graphql';

const setupReplicationForCollection = <T extends Workspace | User | LiveDoc>(
  db: LiveDocsDatabase,
  collectionName: keyof LiveDocsDatabase,
  schema: RxJsonSchema<T>
): RxGraphQLReplicationState<T, ReplicationCheckpoint> => {
  const collection = db[collectionName] as RxCollection<T>;

  const schemaInput: GraphQLSchemaFromRxSchemaInputSingleCollection = {
    schema,
    checkpointFields: ['lastDocumentId', 'updatedAt'],
    deletedField: 'isDeleted',
    headerFields: ['Authorization'],
  };

  const pullQueryBuilder = pullQueryBuilderFromRxSchema(collectionName, schemaInput);
  const pushQueryBuilder = pushQueryBuilderFromRxSchema(collectionName, schemaInput);
  const pullStreamBuilder = pullStreamBuilderFromRxSchema(collectionName, schemaInput);

  const replicationOptions: LiveDocsReplicationOptions<T> = {
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
  };

  return replicateGraphQL(replicationOptions);
};

export const setupReplication = async (db: LiveDocsDatabase): Promise<LiveDocsReplicationStates> => {
  const replicationStates: LiveDocsReplicationStates = {
    workspaces: setupReplicationForCollection<Workspace>(db, 'workspace', workspaceSchema),
    users: setupReplicationForCollection<User>(db, 'user', userSchema),
    livedocs: setupReplicationForCollection<LiveDoc>(db, 'livedoc', liveDocSchema),
  };

  // Handle replication errors
  await Promise.all(
    Object.entries(replicationStates).map(([name, state]) => {
      (state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>).error$.subscribe((error: RxError) => {
        console.error(`Replication error in ${name}:`, error);
      });
    })
  );

  return replicationStates;
};

export const restartReplication = async (replicationStates: LiveDocsReplicationStates): Promise<void> => {
  await Promise.all(
    Object.entries(replicationStates).map(async ([, state]) => {
      await (state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>).cancel();
      await (state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>).start();
    })
  );
};

export const cancelReplication = async (replicationStates: LiveDocsReplicationStates): Promise<void> => {
  await Promise.all(
    Object.values(replicationStates).map((state) =>
      (state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>).cancel()
    )
  );
};
