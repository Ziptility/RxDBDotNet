// src/lib/userReplication.ts

import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { PushUserPayload, User, UserFilterInput } from '@/generated/graphql';
import type { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
import { handleError } from '@/utils/errorHandling';
import type {
  ReplicationPushHandlerResult,
  RxCollection,
  RxGraphQLReplicationPullQueryBuilder,
  RxGraphQLReplicationPullStreamQueryBuilder,
  RxGraphQLReplicationPushQueryBuilder,
  RxReplicationWriteToMasterRow,
} from 'rxdb';

/**
 * Builds the GraphQL query for pulling user data.
 *
 * @param variables - Optional filter input for the query.
 * @returns A function that generates the GraphQL query object.
 */
const pullQueryBuilder = (variables?: UserFilterInput): RxGraphQLReplicationPullQueryBuilder<ReplicationCheckpoint> => {
  return (checkpoint: ReplicationCheckpoint | undefined, limit: number) => {
    const query = `
      query PullUser($checkpoint: UserInputCheckpoint, $limit: Int!, $where: UserFilterInput) {
        pullUser(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            firstName
            lastName
            email
            role
            jwtAccessToken
            topics
            workspaceId
            updatedAt
            isDeleted
          }
          checkpoint {
            lastDocumentId
            updatedAt
          }
        }
      }
    `;

    return {
      query,
      operationName: 'PullUser',
      variables: {
        checkpoint,
        limit,
        where: variables,
      },
    };
  };
};

/**
 * Builds the GraphQL mutation for pushing user data.
 *
 * @param pushRows - The user data to be pushed.
 * @returns The GraphQL mutation object.
 */
const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (pushRows: RxReplicationWriteToMasterRow<User>[]) => {
  const query = `
    mutation PushUser($input: PushUserInput!) {
      pushUser(input: $input) {
        user {
          id
          firstName
          lastName
          email
          role
          topics
          workspaceId
          updatedAt
          isDeleted
        }
        errors {
          ... on AuthenticationError {
            message
          }
          ... on UnauthorizedAccessError {
            message
          }
        }
      }
    }
  `;

  return {
    query,
    operationName: 'PushUser',
    variables: {
      input: {
        userPushRow: pushRows,
      },
    },
  };
};

/**
 * Builds the GraphQL subscription for streaming user data.
 *
 * @param topics - The topics to subscribe to.
 * @returns A function that generates the GraphQL subscription object.
 */
const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamUser($headers: UserInputHeaders, $topics: [String!]) {
        streamUser(headers: $headers, topics: $topics) {
          documents {
            id
            firstName
            lastName
            email
            role
            jwtAccessToken
            topics
            workspaceId
            updatedAt
            isDeleted
          }
          checkpoint {
            lastDocumentId
            updatedAt
          }
        }
      }
    `;

    return {
      query,
      variables: {
        headers,
        topics,
      },
    };
  };
};

/**
 * Sets up logging and error handlers.
 *
 * @param replicationState - The replication state to initialize.
 */
const initializeLoggingAndErrorHandlers = (replicationState: LiveDocsReplicationState<User>): void => {
  // emits and handles all errors that happen when running the push- & pull-handlers.
  replicationState.error$.subscribe((error) => {
    handleError(error, 'User replication');
  });

  // emits each document that was received from the remote
  replicationState.received$.subscribe((doc) => {
    console.log('User replication received:', doc);
  });

  // emits each document that was send to the remote
  replicationState.sent$.subscribe((doc) => {
    console.log('User replication sent:', doc);
  });

  // emits true when a replication cycle is running, false when not.
  replicationState.active$.subscribe((active) => {
    console.log('User replication active:', active);
  });

  replicationState.remoteEvents$.subscribe((event) => {
    console.log('User replication remote event:', event);
  });

  // emits true when the replication was canceled, false when not.
  replicationState.canceled$.subscribe(() => {
    console.log('User replication canceled');
  });
};

/**
 * Creates a replicator for the User collection.
 *
 * @param token - The authentication token.
 * @param collection - The RxDB collection for users.
 * @param filter - Optional filter for the replication.
 * @param topics - Optional topics for subscription.
 * @param batchSize - The number of documents to process in each batch.
 * @returns A LiveDocsReplicationState for the User collection.
 */
export const createUserReplicator = (
  token: string,
  collection: RxCollection<User>,
  filter?: UserFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<User> => {
  const replicationState = replicateGraphQL({
    collection,
    url: {
      http: API_CONFIG.GRAPHQL_ENDPOINT,
      ws: API_CONFIG.WS_ENDPOINT,
    },
    pull: {
      queryBuilder: pullQueryBuilder(filter),
      streamQueryBuilder: pullStreamBuilder(topics),
      batchSize,
      includeWsHeaders: true,
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize,
      responseModifier: (response: PushUserPayload): ReplicationPushHandlerResult<User> => {
        if (response.errors) {
          response.errors.forEach((error) => handleError(error, 'User replication push'));
        }
        return response.user ?? [];
      },
    },
    live: true,
    deletedField: 'isDeleted',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    replicationIdentifier: 'user-replication',
    autoStart: true,
  });

  initializeLoggingAndErrorHandlers(replicationState);

  return replicationState;
};
