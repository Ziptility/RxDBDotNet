// src\lib\workspaceReplication.ts
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { API_CONFIG } from '@/config';
import type { LiveDocsReplicationState, ReplicationCheckpoint } from '@/types';
import { type Workspace } from './schemas';
import type {
  RxCollection,
  RxGraphQLReplicationPullQueryBuilder,
  RxGraphQLReplicationPullStreamQueryBuilder,
  RxReplicationWriteToMasterRow,
  RxGraphQLReplicationPushQueryBuilder,
} from 'rxdb';

interface WorkspaceFilterInput {
  name?: { eq?: string; contains?: string };
  isDeleted?: { eq?: boolean };
}

const pullQueryBuilder = (
  variables: WorkspaceFilterInput
): RxGraphQLReplicationPullQueryBuilder<ReplicationCheckpoint> => {
  return (checkpoint: ReplicationCheckpoint | undefined, limit: number) => {
    const query = `
      query PullWorkspace($checkpoint: WorkspaceInputCheckpoint, $limit: Int!, $where: WorkspaceFilterInput) {
        pullWorkspace(checkpoint: $checkpoint, limit: $limit, where: $where) {
          documents {
            id
            name
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

const pushQueryBuilder: RxGraphQLReplicationPushQueryBuilder = (
  pushRows: RxReplicationWriteToMasterRow<Workspace>[]
) => {
  const query = `
    mutation PushWorkspace($input: PushWorkspaceInput!) {
      pushWorkspace(input: $input) {
        workspace {
          id
          name
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

const pullStreamBuilder = (topics: string[]): RxGraphQLReplicationPullStreamQueryBuilder => {
  return (headers: { [k: string]: string }) => {
    const query = `
      subscription StreamWorkspace($headers: WorkspaceInputHeaders, $topics: [String!]) {
        streamWorkspace(headers: $headers, topics: $topics) {
          documents {
            id
            name
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

export const createWorkspaceReplicator = (
  token: string,
  collection: RxCollection<Workspace>,
  filter: WorkspaceFilterInput = {},
  topics: string[] = [],
  batchSize = 100
): LiveDocsReplicationState<Workspace> => {
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
    },
    push: {
      queryBuilder: pushQueryBuilder,
      batchSize,
    },
    live: true,
    deletedField: 'isDeleted',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    replicationIdentifier: `workspace-replication`,
    autoStart: true,
  });
};
