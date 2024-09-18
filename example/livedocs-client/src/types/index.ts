// src\types\index.ts
import { Workspace, User, LiveDoc } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspace: RxCollection<Workspace>;
  user: RxCollection<User>;
  livedoc: RxCollection<LiveDoc>;
}

export interface LiveDocsDatabase extends RxDatabase<LiveDocsCollections> {
  replicationStates?: LiveDocsReplicationState;
}

export interface LiveDocsReplicationState {
  workspaces: RxGraphQLReplicationState<Workspace, ReplicationCheckpoint>;
  users: RxGraphQLReplicationState<User, ReplicationCheckpoint>;
  livedocs: RxGraphQLReplicationState<LiveDoc, ReplicationCheckpoint>;
}

export interface ReplicationCheckpoint {
  lastDocumentId: string | null;
  updatedAt: string | null;
}
