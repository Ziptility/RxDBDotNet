// src/lib/liveDocReplication.ts

import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { Checkpoint, LiveDoc, LiveDocFilterInput, PushLiveDocPayload } from '@/generated/graphql';
import type { LiveDocsReplicationState } from '@/types';
import { handleError } from '@/utils/errorHandling';
import type {
  RxCollection,
  RxGraphQLReplicationPullQueryBuilder,
  RxGraphQLReplicationPullStreamQueryBuilder,
  RxReplicationWriteToMasterRow,
  RxGraphQLReplicationPushQueryBuilder,
  ReplicationPushHandlerResult,
} from 'rxdb';

/**
 * Builds the GraphQL query for pulling LiveDoc data.
 *
 * @param variables - Optional filter input for the query.
 * @returns A function that generates the GraphQL query object.
 */
const pullQueryBuilder = (variables?: LiveDocFilterInput): RxGraphQLReplicationPullQueryBuilder<Checkpoint> => {
  return (checkpoint: Checkpoint | undefined, limit: number) => {
    const query = `
      query PullLiveDoc($checkpoint: LiveDocInputCheckpoint, $limit: Int!, $where: LiveDocFilterInput) {
        pullLiveDoc(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            content
            topics
            ownerId
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
      operationName: 'PullLiveDoc',
      variables: {
        checkpoint,
        limit,
        where: variables,
      },
    };
  };
};

/**
 * Builds the GraphQL mutation for pushing LiveDoc data.
 *
 * @param pushRows - The LiveDoc data to be pushed.
 * @returns The GraphQL mutation object.
 */
const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (pushRows: RxReplicationWriteToMasterRow<LiveDoc>[]) => {
  const query = `
    mutation PushLiveDoc($input: PushLiveDocInput!) {
      pushLiveDoc(input: $input) {
        liveDoc {
          id
          content
          topics
          ownerId
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
    operationName: 'PushLiveDoc',
    variables: {
      input: {
        liveDocPushRow: pushRows,
      },
    },
  };
};

/**
 * Builds the GraphQL subscription for streaming LiveDoc data.
 *
 * @param topics - The topics to subscribe to.
 * @returns A function that generates the GraphQL subscription object.
 */
const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamLiveDoc($headers: LiveDocInputHeaders, $topics: [String!]) {
        streamLiveDoc(headers: $headers, topics: $topics) {
          documents {
            id
            content
            topics
            ownerId
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
const initializeLoggingAndErrorHandlers = (replicationState: LiveDocsReplicationState<LiveDoc>): void => {
  // emits and handles all errors that happen when running the push- & pull-handlers.
  replicationState.error$.subscribe((error) => {
    handleError(error, 'LiveDoc replication');
  });

  // emits each document that was received from the remote
  replicationState.received$.subscribe((doc) => {
    console.log('LiveDoc replication received:', doc);
  });

  // emits each document that was send to the remote
  replicationState.sent$.subscribe((doc) => {
    console.log('LiveDoc replication sent:', doc);
  });

  // emits true when a replication cycle is running, false when not.
  replicationState.active$.subscribe((active) => {
    console.log('LiveDoc replication active:', active);
  });

  replicationState.remoteEvents$.subscribe((event) => {
    console.log('LiveDoc replication remote event:', event);
  });

  // emits true when the replication was canceled, false when not.
  replicationState.canceled$.subscribe(() => {
    console.log('LiveDoc replication canceled');
  });
};

/**
 * Creates a replicator for the LiveDoc collection.
 *
 * @param token - The authentication token.
 * @param collection - The RxDB collection for LiveDocs.
 * @param filter - Optional filter for the replication.
 * @param topics - Optional topics for subscription.
 * @param batchSize - The number of documents to process in each batch.
 * @returns A LiveDocsReplicationState for the LiveDoc collection.
 */
export const replicateLiveDocs = (
  token: string,
  collection: RxCollection<LiveDoc>,
  filter?: LiveDocFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<LiveDoc> => {
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
      responseModifier: (response: PushLiveDocPayload): ReplicationPushHandlerResult<LiveDoc> => {
        if (response.errors) {
          response.errors.forEach((error) => handleError(error, 'LiveDoc replication push'));
        }
        return response.liveDoc ?? [];
      },
    },
    live: true,
    deletedField: 'isDeleted',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    replicationIdentifier: 'live-doc-replication',
    autoStart: true,
  });

  initializeLoggingAndErrorHandlers(replicationState);

  return replicationState;
};
