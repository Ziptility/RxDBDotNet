// src/lib/database.ts

import { createRxDatabase, addRxPlugin } from 'rxdb';
import { RxDBDevModePlugin, disableWarnings } from 'rxdb/plugins/dev-mode';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { API_CONFIG } from '@/config';
import type { LiveDocsDatabase, LiveDocsCollections, LiveDocsCollectionConfig } from '@/types';
import { setupReplication } from './replication';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';

// Disable RxDB warnings in development mode
disableWarnings();

// Add the development mode plugin for better debugging capabilities
addRxPlugin(RxDBDevModePlugin);

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
 * Initializes the RxDB database.
 * This function creates the database, adds collections, and sets up replication.
 * It should only be called once during the application lifecycle.
 *
 * @returns {Promise<LiveDocsDatabase>} A promise that resolves to the initialized database.
 * @throws {Error} If database initialization fails.
 */
const initializeDatabase = async (): Promise<LiveDocsDatabase> => {
  initializationCount++;
  console.log(`Initializing database... (Attempt #${initializationCount})`);

  try {
    // Create the RxDB database
    // IMPORTANT: Ensure this combination of name and adapter is used only once.
    // This can cause issues in React projects with hot reloading, which may
    // reload code without fully resetting the application state.
    const db = await createRxDatabase<LiveDocsCollections>({
      name: 'livedocsdb',
      storage: getRxStorageDexie(),
      multiInstance: false, // Set to false for single-instance applications (e.g., single-window electron apps)
      ignoreDuplicate: false, // Set to false to throw an error if multiple instances are created (helps catch mistakes)
      eventReduce: true, // Improves performance by reducing the number of change events
    });

    console.log('Database created successfully');

    // Add collections to the database
    await db.addCollections(createCollections());
    console.log('Collections added successfully');

    // Set up replication with the backend
    // We're passing true as the third argument to enable autoStart
    const replicationStates = setupReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);
    console.log('Replication set up successfully');

    // Return the database instance with replication states attached
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
  if (!databasePromise) {
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
 */
