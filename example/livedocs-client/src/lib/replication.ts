// src/lib/replication.ts
import { API_CONFIG } from '@/config';
import { createLiveDocReplicator } from '@/lib/liveDocReplication';
import { createUserReplicator } from '@/lib/userReplication';
import { createWorkspaceReplicator } from '@/lib/workspaceReplication';
import type { LiveDocsDatabase, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';

export const setupReplication = async (
  db: LiveDocsDatabase,
  jwtAccessToken: string
): Promise<LiveDocsReplicationStates> => {
  const token = jwtAccessToken || API_CONFIG.DEFAULT_JWT_TOKEN;

  const replicationStates: LiveDocsReplicationStates = {
    workspaces: createWorkspaceReplicator(token, db.workspace),
    users: createUserReplicator(token, db.user),
    livedocs: createLiveDocReplicator(token, db.livedoc),
  };

  // Start all replications
  await Promise.all(Object.values(replicationStates).map((state: LiveDocsReplicationState<unknown>) => state.start()));

  return replicationStates;
};

export const updateReplicationToken = async (
  replicationStates: LiveDocsReplicationStates,
  newToken: string
): Promise<void> => {
  await Promise.all(
    Object.values(replicationStates).map(async (state: LiveDocsReplicationState<unknown>) => {
      await state.cancel();
      state.setHeaders({ Authorization: `Bearer ${newToken}` });
      await state.start();
    })
  );
};
