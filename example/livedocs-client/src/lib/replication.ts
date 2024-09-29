// src/lib/replication.ts

import { API_CONFIG } from '@/config';
import { replicateLiveDocs } from '@/lib/liveDocReplication';
import { replicateUsers } from '@/lib/userReplication';
import { replicateWorkspaces } from '@/lib/workspaceReplication';
import type { Document, LiveDocsDatabase, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';
import { handleError } from '@/utils/errorHandling';

/**
 * Sets up replication for all collections in the database.
 * This function creates replicators for each collection and initializes them.
 *
 * @param {LiveDocsDatabase} db - The RxDB database instance.
 * @param {string} jwtAccessToken - The JWT token for authentication with the backend.
 * @returns {LiveDocsReplicationStates} An object containing all replication states.
 *
 * @throws {Error} If there's an issue setting up the replication.
 */
export const setupReplication = (db: LiveDocsDatabase, jwtAccessToken: string): LiveDocsReplicationStates => {
  const token = jwtAccessToken || API_CONFIG.DEFAULT_JWT_TOKEN;

  try {
    return {
      workspaces: replicateWorkspaces(token, db.workspace),
      users: replicateUsers(token, db.user),
      livedocs: replicateLiveDocs(token, db.livedoc),
    };
  } catch (error) {
    handleError(error, 'Setting up replication');
    throw error; // Re-throw to allow caller to handle the error
  }
};

/**
 * Updates the JWT token used for authentication in all replication states.
 *
 * @param {LiveDocsReplicationStates} replicationStates - The current replication states for all collections.
 * @param {string} newToken - The new JWT token to use for authentication.
 * @returns {Promise<void>}
 *
 * @throws {Error} If there's an issue updating the token for any replication state.
 */
export const updateReplicationToken = async (
  replicationStates: LiveDocsReplicationStates,
  newToken: string
): Promise<void> => {
  try {
    await Promise.all(
      Object.values(replicationStates).map((state: LiveDocsReplicationState<Document>) => {
        state.setHeaders({ Authorization: `Bearer ${newToken}` });
      })
    );
  } catch (error) {
    handleError(error, 'Updating replication token');
    throw error; // Re-throw to allow caller to handle the error
  }
};
