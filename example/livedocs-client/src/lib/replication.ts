// src/lib/replication.ts

import { API_CONFIG } from '@/config';
import { createLiveDocReplicator } from '@/lib/liveDocReplication';
import { createUserReplicator } from '@/lib/userReplication';
import { createWorkspaceReplicator } from '@/lib/workspaceReplication';
import type { LiveDocsDatabase, LiveDocsReplicationState, LiveDocsReplicationStates } from '@/types';
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
    const replicationStates: LiveDocsReplicationStates = {
      workspaces: createWorkspaceReplicator(token, db.workspace),
      users: createUserReplicator(token, db.user),
      livedocs: createLiveDocReplicator(token, db.livedoc),
    };

    return replicationStates;
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
      Object.values(replicationStates).map((state: LiveDocsReplicationState<unknown>) => {
        state.setHeaders({ Authorization: `Bearer ${newToken}` });
      })
    );
  } catch (error) {
    handleError(error, 'Updating replication token');
    throw error; // Re-throw to allow caller to handle the error
  }
};

/**
 * Best Practices and Notes for Maintainers:
 *
 * 1. Token Management:
 *    - The setupReplication function uses a fallback to DEFAULT_JWT_TOKEN if no token is provided.
 *    - Ensure that your authentication flow properly manages and updates tokens as needed.
 *    - Use updateReplicationToken whenever the JWT token changes (e.g., on user login/logout or token refresh).
 *
 * 2. Replication States:
 *    - The setupReplication function returns an object with replication states for each collection.
 *    - These states can be used to monitor and control replication for individual collections.
 *    - You can access properties like .error$, .received$, and .sent$ on each state for detailed monitoring.
 *
 * 3. Error Handling:
 *    - While these functions handle errors internally using handleError, they also re-throw errors.
 *    - Implement proper error handling where these functions are called, especially when starting replications.
 *    - Consider implementing retry mechanisms for transient errors.
 *
 * 4. Performance Considerations:
 *    - Starting and stopping replications can be resource-intensive.
 *    - Consider the frequency of token updates and their impact on application performance.
 *    - Use stopAllReplications and restartAllReplications judiciously, as frequent starts and stops can impact performance.
 *
 * 5. Customization:
 *    - If different collections require different replication strategies, modify the setupReplication function accordingly.
 *    - You may need to add additional parameters to createWorkspaceReplicator, createUserReplicator, and createLiveDocReplicator for custom behaviors.
 *
 * 6. Next.js Considerations:
 *    - This replication setup is designed to work on the client-side in a Next.js application.
 *    - Ensure that replication is only set up in useEffect hooks or similar client-side lifecycle methods.
 *    - Be cautious of using this in server-side rendered (SSR) contexts, as RxDB is primarily a client-side database.
 *
 * 7. Testing:
 *    - Implement thorough unit and integration tests for these replication functions.
 *    - Test various scenarios including token updates, network disconnections, and error conditions.
 *
 * 8. Monitoring and Logging:
 *    - Consider implementing more detailed logging for replication events in a production environment.
 *    - You might want to add application monitoring to track replication performance and error rates.
 *
 * 9. Scalability:
 *    - As your application grows, you might need to implement more granular control over replication.
 *    - Consider strategies like selective replication or pagination for large datasets.
 *
 * 10. Security:
 *     - Ensure that the JWT tokens are securely stored and transmitted.
 *     - Implement proper token refresh mechanisms to maintain continuous authentication.
 *
 * Remember to keep this documentation up-to-date as the replication logic evolves.
 */
