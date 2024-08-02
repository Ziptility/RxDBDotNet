import { LiveDocsDatabase } from '@/lib/database';
import { replicateGraphQL } from 'rxdb/plugins/replication-graphql';
import { SubscriptionClient } from 'subscriptions-transport-ws';
import { lastValueFrom } from 'rxjs';

const GRAPHQL_ENDPOINT = process.env.NEXT_PUBLIC_GRAPHQL_ENDPOINT || 'http://localhost:5414/graphql';
const WS_ENDPOINT = process.env.NEXT_PUBLIC_WS_ENDPOINT || 'ws://localhost:5414/graphql';

export const setupReplication = async (db: LiveDocsDatabase) => {
  const subscriptionClient = new SubscriptionClient(WS_ENDPOINT, {
    reconnect: true,
  });

  const replicationStates = [
    replicateGraphQL({
      collection: db.workspaces,
      url: GRAPHQL_ENDPOINT,
      pull: {
        queryBuilder: (checkpoint) => ({
          query: `
            query PullWorkspaces($checkpoint: WorkspaceInputCheckpoint, $limit: Int!) {
              pullWorkspace(checkpoint: $checkpoint, limit: $limit) {
                documents {
                  id
                  name
                  updatedAt
                  isDeleted
                }
                checkpoint {
                  id
                  updatedAt
                }
              }
            }
          `,
          variables: {
            checkpoint,
            limit: 100,
          },
        }),
        dataPath: 'pullWorkspace.documents',
        checkpointPath: 'pullWorkspace.checkpoint',
      },
      push: {
        queryBuilder: (docs) => ({
          query: `
            mutation PushWorkspaces($workspaces: [WorkspaceInputPushRow!]) {
              pushWorkspace(workspacePushRow: $workspaces) {
                id
                name
                updatedAt
                isDeleted
              }
            }
          `,
          variables: {
            workspaces: docs.map((d) => ({
              assumedMasterState: null,
              newDocumentState: d,
            })),
          },
        }),
        dataPath: 'pushWorkspace',
      },
      live: true,
      liveInterval: 1000 * 60 * 10, // 10 minutes
      deletedFlag: 'isDeleted',
    }),
    replicateGraphQL({
      collection: db.users,
      url: GRAPHQL_ENDPOINT,
      pull: {
        queryBuilder: (checkpoint) => ({
          query: `
            query PullUsers($checkpoint: UserInputCheckpoint, $limit: Int!) {
              pullUser(checkpoint: $checkpoint, limit: $limit) {
                documents {
                  id
                  firstName
                  lastName
                  email
                  role
                  workspaceId
                  updatedAt
                  isDeleted
                }
                checkpoint {
                  id
                  updatedAt
                }
              }
            }
          `,
          variables: {
            checkpoint,
            limit: 100,
          },
        }),
        dataPath: 'pullUser.documents',
        checkpointPath: 'pullUser.checkpoint',
      },
      push: {
        queryBuilder: (docs) => ({
          query: `
            mutation PushUsers($users: [UserInputPushRow!]) {
              pushUser(userPushRow: $users) {
                id
                firstName
                lastName
                email
                role
                workspaceId
                updatedAt
                isDeleted
              }
            }
          `,
          variables: {
            users: docs.map((d) => ({
              assumedMasterState: null,
              newDocumentState: d,
            })),
          },
        }),
        dataPath: 'pushUser',
      },
      live: true,
      liveInterval: 1000 * 60 * 10, // 10 minutes
      deletedFlag: 'isDeleted',
    }),
    replicateGraphQL({
      collection: db.liveDocs,
      url: GRAPHQL_ENDPOINT,
      pull: {
        queryBuilder: (checkpoint) => ({
          query: `
            query PullLiveDocs($checkpoint: LiveDocInputCheckpoint, $limit: Int!) {
              pullLiveDoc(checkpoint: $checkpoint, limit: $limit) {
                documents {
                  id
                  content
                  ownerId
                  workspaceId
                  updatedAt
                  isDeleted
                }
                checkpoint {
                  id
                  updatedAt
                }
              }
            }
          `,
          variables: {
            checkpoint,
            limit: 100,
          },
        }),
        dataPath: 'pullLiveDoc.documents',
        checkpointPath: 'pullLiveDoc.checkpoint',
      },
      push: {
        queryBuilder: (docs) => ({
          query: `
            mutation PushLiveDocs($liveDocs: [LiveDocInputPushRow!]) {
              pushLiveDoc(liveDocPushRow: $liveDocs) {
                id
                content
                ownerId
                workspaceId
                updatedAt
                isDeleted
              }
            }
          `,
          variables: {
            liveDocs: docs.map((d) => ({
              assumedMasterState: null,
              newDocumentState: d,
            })),
          },
        }),
        dataPath: 'pushLiveDoc',
      },
      live: true,
      liveInterval: 1000 * 60 * 10, // 10 minutes
      deletedFlag: 'isDeleted',
    }),
  ];

  await Promise.all(replicationStates.map((state) => lastValueFrom(state.awaitInitialReplication())));

  return replicationStates;
};