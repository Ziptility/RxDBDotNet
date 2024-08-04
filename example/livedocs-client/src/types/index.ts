import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxReplicationState as RxDBReplicationState } from 'rxdb/plugins/replication';
import { Observable } from 'rxjs';

export interface Workspace extends WorkspaceDocType {}
export interface User extends UserDocType {}
export interface LiveDoc extends LiveDocDocType {}

export type LiveDocsCollections = {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  liveDocs: RxCollection<LiveDocDocType>;
};

export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

export interface RxReplicationState<T, C> extends RxDBReplicationState<T, C> {
  active$: Observable<boolean>;
}

export type ReplicationCheckpoint = {
  id: string;
  updatedAt: string;
};

export type LiveDocsDocType = WorkspaceDocType | UserDocType | LiveDocDocType;