import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';
import { RxCollection, RxDatabase } from 'rxdb';

export interface Workspace extends WorkspaceDocType {}
export interface User extends UserDocType {}
export interface LiveDoc extends LiveDocDocType {}

// Define a simplified RxReplicationState type
export interface RxReplicationState {
  cancel: () => Promise<void>;
  reSync: () => void;
  awaitInitialReplication: () => Promise<void>;
  error$: {
    subscribe: (callback: (error: any) => void) => { unsubscribe: () => void };
  };
  active$: {
    getValue: () => boolean;
    subscribe: (callback: (active: boolean) => void) => { unsubscribe: () => void };
  };
}

// Define the LiveDocsCollections type
export type LiveDocsCollections = {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  liveDocs: RxCollection<LiveDocDocType>;
};

// Define the LiveDocsDatabase type
export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;