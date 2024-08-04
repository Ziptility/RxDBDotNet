import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';
import { RxCollection, RxDatabase, RxDocument } from 'rxdb';
import { RxReplicationState as RxDBReplicationState } from 'rxdb/plugins/replication';

export interface Workspace extends WorkspaceDocType {}
export interface User extends UserDocType {}
export interface LiveDoc extends LiveDocDocType {}

// Define the LiveDocsCollections type
export type LiveDocsCollections = {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  liveDocs: RxCollection<LiveDocDocType>;
};

// Define the LiveDocsDatabase type
export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

// Use the RxReplicationState type from RxDB
export type RxReplicationState<T, C> = RxDBReplicationState<RxDocument<T>, C>;

// Type for the checkpoint
export type Checkpoint = {
  id: string;
  updatedAt: string;
};

// Generic type for document types
export type DocType = WorkspaceDocType | UserDocType | LiveDocDocType;