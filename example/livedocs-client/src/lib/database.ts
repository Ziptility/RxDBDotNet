// src\lib\database.ts
import { createRxDatabase, addRxPlugin, RxDatabase } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDatabase, LiveDocsCollections } from '@/types';
import { setupReplication } from './replication';

addRxPlugin(RxDBDevModePlugin);

let dbPromise: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (dbPromise) return dbPromise;

  dbPromise = createRxDatabase<LiveDocsCollections>({
    name: 'livedocsdb',
    storage: getRxStorageDexie(),
  }).then(async (db: RxDatabase<LiveDocsCollections>): Promise<LiveDocsDatabase> => {
    await db.addCollections({
      workspace: {
        schema: workspaceSchema,
      },
      user: {
        schema: userSchema,
      },
      livedoc: {
        schema: liveDocSchema,
      },
    });

    // Set up replication for all collections
    await setupReplication(db);

    return db as LiveDocsDatabase;
  });

  return dbPromise;
};

export type { LiveDocsDatabase };
