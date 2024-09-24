// src/lib/replication.ts

import { API_CONFIG } from '@/config';
import { createLiveDocReplicator } from '@/lib/liveDocReplication';
import { createUserReplicator } from '@/lib/userReplication';
import { createWorkspaceReplicator } from '@/lib/workspaceReplication';
import type { LiveDocsDatabase, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';

/**
 * Sets up replication for all collections in the database.
 * This function creates replicators for each collection and optionally starts them.
 *
 * @param {LiveDocsDatabase} db - The RxDB database instance.
 * @param {string} jwtAccessToken - The JWT token for authentication with the backend.
 * @param {boolean} [autoStart=true] - Whether to automatically start the replication.
 * @returns {Promise<LiveDocsReplicationStates>} A promise that resolves to an object containing all replication states.
 */
export const setupReplication = (db: LiveDocsDatabase, jwtAccessToken: string): LiveDocsReplicationStates => {
  const token = jwtAccessToken || API_CONFIG.DEFAULT_JWT_TOKEN;

  const replicationStates: LiveDocsReplicationStates = {
    workspaces: createWorkspaceReplicator(token, db.workspace),
    users: createUserReplicator(token, db.user),
    livedocs: createLiveDocReplicator(token, db.livedoc),
  };

  return replicationStates;
};

/**
 * Updates the JWT token used for authentication in all replication states.
 * This function cancels ongoing replications, updates the token, and restarts the replications.
 *
 * @param {LiveDocsReplicationStates} replicationStates - The current replication states for all collections.
 * @param {string} newToken - The new JWT token to use for authentication.
 * @returns {Promise<void>}
 */
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

/**
 * Best Practices and Notes for Maintainers:
 *
 * 1. Token Management: The setupReplication function uses a fallback to DEFAULT_JWT_TOKEN
 *    if no token is provided. Ensure that your authentication flow properly manages and
 *    updates tokens as needed.
 *
 * 2. AutoStart: The autoStart parameter allows for flexible control over when replication begins.
 *    By default, it's set to true, which means replication will start immediately after setup.
 *
 * 3. Replication States: The function returns an object with replication states for each collection.
 *    These states can be used to monitor and control replication for individual collections.
 *
 * 4. Error Handling: While this function doesn't explicitly handle errors, it's important to
 *    implement proper error handling where this function is called, especially when starting
 *    replications.
 *
 * 5. Token Updates: The updateReplicationToken function should be called whenever the JWT token
 *    changes (e.g., on user login/logout or token refresh).
 *
 * 6. Performance Considerations: Starting and stopping replications can be resource-intensive.
 *    Consider the frequency of token updates and their impact on application performance.
 *
 * 7. Customization: If different collections require different replication strategies,
 *    modify the setupReplication function accordingly.
 */
