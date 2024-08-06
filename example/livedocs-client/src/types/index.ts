import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

export interface LiveDocsCollections {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  livedocs: RxCollection<LiveDocDocType>;
}

export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

export interface ReplicationCheckpoint {
  id: string | null;
  updatedAt: string | null;
}

export interface LiveDocsReplicationState {
  workspaces: RxGraphQLReplicationState<WorkspaceDocType, ReplicationCheckpoint>;
  users: RxGraphQLReplicationState<UserDocType, ReplicationCheckpoint>;
  livedocs: RxGraphQLReplicationState<LiveDocDocType, ReplicationCheckpoint>;
}
