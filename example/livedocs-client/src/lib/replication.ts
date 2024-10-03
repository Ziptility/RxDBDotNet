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
    console.log('Setting up replication with token:', token);
    const replicationStates: LiveDocsReplicationStates = {
      workspaces: replicateWorkspaces(token, db.workspace),
      users: replicateUsers(token, db.user),
      livedocs: replicateLiveDocs(token, db.livedoc),
    };
    console.log('Replication setup completed');
    return replicationStates;
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
  replicationStates: LiveDocsReplicationStates | undefined,
  newToken: string
): Promise<void> => {
  await handleAsyncError(async () => {
    if (!replicationStates) {
      console.warn('Replication states are undefined, skipping token update');
      return;
    }

    console.log('Updating replication token for all collections');
    await Promise.all(
      Object.entries(replicationStates).map(([name, state]: [string, LiveDocsReplicationState<Document>]) => {
        if (typeof state.setHeaders === 'function') {
          state.setHeaders({ Authorization: `Bearer ${newToken}` });
          console.log(`Updated token for ${name} replication`);
        } else {
          console.warn(`Replication state for ${name} is not properly initialized or lacks setHeaders method`);
        }
      })
    );
    console.log('Replication token update completed');
  }, 'updateReplicationToken');
};

/**
 * Best Practices and Notes for Maintainers:
 *
 * 1. Error Handling: Both functions use centralized error handling utilities.
 *    This ensures consistent error logging and handling across the application.
 *
 * 2. Logging: Extensive logging has been added to help diagnose issues.
 *    In a production environment, consider using a more sophisticated logging system.
 *
 * 3. Token Management: The code now handles cases where the token might be undefined or null,
 *    falling back to a default token. Always ensure that a valid token is provided.
 *
 * 4. Type Safety: The functions are now more type-safe, using TypeScript's strict mode.
 *    Always maintain strict typing to catch potential errors at compile-time.
 *
 * 5. Asynchronous Operations: The updateReplicationToken function uses Promise.all for
 *    concurrent execution. Be aware of potential race conditions when updating multiple
 *    replication states simultaneously.
 *
 * 6. Extensibility: The code is structured to easily add new collection types for replication.
 *    When adding a new collection, ensure to update the LiveDocsReplicationStates type and
 *    include it in the setupReplication function.
 *
 * 7. Configuration: The code uses API_CONFIG for default values. Ensure that this configuration
 *    is properly set up and maintained.
 *
 * 8. Replication State Management: The code assumes that replication states have a setHeaders
 *    method. If this changes in future RxDB versions, update this code accordingly.
 *
 * 9. Error Propagation: setupReplication throws errors, while updateReplicationToken handles
 *    them internally. Consider standardizing this behavior based on how errors should be
 *    handled at the application level.
 *
 * 10. Testing: Implement unit tests for these functions, mocking the RxDB replication states
 *     and testing various scenarios including error cases.
 * 11. Null Safety: The updateReplicationToken function now handles the case where
 *     replicationStates might be undefined. This makes the function more robust and
 *     prevents runtime errors.
 *
 * 12. Logging: Extensive logging has been added to help diagnose issues with replication
 *     state initialization and token updates. In a production environment, consider
 *     using a more sophisticated logging system and potentially reducing log verbosity.
 */
