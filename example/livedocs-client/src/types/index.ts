// src\types\index.ts
import type { LiveDoc, User, Workspace } from '@/generated/graphql';
import type { RxCollection, RxDatabase, SyncOptionsGraphQL, RxJsonSchema } from 'rxdb';
import type { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspace: RxCollection<Workspace>;
  user: RxCollection<User>;
  livedoc: RxCollection<LiveDoc>;
}

export interface LiveDocsCollectionConfig {
  workspace: {
    schema: RxJsonSchema<Workspace>;
  };
  user: {
    schema: RxJsonSchema<User>;
  };
  livedoc: {
    schema: RxJsonSchema<LiveDoc>;
  };
}

export interface LiveDocsDatabase extends RxDatabase<LiveDocsCollections> {
  replicationStates?: LiveDocsReplicationStates;
}

export type LiveDocsReplicationState<T> = RxGraphQLReplicationState<T, ReplicationCheckpoint>;

export interface LiveDocsReplicationStates {
  workspaces: LiveDocsReplicationState<Workspace>;
  users: LiveDocsReplicationState<User>;
  livedocs: LiveDocsReplicationState<LiveDoc>;
}

export interface ReplicationCheckpoint {
  lastDocumentId: string | null;
  updatedAt: string | null;
}

export type LiveDocsReplicationOptions<T> = SyncOptionsGraphQL<T, ReplicationCheckpoint>;

export type LiveDocTypes = Workspace | User | LiveDoc;
