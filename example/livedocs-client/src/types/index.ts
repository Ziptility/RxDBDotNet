import { Workspace, User, LiveDoc } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspaces: RxCollection<Workspace>;
  users: RxCollection<User>;
  livedocs: RxCollection<LiveDoc>;
}

export interface LiveDocsDatabase extends RxDatabase<LiveDocsCollections> {
  replicationStates?: LiveDocsReplicationState;
}

export interface LiveDocsReplicationState {
  workspaces: RxGraphQLReplicationState<Workspace, unknown>;
  users: RxGraphQLReplicationState<User, unknown>;
  livedocs: RxGraphQLReplicationState<LiveDoc, unknown>;
}

export interface ReplicationCheckpoint {
  id: string | null;
  updatedAt: string | null;
}
