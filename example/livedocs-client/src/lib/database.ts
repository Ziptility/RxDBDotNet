// src/lib/database.ts

import { createRxDatabase, addRxPlugin } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDatabase, LiveDocsCollections } from '@/types';

addRxPlugin(RxDBDevModePlugin);

let dbPromise: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (dbPromise) return dbPromise;

  dbPromise = createRxDatabase<LiveDocsCollections>({
    name: 'livedocsdb',
    storage: getRxStorageDexie(),
  }).then(async (db) => {
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

    return db;
  });

  return dbPromise;
};

export type { LiveDocsDatabase };
