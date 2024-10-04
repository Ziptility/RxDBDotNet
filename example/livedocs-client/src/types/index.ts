// example/livedocs-client/src/types/index.ts
import type { Checkpoint, LiveDoc, Maybe, Scalars, User, Workspace } from '@/generated/graphql';
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

export type LiveDocsReplicationState<T extends Document> = RxGraphQLReplicationState<T, Checkpoint>;

export interface LiveDocsReplicationStates {
  workspaces: LiveDocsReplicationState<Workspace>;
  users: LiveDocsReplicationState<User>;
  livedocs: LiveDocsReplicationState<LiveDoc>;
}

export type LiveDocsReplicationOptions<T> = SyncOptionsGraphQL<T, Checkpoint>;

/**
 * Represents a document in the LiveDocs system, designed for synchronization via RxDBDotNet.
 */
export interface Document {
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['output'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['output'];
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: Maybe<Array<Scalars['String']['output']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['output'];
}
