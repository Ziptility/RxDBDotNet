// src/lib/replication.ts
import { API_CONFIG } from '@/config';
import { replicateLiveDocs } from '@/lib/liveDocReplication';
import { replicateUsers } from '@/lib/userReplication';
import { replicateWorkspaces } from '@/lib/workspaceReplication';
import type { Document, LiveDocsDatabase, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';
import { handleError, handleAsyncError } from '@/utils/errorHandling';

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
    handleError(error, 'setupReplication', { jwtAccessToken });
    throw error;
  }
};

/**
 * Updates the JWT token used for authentication in all replication states.
 *
 * @param {LiveDocsReplicationStates} replicationStates - The current replication states for all collections.
 * @param {string} newToken - The new JWT token to use for authentication.
 * @returns {Promise<void>}
 */
export const updateReplicationToken = async (
  replicationStates: LiveDocsReplicationStates,
  newToken: string
): Promise<void> => {
  await handleAsyncError(async () => {
    await Promise.all(
      Object.entries(replicationStates).map(([name, state]: [string, LiveDocsReplicationState<Document>]) =>
        handleError(
          state.setHeaders({ Authorization: `Bearer ${newToken}` }),
          'updateReplicationToken for collection',
          { collectionName: name, newToken }
        )
      )
    );
  }, 'updateReplicationToken');
};
