// src/types/index.ts

import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';
import { RxReplicationState as RxDBReplicationState } from 'rxdb/plugins/replication';

export interface LiveDocsCollections {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  livedocs: RxCollection<LiveDocDocType>;
}

export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

export type RxReplicationState<T, C> = RxDBReplicationState<T, C>;

export interface ReplicationCheckpoint {
  lastDocumentId: string | null;
  updatedAt: string | null;
}

export type LiveDocsDocType = WorkspaceDocType | UserDocType | LiveDocDocType;
