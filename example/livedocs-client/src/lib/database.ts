// src/lib/database.ts

import { createRxDatabase, addRxPlugin } from 'rxdb';
import { RxDBDevModePlugin, disableWarnings } from 'rxdb/plugins/dev-mode';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBUpdatePlugin } from 'rxdb/plugins/update';
import { API_CONFIG } from '@/config';
import type {
  Document,
  LiveDocsDatabase,
  LiveDocsCollections,
  LiveDocsCollectionConfig,
  LiveDocsReplicationState,
  LiveDocsReplicationStates,
} from '@/types';
import { setupReplication } from './replication';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';

// Disable RxDB warnings in development mode
disableWarnings();

// Add the development mode plugin for better debugging capabilities
addRxPlugin(RxDBDevModePlugin);

// Add the update plugin for more efficient document updates
addRxPlugin(RxDBUpdatePlugin);

/**
 * Promise to hold the database instance.
 * Using a promise allows us to handle asynchronous database creation
 * while ensuring only one instance is ever created.
 */
let databasePromise: Promise<LiveDocsDatabase> | null = null;

/**
 * Counter to track database initialization attempts.
 * This is useful for debugging and understanding the lifecycle of the database.
 */
let initializationCount = 0;

/**
 * Creates the collection configurations for the database.
 * This function returns an object with the schema configurations for each collection.
 *
 * @returns {LiveDocsCollectionConfig} An object containing the schema configurations for each collection.
 */
const createCollections = (): LiveDocsCollectionConfig => ({
  workspace: { schema: workspaceSchema },
  user: { schema: userSchema },
  livedoc: { schema: liveDocSchema },
});

/**
 * Sets up detailed logging for replication operations.
 * This function subscribes to various observables of each replication state
 * and logs relevant information to the console.
 *
 * @param {LiveDocsReplicationStates} replicationStates - The replication states for all collections.
 */
const setupReplicationLogging = (replicationStates: LiveDocsReplicationStates): void => {
  Object.entries(replicationStates).forEach(([collectionName, replicator]) => {
    const typedReplicator = replicator as LiveDocsReplicationState<Document>;

    // emits all errors that happen when running the push- & pull-handlers.
    typedReplicator.error$.subscribe((error) => {
      console.warn(`Replication error for ${collectionName}:`, error);
    });

    // emits each document that was send to the remote
    typedReplicator.sent$.subscribe((docs) => {
      console.log(`Replication sent for ${collectionName}:`, docs);
    });

    // emits each document that was received from the remote
    typedReplicator.received$.subscribe((doc) => {
      console.log(`Replication received for ${collectionName}:`, doc);
    });

    // emits true when a replication cycle is running, false when not.
    typedReplicator.active$.subscribe((active) => {
      console.log(`Replication active state for ${collectionName}:`, active);
    });

    // emits true when the replication was canceled, false when not.
    typedReplicator.canceled$.subscribe((canceled) => {
      console.log(`Replication canceled state for ${collectionName}:`, canceled);
    });

    // Log initial replication state
    console.log(`Initial replication state for ${collectionName}:`, {
      isStopped: typedReplicator.isStopped(),
    });
  });
};

/**
 * Initializes the RxDB database.
 * This function creates the database, adds collections, and sets up replication.
 * It should only be called once during the application lifecycle.
 *
 * @returns {Promise<LiveDocsDatabase>} A promise that resolves to the initialized database.
 * @throws {Error} If database initialization fails.
 */
const initializeDatabase = async (): Promise<LiveDocsDatabase> => {
  initializationCount += 1;
  console.log(`Initializing database... (Attempt #${initializationCount})`);

  try {
    const db = await createRxDatabase<LiveDocsCollections>({
      name: 'livedocsdb',
      storage: getRxStorageDexie(),
      multiInstance: false,
      ignoreDuplicate: false,
      eventReduce: true,
    });

    console.log('Database created successfully');

    await db.addCollections(createCollections());
    console.log('Collections added successfully');

    const replicationStates = setupReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);
    console.log('Replication set up successfully');

    setupReplicationLogging(replicationStates);

    return Object.assign(db, { replicationStates }) as LiveDocsDatabase;
  } catch (error) {
    console.error('Failed to initialize database:', error);
    throw error;
  }
};

/**
 * Gets the database instance, creating it if it doesn't exist.
 * This function ensures that only one database instance is created and used throughout the application.
 *
 * @returns {Promise<LiveDocsDatabase>} A promise that resolves to the database instance.
 * @throws {Error} If database initialization fails.
 */
export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  console.log('getDatabase called');
  if (databasePromise === null) {
    console.log('Creating new database promise');
    try {
      databasePromise = initializeDatabase();
      await databasePromise;
      console.log('Database initialized successfully');
    } catch (error) {
      console.error('Database initialization failed:', error);
      databasePromise = null;
      throw error;
    }
  } else {
    console.log('Returning existing database promise');
  }
  return databasePromise;
};

/**
 * Resets the database promise and initialization count.
 * This function is primarily for debugging and testing purposes.
 * It allows for forced re-initialization of the database.
 *
 * CAUTION: Use this function carefully, as it can lead to unexpected behavior if called while the database is in use.
 */
export const resetDatabasePromise = (): void => {
  console.log('Resetting database promise');
  databasePromise = null;
  initializationCount = 0;
};

// Export the LiveDocsDatabase type for use in other parts of the application
export type { LiveDocsDatabase };

/**
 * Best Practices and Notes for Maintainers:
 *
 * 1. Database Singleton: The database should be a singleton in your application.
 *    Always use getDatabase() to access the database instance.
 *
 * 2. Initialization: Database initialization happens lazily when getDatabase() is first called.
 *    Ensure that your application's startup logic accounts for this asynchronous initialization.
 *
 * 3. Error Handling: Always handle potential errors when calling getDatabase().
 *    Database initialization can fail for various reasons (e.g., storage quota exceeded).
 *
 * 4. React and Next.js Considerations:
 *    - In SSR (Server-Side Rendering) contexts, the database should not be initialized.
 *    - For CSR (Client-Side Rendering), ensure database operations are performed in useEffect hooks
 *      or event handlers, not during the render phase.
 *
 * 5. Performance: RxDB operations are asynchronous. Use appropriate React patterns
 *    (like useEffect and useState) to handle asynchronous data fetching and updates.
 *
 * 6. Debugging: Use the extensive console logging provided to diagnose initialization issues.
 *    The resetDatabasePromise() function can be useful for testing but should not be used in production code.
 *
 * 7. Replication: The setupReplication function is called during initialization with autoStart set to true.
 *    This means replication will start automatically when the database is initialized.
 *
 * 8. Schema Changes: If you modify the database schema, ensure you implement and test
 *    appropriate migration strategies to handle existing data.
 *
 * 9. Logging: The setupReplicationLogging function provides detailed logs about replication.
 *    Consider adding a way to toggle this logging on/off in production environments.
 */
