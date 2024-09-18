// src\types\index.ts
import { Workspace, User, LiveDoc } from '@/lib/schemas';
import { RxCollection, RxDatabase, SyncOptionsGraphQL } from 'rxdb';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspace: RxCollection<Workspace>;
  user: RxCollection<User>;
  livedoc: RxCollection<LiveDoc>;
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
