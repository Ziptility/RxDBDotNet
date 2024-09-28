// src/lib/workspaceReplication.ts

import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { PushWorkspacePayload, Workspace, WorkspaceFilterInput } from '@/generated/graphql';
import type { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
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
 * Builds the GraphQL query for pulling Workspace data.
 *
 * @param variables - Optional filter input for the query.
 * @returns A function that generates the GraphQL query object.
 */
const pullQueryBuilder = (
  variables?: WorkspaceFilterInput
): RxGraphQLReplicationPullQueryBuilder<ReplicationCheckpoint> => {
  return (checkpoint: ReplicationCheckpoint | undefined, limit: number) => {
    const query = `
      query PullWorkspace($checkpoint: WorkspaceInputCheckpoint, $limit: Int!, $where: WorkspaceFilterInput) {
        pullWorkspace(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            name
            topics
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
      operationName: 'PullWorkspace',
      variables: {
        checkpoint,
        limit,
        where: variables,
      },
    };
  };
};

/**
 * Builds the GraphQL mutation for pushing Workspace data.
 *
 * @param pushRows - The Workspace data to be pushed.
 * @returns The GraphQL mutation object.
 */
const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (
  pushRows: RxReplicationWriteToMasterRow<Workspace>[]
) => {
  const query = `
    mutation PushWorkspace($input: PushWorkspaceInput!) {
      pushWorkspace(input: $input) {
        workspace {
          id
          name
          topics
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
    operationName: 'PushWorkspace',
    variables: {
      input: {
        workspacePushRow: pushRows,
      },
    },
  };
};

/**
 * Builds the GraphQL subscription for streaming Workspace data.
 *
 * @param topics - The topics to subscribe to.
 * @returns A function that generates the GraphQL subscription object.
 */
const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamWorkspace($headers: WorkspaceInputHeaders, $topics: [String!]) {
        streamWorkspace(headers: $headers, topics: $topics) {
          documents {
            id
            name
            topics
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
const initializeLoggingAndErrorHandlers = (replicationState: LiveDocsReplicationState<Workspace>): void => {
  // emits and handles all errors that happen when running the push- & pull-handlers.
  replicationState.error$.subscribe((error) => {
    handleError(error, 'Workspace replication');
  });

  // emits each document that was received from the remote
  replicationState.received$.subscribe((doc) => {
    console.log('Workspace replication received:', doc);
  });

  // emits each document that was send to the remote
  replicationState.sent$.subscribe((doc) => {
    console.log('Workspace replication sent:', doc);
  });

  // emits true when a replication cycle is running, false when not.
  replicationState.active$.subscribe((active) => {
    console.log('Workspace replication active:', active);
  });

  replicationState.remoteEvents$.subscribe((event) => {
    console.log('Workspace replication remote event:', event);
  });

  // emits true when the replication was canceled, false when not.
  replicationState.canceled$.subscribe(() => {
    console.log('Workspace replication canceled');
  });
};

/**
 * Creates a replicator for the Workspace collection.
 *
 * @param token - The authentication token.
 * @param collection - The RxDB collection for Workspaces.
 * @param filter - Optional filter for the replication.
 * @param topics - Optional topics for subscription.
 * @param batchSize - The number of documents to process in each batch.
 * @returns A LiveDocsReplicationState for the Workspace collection.
 */
export const createWorkspaceReplicator = (
  token: string,
  collection: RxCollection<Workspace>,
  filter?: WorkspaceFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<Workspace> => {
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
      responseModifier: (response: PushWorkspacePayload): ReplicationPushHandlerResult<Workspace> => {
        if (response.errors) {
          response.errors.forEach((error) => handleError(error, 'Workspace replication push'));
        }
        return response.workspace ?? [];
      },
    },
    live: true,
    deletedField: 'isDeleted',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    replicationIdentifier: 'workspace-replication',
    autoStart: true,
  });

  initializeLoggingAndErrorHandlers(replicationState);

  return replicationState;
};
