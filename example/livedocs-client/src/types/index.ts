import { Workspace, User, LiveDoc } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspaces: RxCollection<Workspace>;
  users: RxCollection<User>;
  livedocs: RxCollection<LiveDoc>;
}

export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

export interface ReplicationCheckpoint {
  id: string | null;
  updatedAt: string | null;
}

export interface LiveDocsReplicationState {
  workspaces: RxGraphQLReplicationState<Workspace, ReplicationCheckpoint>;
  users: RxGraphQLReplicationState<User, ReplicationCheckpoint>;
  livedocs: RxGraphQLReplicationState<LiveDoc, ReplicationCheckpoint>;
}
