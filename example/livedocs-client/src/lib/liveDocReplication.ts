// src\lib\liveDocReplication.ts
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { LiveDoc, LiveDocFilterInput, PushLiveDocPayload } from '@/generated/graphql';
import type { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
import type {
  RxCollection,
  RxGraphQLReplicationPullQueryBuilder,
  RxGraphQLReplicationPullStreamQueryBuilder,
  RxReplicationWriteToMasterRow,
  RxGraphQLReplicationPushQueryBuilder,
  ReplicationPushHandlerResult,
} from 'rxdb';

const pullQueryBuilder = (
  variables?: LiveDocFilterInput
): RxGraphQLReplicationPullQueryBuilder<ReplicationCheckpoint> => {
  return (checkpoint: ReplicationCheckpoint | undefined, limit: number) => {
    const query = `
      query PullLiveDoc($checkpoint: LiveDocInputCheckpoint, $limit: Int!, $where: LiveDocFilterInput) {
        pullLiveDoc(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            content
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

const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (pushRows: RxReplicationWriteToMasterRow<LiveDoc>[]) => {
  const query = `
    mutation PushLiveDoc($input: PushLiveDocInput!) {
      pushLiveDoc(input: $input) {
        liveDoc {
          id
          content
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

const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamLiveDoc($headers: LiveDocInputHeaders, $topics: [String!]) {
        streamLiveDoc(headers: $headers, topics: $topics) {
          documents {
            id
            content
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

export const createLiveDocReplicator = (
  token: string,
  collection: RxCollection<LiveDoc>,
  filter?: LiveDocFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<LiveDoc> => {
  return replicateGraphQL({
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
          console.log('push live doc errors', response.errors);
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
};
