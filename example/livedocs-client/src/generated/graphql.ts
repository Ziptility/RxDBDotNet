export type Maybe<T> = T | null;
export type InputMaybe<T> = T | null | undefined;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export interface Scalars {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: string; output: string; }
  /** The EmailAddress scalar type constitutes a valid email address, represented as a UTF-8 character sequence. The scalar follows the specification defined by the HTML Spec https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address. */
  EmailAddress: { input: string; output: string; }
  UUID: { input: string; output: string; }
}

export enum ApplyPolicy {
  AFTER_RESOLVER = 'AFTER_RESOLVER',
  BEFORE_RESOLVER = 'BEFORE_RESOLVER',
  VALIDATION = 'VALIDATION'
}

export interface AuthenticationError extends Error {
  message: Scalars['String']['output'];
}

export interface BooleanOperationFilterInput {
  eq: InputMaybe<Scalars['Boolean']['input']>;
  neq: InputMaybe<Scalars['Boolean']['input']>;
}

/**
 * Represents a checkpoint in the replication process.
 * Checkpoints are used to track the state of synchronization
 * between the client and server, ensuring that only new or updated
 * documents are synchronized.
 */
export interface Checkpoint {
  /**
   * The ID of the last document included in the synchronization batch.
   *     This ID is used to ensure that synchronization can accurately resume
   *     if it is interrupted, by providing a unique identifier for the last processed document.
   *
   *
   *     If the value is null, it indicates that there are no documents to synchronize,
   *     and the client should treat this as a starting point or an indication that no previous checkpoint exists.
   */
  lastDocumentId: Maybe<Scalars['UUID']['output']>;
  /**
   * The timestamp of the last update included in the synchronization batch.
   *     This timestamp helps in filtering out documents that have already been synchronized,
   *     ensuring that only new or updated documents are processed during synchronization.
   *
   *
   *     If the value is null, it indicates that there are no updates to synchronize,
   *     and the client should treat this as a starting point or an indication that no previous checkpoint exists.
   */
  updatedAt: Maybe<Scalars['DateTime']['output']>;
}

export interface DateTimeOperationFilterInput {
  eq: InputMaybe<Scalars['DateTime']['input']>;
  gt: InputMaybe<Scalars['DateTime']['input']>;
  gte: InputMaybe<Scalars['DateTime']['input']>;
  in: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  lt: InputMaybe<Scalars['DateTime']['input']>;
  lte: InputMaybe<Scalars['DateTime']['input']>;
  neq: InputMaybe<Scalars['DateTime']['input']>;
  ngt: InputMaybe<Scalars['DateTime']['input']>;
  ngte: InputMaybe<Scalars['DateTime']['input']>;
  nin: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  nlt: InputMaybe<Scalars['DateTime']['input']>;
  nlte: InputMaybe<Scalars['DateTime']['input']>;
}

export interface Error {
  message: Scalars['String']['output'];
}

export interface ListStringOperationFilterInput {
  all: InputMaybe<StringOperationFilterInput>;
  any: InputMaybe<Scalars['Boolean']['input']>;
  none: InputMaybe<StringOperationFilterInput>;
  some: InputMaybe<StringOperationFilterInput>;
}

/**
 * Represents a document that can be collaboratively edited in real-time
 * by multiple users within the same workspace.
 */
export interface LiveDoc {
  /** The content of the live doc. */
  content: Scalars['String']['output'];
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['output'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['output'];
  /** The client-assigned identifier of the live doc's owner within the workspace. */
  ownerId: Scalars['UUID']['output'];
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: Maybe<Array<Scalars['String']['output']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['output'];
  /** The client-assigned identifier of the workspace to which the live doc belongs. */
  workspaceId: Scalars['UUID']['output'];
}

/**
 * Represents a document that can be collaboratively edited in real-time
 * by multiple users within the same workspace.
 */
export interface LiveDocFilterInput {
  and: InputMaybe<Array<LiveDocFilterInput>>;
  /** The content of the live doc. */
  content: InputMaybe<StringOperationFilterInput>;
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: InputMaybe<UuidOperationFilterInput>;
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: InputMaybe<BooleanOperationFilterInput>;
  or: InputMaybe<Array<LiveDocFilterInput>>;
  /** The client-assigned identifier of the live doc's owner within the workspace. */
  ownerId: InputMaybe<UuidOperationFilterInput>;
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<ListStringOperationFilterInput>;
  /** The timestamp of the last update to the document. */
  updatedAt: InputMaybe<DateTimeOperationFilterInput>;
  /** The client-assigned identifier of the workspace to which the live doc belongs. */
  workspaceId: InputMaybe<UuidOperationFilterInput>;
}

/**
 * Represents a document that can be collaboratively edited in real-time
 * by multiple users within the same workspace.
 */
export interface LiveDocInput {
  /** The content of the live doc. */
  content: Scalars['String']['input'];
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['input'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['input'];
  /** The client-assigned identifier of the live doc's owner within the workspace. */
  ownerId: Scalars['UUID']['input'];
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<Array<Scalars['String']['input']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['input'];
  /** The client-assigned identifier of the workspace to which the live doc belongs. */
  workspaceId: Scalars['UUID']['input'];
}

/** Input type for the checkpoint of LiveDoc replication. */
export interface LiveDocInputCheckpoint {
  /** The ID of the last document included in the synchronization batch. */
  lastDocumentId: InputMaybe<Scalars['ID']['input']>;
  /** The timestamp of the last update included in the synchronization batch. */
  updatedAt: InputMaybe<Scalars['DateTime']['input']>;
}

export interface LiveDocInputHeaders {
  /** The JWT bearer token for authentication. */
  Authorization: Scalars['String']['input'];
}

/** Input type for pushing LiveDoc documents to the server. */
export interface LiveDocInputPushRow {
  /** The assumed state of the document on the server before the push. */
  assumedMasterState: InputMaybe<LiveDocInput>;
  /** The new state of the document being pushed. */
  newDocumentState: LiveDocInput;
}

/** Represents the result of a pull operation for LiveDoc documents. */
export interface LiveDocPullBulk {
  /** The new checkpoint after this pull operation. */
  checkpoint: Checkpoint;
  /** The list of LiveDoc documents pulled from the server. */
  documents: Array<LiveDoc>;
}

export interface Mutation {
  /** Pushes LiveDoc documents to the server and detects any conflicts. */
  pushLiveDoc: PushLiveDocPayload;
  /** Pushes User documents to the server and detects any conflicts. */
  pushUser: PushUserPayload;
  /** Pushes Workspace documents to the server and detects any conflicts. */
  pushWorkspace: PushWorkspacePayload;
}


export interface MutationPushLiveDocArgs {
  input: PushLiveDocInput;
}


export interface MutationPushUserArgs {
  input: PushUserInput;
}


export interface MutationPushWorkspaceArgs {
  input: PushWorkspaceInput;
}

export type PushLiveDocError = AuthenticationError | UnauthorizedAccessError;

export interface PushLiveDocInput {
  /** The list of LiveDoc documents to push to the server. */
  liveDocPushRow: InputMaybe<Array<InputMaybe<LiveDocInputPushRow>>>;
}

export interface PushLiveDocPayload {
  errors: Maybe<Array<PushLiveDocError>>;
  liveDoc: Maybe<Array<LiveDoc>>;
}

export type PushUserError = AuthenticationError | UnauthorizedAccessError;

export interface PushUserInput {
  /** The list of User documents to push to the server. */
  userPushRow: InputMaybe<Array<InputMaybe<UserInputPushRow>>>;
}

export interface PushUserPayload {
  errors: Maybe<Array<PushUserError>>;
  user: Maybe<Array<User>>;
}

export type PushWorkspaceError = AuthenticationError | UnauthorizedAccessError;

export interface PushWorkspaceInput {
  /** The list of Workspace documents to push to the server. */
  workspacePushRow: InputMaybe<Array<InputMaybe<WorkspaceInputPushRow>>>;
}

export interface PushWorkspacePayload {
  errors: Maybe<Array<PushWorkspaceError>>;
  workspace: Maybe<Array<Workspace>>;
}

/** src/schema/schema.graphql */
export interface Query {
  /** Pulls LiveDoc documents from the server based on the given checkpoint, limit, optional filters, and projections */
  pullLiveDoc: LiveDocPullBulk;
  /** Pulls User documents from the server based on the given checkpoint, limit, optional filters, and projections */
  pullUser: UserPullBulk;
  /** Pulls Workspace documents from the server based on the given checkpoint, limit, optional filters, and projections */
  pullWorkspace: WorkspacePullBulk;
}


/** src/schema/schema.graphql */
export interface QueryPullLiveDocArgs {
  checkpoint: InputMaybe<LiveDocInputCheckpoint>;
  limit: Scalars['Int']['input'];
  where: InputMaybe<LiveDocFilterInput>;
}


/** src/schema/schema.graphql */
export interface QueryPullUserArgs {
  checkpoint: InputMaybe<UserInputCheckpoint>;
  limit: Scalars['Int']['input'];
  where: InputMaybe<UserFilterInput>;
}


/** src/schema/schema.graphql */
export interface QueryPullWorkspaceArgs {
  checkpoint: InputMaybe<WorkspaceInputCheckpoint>;
  limit: Scalars['Int']['input'];
  where: InputMaybe<WorkspaceFilterInput>;
}

export interface StringOperationFilterInput {
  and: InputMaybe<Array<StringOperationFilterInput>>;
  contains: InputMaybe<Scalars['String']['input']>;
  endsWith: InputMaybe<Scalars['String']['input']>;
  eq: InputMaybe<Scalars['String']['input']>;
  in: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  ncontains: InputMaybe<Scalars['String']['input']>;
  nendsWith: InputMaybe<Scalars['String']['input']>;
  neq: InputMaybe<Scalars['String']['input']>;
  nin: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  nstartsWith: InputMaybe<Scalars['String']['input']>;
  or: InputMaybe<Array<StringOperationFilterInput>>;
  startsWith: InputMaybe<Scalars['String']['input']>;
}

export interface Subscription {
  /** An optional set topics to receive events for when LiveDoc is upserted. If null then events will be received for all LiveDoc upserts. */
  streamLiveDoc: LiveDocPullBulk;
  /** An optional set topics to receive events for when User is upserted. If null then events will be received for all User upserts. */
  streamUser: UserPullBulk;
  /** An optional set topics to receive events for when Workspace is upserted. If null then events will be received for all Workspace upserts. */
  streamWorkspace: WorkspacePullBulk;
}


export interface SubscriptionStreamLiveDocArgs {
  headers: InputMaybe<LiveDocInputHeaders>;
  topics: InputMaybe<Array<Scalars['String']['input']>>;
}


export interface SubscriptionStreamUserArgs {
  headers: InputMaybe<UserInputHeaders>;
  topics: InputMaybe<Array<Scalars['String']['input']>>;
}


export interface SubscriptionStreamWorkspaceArgs {
  headers: InputMaybe<WorkspaceInputHeaders>;
  topics: InputMaybe<Array<Scalars['String']['input']>>;
}

export interface UnauthorizedAccessError extends Error {
  message: Scalars['String']['output'];
}

/** Represents a user of the LiveDocs system. */
export interface User {
  /**
   * The email of the user. jThis must be globally unique.
   * For simplicity in this example app, it cannot be updated.
   */
  email: Maybe<Scalars['EmailAddress']['output']>;
  /** The first name of the user. */
  firstName: Scalars['String']['output'];
  /** The full name of the user. */
  fullName: Maybe<Scalars['String']['output']>;
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['output'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['output'];
  /**
   * A JWT access token used to simulate user authentication in a non-production environment.
   * For simplicity in this example app, it cannot be updated.
   */
  jwtAccessToken: Maybe<Scalars['String']['output']>;
  /** The last name of the user. */
  lastName: Scalars['String']['output'];
  /** The role of the user. */
  role: UserRole;
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: Maybe<Array<Scalars['String']['output']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['output'];
  /** The client-assigned identifier of the workspace to which the user belongs. */
  workspaceId: Scalars['UUID']['output'];
}

/** Represents a user of the LiveDocs system. */
export interface UserFilterInput {
  and: InputMaybe<Array<UserFilterInput>>;
  /**
   * The email of the user. jThis must be globally unique.
   * For simplicity in this example app, it cannot be updated.
   */
  email: InputMaybe<StringOperationFilterInput>;
  /** The first name of the user. */
  firstName: InputMaybe<StringOperationFilterInput>;
  /** The full name of the user. */
  fullName: InputMaybe<StringOperationFilterInput>;
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: InputMaybe<UuidOperationFilterInput>;
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: InputMaybe<BooleanOperationFilterInput>;
  /**
   * A JWT access token used to simulate user authentication in a non-production environment.
   * For simplicity in this example app, it cannot be updated.
   */
  jwtAccessToken: InputMaybe<StringOperationFilterInput>;
  /** The last name of the user. */
  lastName: InputMaybe<StringOperationFilterInput>;
  or: InputMaybe<Array<UserFilterInput>>;
  /** The role of the user. */
  role: InputMaybe<UserRoleOperationFilterInput>;
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<ListStringOperationFilterInput>;
  /** The timestamp of the last update to the document. */
  updatedAt: InputMaybe<DateTimeOperationFilterInput>;
  /** The client-assigned identifier of the workspace to which the user belongs. */
  workspaceId: InputMaybe<UuidOperationFilterInput>;
}

/** Represents a user of the LiveDocs system. */
export interface UserInput {
  /**
   * The email of the user. jThis must be globally unique.
   * For simplicity in this example app, it cannot be updated.
   */
  email: InputMaybe<Scalars['EmailAddress']['input']>;
  /** The first name of the user. */
  firstName: Scalars['String']['input'];
  /** The full name of the user. */
  fullName: InputMaybe<Scalars['String']['input']>;
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['input'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['input'];
  /**
   * A JWT access token used to simulate user authentication in a non-production environment.
   * For simplicity in this example app, it cannot be updated.
   */
  jwtAccessToken: InputMaybe<Scalars['String']['input']>;
  /** The last name of the user. */
  lastName: Scalars['String']['input'];
  /** The role of the user. */
  role: UserRole;
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<Array<Scalars['String']['input']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['input'];
  /** The client-assigned identifier of the workspace to which the user belongs. */
  workspaceId: Scalars['UUID']['input'];
}

/** Input type for the checkpoint of User replication. */
export interface UserInputCheckpoint {
  /** The ID of the last document included in the synchronization batch. */
  lastDocumentId: InputMaybe<Scalars['ID']['input']>;
  /** The timestamp of the last update included in the synchronization batch. */
  updatedAt: InputMaybe<Scalars['DateTime']['input']>;
}

export interface UserInputHeaders {
  /** The JWT bearer token for authentication. */
  Authorization: Scalars['String']['input'];
}

/** Input type for pushing User documents to the server. */
export interface UserInputPushRow {
  /** The assumed state of the document on the server before the push. */
  assumedMasterState: InputMaybe<UserInput>;
  /** The new state of the document being pushed. */
  newDocumentState: UserInput;
}

/** Represents the result of a pull operation for User documents. */
export interface UserPullBulk {
  /** The new checkpoint after this pull operation. */
  checkpoint: Checkpoint;
  /** The list of User documents pulled from the server. */
  documents: Array<User>;
}

/**
 * Defines the roles a user can have within the LiveDocs system,
 * determining their level of access and permissions.
 */
export enum UserRole {
  /** A standard user with access to basic features like viewing and editing their own documents. */
  StandardUser = 'StandardUser',
  /** A system administrator with full control over all workspaces and system settings. */
  SystemAdmin = 'SystemAdmin',
  /** A workspace administrator with permissions to manage users and settings within their own workspace. */
  WorkspaceAdmin = 'WorkspaceAdmin'
}

export interface UserRoleOperationFilterInput {
  eq: InputMaybe<UserRole>;
  in: InputMaybe<Array<UserRole>>;
  neq: InputMaybe<UserRole>;
  nin: InputMaybe<Array<UserRole>>;
}

export interface UuidOperationFilterInput {
  eq: InputMaybe<Scalars['UUID']['input']>;
  gt: InputMaybe<Scalars['UUID']['input']>;
  gte: InputMaybe<Scalars['UUID']['input']>;
  in: InputMaybe<Array<InputMaybe<Scalars['UUID']['input']>>>;
  lt: InputMaybe<Scalars['UUID']['input']>;
  lte: InputMaybe<Scalars['UUID']['input']>;
  neq: InputMaybe<Scalars['UUID']['input']>;
  ngt: InputMaybe<Scalars['UUID']['input']>;
  ngte: InputMaybe<Scalars['UUID']['input']>;
  nin: InputMaybe<Array<InputMaybe<Scalars['UUID']['input']>>>;
  nlt: InputMaybe<Scalars['UUID']['input']>;
  nlte: InputMaybe<Scalars['UUID']['input']>;
}

/** Represents a workspace in the LiveDocs system, designed for synchronization via RxDBDotNet. */
export interface Workspace {
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['output'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['output'];
  /** The name of the workspace. This must be globally unique. */
  name: Scalars['String']['output'];
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: Maybe<Array<Scalars['String']['output']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['output'];
}

/** Represents a workspace in the LiveDocs system, designed for synchronization via RxDBDotNet. */
export interface WorkspaceFilterInput {
  and: InputMaybe<Array<WorkspaceFilterInput>>;
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: InputMaybe<UuidOperationFilterInput>;
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: InputMaybe<BooleanOperationFilterInput>;
  /** The name of the workspace. This must be globally unique. */
  name: InputMaybe<StringOperationFilterInput>;
  or: InputMaybe<Array<WorkspaceFilterInput>>;
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<ListStringOperationFilterInput>;
  /** The timestamp of the last update to the document. */
  updatedAt: InputMaybe<DateTimeOperationFilterInput>;
}

/** Represents a workspace in the LiveDocs system, designed for synchronization via RxDBDotNet. */
export interface WorkspaceInput {
  /**
   * The client-assigned identifier for this document.
   * This property is used for client-side identification and replication purposes.
   */
  id: Scalars['UUID']['input'];
  /** A value indicating whether the document has been marked as deleted. */
  isDeleted: Scalars['Boolean']['input'];
  /** The name of the workspace. This must be globally unique. */
  name: Scalars['String']['input'];
  /** An optional list of topics to publish events to when an instance is upserted. */
  topics: InputMaybe<Array<Scalars['String']['input']>>;
  /** The timestamp of the last update to the document. */
  updatedAt: Scalars['DateTime']['input'];
}

/** Input type for the checkpoint of Workspace replication. */
export interface WorkspaceInputCheckpoint {
  /** The ID of the last document included in the synchronization batch. */
  lastDocumentId: InputMaybe<Scalars['ID']['input']>;
  /** The timestamp of the last update included in the synchronization batch. */
  updatedAt: InputMaybe<Scalars['DateTime']['input']>;
}

export interface WorkspaceInputHeaders {
  /** The JWT bearer token for authentication. */
  Authorization: Scalars['String']['input'];
}

/** Input type for pushing Workspace documents to the server. */
export interface WorkspaceInputPushRow {
  /** The assumed state of the document on the server before the push. */
  assumedMasterState: InputMaybe<WorkspaceInput>;
  /** The new state of the document being pushed. */
  newDocumentState: WorkspaceInput;
}

/** Represents the result of a pull operation for Workspace documents. */
export interface WorkspacePullBulk {
  /** The new checkpoint after this pull operation. */
  checkpoint: Checkpoint;
  /** The list of Workspace documents pulled from the server. */
  documents: Array<Workspace>;
}
