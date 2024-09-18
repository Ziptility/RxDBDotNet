// src\lib\database.ts
import { createRxDatabase, addRxPlugin } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDatabase, LiveDocsCollections } from '@/types';
import { setupReplication } from './replication';
import { handleAsyncError } from '@/utils/errorHandling';

addRxPlugin(RxDBDevModePlugin);

let dbPromise: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (dbPromise) return dbPromise;

  dbPromise = new Promise((resolve, reject) => {
    handleAsyncError<LiveDocsDatabase>(async () => {
      const db = await createRxDatabase<LiveDocsCollections>({
        name: 'livedocsdb',
        storage: getRxStorageDexie(),
      });

      await db.addCollections({
        workspaces: {
          schema: workspaceSchema,
        },
        users: {
          schema: userSchema,
        },
        livedocs: {
          schema: liveDocSchema,
        },
      });

      // Set up replication for all collections
      const replicationStates = await setupReplication(db);

      // Attach replication states to the database instance for later use
      (db as LiveDocsDatabase).replicationStates = replicationStates;

      resolve(db);
      return db;
    }, 'Creating and setting up database').catch(reject);
  });

  return dbPromise;
};

export type { LiveDocsDatabase };
