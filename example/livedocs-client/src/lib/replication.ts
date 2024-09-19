import { API_CONFIG } from '@/config';
import { createLiveDocReplicator } from '@/lib/liveDocReplication';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
import { createUserReplicator } from '@/lib/userReplication';
import { createWorkspaceReplicator } from '@/lib/workspaceReplication';
import type { LiveDocsDatabase, LiveDocsReplicationStates, ReplicationCheckpoint } from '@/types';
import type { RxCollection } from 'rxdb';
import type { RxGraphQLReplicationState } from 'rxdb/plugins/replication-graphql';

const setupWorkspaceReplication = async (
  collection: RxCollection<Workspace>,
  token: string
): Promise<LiveDocsReplicationStates['workspaces']> => {
  const replicator = createWorkspaceReplicator(
    token,
    collection,
    {}, // Add any default filters here
    [] // Add any default topics here
  );

  await replicator.start();
  return replicator;
};

const setupUserReplication = async (
  collection: RxCollection<User>,
  token: string
): Promise<LiveDocsReplicationStates['users']> => {
  const replicator = createUserReplicator(
    token,
    collection,
    {}, // Add any default filters here
    [] // Add any default topics here
  );

  await replicator.start();
  return replicator;
};

const setupLiveDocReplication = async (
  collection: RxCollection<LiveDoc>,
  token: string
): Promise<LiveDocsReplicationStates['livedocs']> => {
  const replicator = createLiveDocReplicator(
    token,
    collection,
    {}, // Add any default filters here
    [] // Add any default topics here
  );

  await replicator.start();
  return replicator;
};

export const setupReplication = async (
  db: LiveDocsDatabase,
  jwtAccessToken: string
): Promise<LiveDocsReplicationStates> => {
  const token = jwtAccessToken || API_CONFIG.DEFAULT_JWT_TOKEN;

  const replicationStates: LiveDocsReplicationStates = {
    workspaces: await setupWorkspaceReplication(db.workspace, token),
    users: await setupUserReplication(db.user, token),
    livedocs: await setupLiveDocReplication(db.livedoc, token),
  };

  return replicationStates;
};

export const restartReplication = async (replicationStates: LiveDocsReplicationStates): Promise<void> => {
  await Promise.all(
    Object.entries(replicationStates).map(async ([, state]) => {
      if (typeof state === 'object' && 'cancel' in state && 'start' in state) {
        const typedState = state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>;
        await typedState.cancel();
        await typedState.start();
      }
    })
  );
};

export const cancelReplication = async (replicationStates: LiveDocsReplicationStates): Promise<void> => {
  await Promise.all(
    Object.values(replicationStates).map((state) =>
      (state as RxGraphQLReplicationState<unknown, ReplicationCheckpoint>).cancel()
    )
  );
};
