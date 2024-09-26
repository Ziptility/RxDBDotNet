// src\lib\userReplication.ts
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { PushUserPayload, User, UserFilterInput } from '@/generated/graphql';
import type { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
import type {
  ReplicationPushHandlerResult,
  RxCollection,
  RxGraphQLReplicationPullQueryBuilder,
  RxGraphQLReplicationPullStreamQueryBuilder,
  RxGraphQLReplicationPushQueryBuilder,
  RxReplicationWriteToMasterRow,
} from 'rxdb';

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

export const createUserReplicator = (
  token: string,
  collection: RxCollection<User>,
  filter?: UserFilterInput,
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<User> => {
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
      responseModifier: (response: PushUserPayload): ReplicationPushHandlerResult<User> => {
        if (response.errors) {
          console.log('push user errors', response.errors);
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
};
